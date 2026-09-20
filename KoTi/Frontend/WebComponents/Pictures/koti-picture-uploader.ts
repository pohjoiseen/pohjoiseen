//
// <koti-picture-uploader>: friendly picture upload panel.  This is implemented entirely in JS without htmx,
// with the basic unit being <koti-picture> also used in server-side picture list.  This uploads pictures
// one by one, from file input, drag and drop, or pasting, shows progress and handles errors, allows retry in case
// of errors, allows to view uploaded pictures in fullscreen.
//
// Attributes:
// - target-set-id: id of the folder to upload to (0 for no folder); if not set, target-set is used instead
// - target-set: name of the folder to upload to; without target-set-id, it is a top-level folder which gets created
//   if necessary; with target-set-id this is for display only
// - global-paste: accept pictures pasted anywhere in the document, not just into the component itself
//
export default class PictureUploaderElement extends HTMLElement {
    // list of blobs to be uploaded or picture ids already uploaded
    #pictures: (File | number)[] = [];
    #isUploading = false;
    // after an error, uploading stops until explicitly resumed
    #hasError = false;
    
    // DOM elements
    #pictureList: HTMLElement = null!;
    #uploadButton: HTMLButtonElement = null!;
    #errorAlert: HTMLElement = null!;
    #errorMessage: HTMLElement = null!;
    #uploadInput: HTMLInputElement = null!;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        if (this.hasAttribute('global-paste')) {
            document.addEventListener('paste', this.#onPaste);
        }
        window.addEventListener('beforeunload', this.#onBeforeUnload);
        
        // do not re-initialize if already initialized
        if (this.#pictureList) return;
        
        // add DOM and store important elements
        this.innerHTML = `
            <div class="picture-uploader">
                <div class="list">
                    <h2 class="placeholder-heading">Paste or drop picture(s) here</h2>            
                </div>
                <div class="footer">
                    <div class="alert mb" style="display: none;">
                        <span>Error uploading picture: <span class="error-message"></span>.  Click any not yet uploaded picture or Retry to resume.</span>
                        <button type="button" class="koti-btn primary retry-button">Retry</button>
                    </div>
                    <button type="button" class="koti-btn primary upload-button with-indicator">
                        <span class="spinner"></span>
                        <i class="bi bi-upload"></i> Choose picture(s) to upload...
                    </button>
                    <input type="file" name="files" accept="image/jpeg, image/png" multiple="multiple" hidden="hidden" class="upload-hidden-button">
                    <div class="mt target-set-message"></div>
                </div>
            </div>
        `;
        this.#pictureList = this.querySelector('.list')!;
        this.#uploadButton = this.querySelector('.upload-button')!;
        this.#uploadInput = this.querySelector('.upload-hidden-button')!;
        this.#errorAlert = this.querySelector('.alert')!;
        this.#errorMessage = this.querySelector('.error-message')!;
        this.querySelector('.retry-button')!.addEventListener('click', () => this.uploadNextPicture());
        
        const targetSetMessage = this.querySelector('.target-set-message')!;
        if (this.getAttribute('target-set')) {
            const targetSetName = document.createElement('b');
            targetSetName.textContent = this.getAttribute('target-set');
            targetSetMessage.append('Uploaded pictures will go to ', targetSetName, ' folder.');
        } else {
            targetSetMessage.textContent = 'Uploaded pictures will not go to any folder.';
        }
        
        // upload through file input
        this.querySelector('.upload-button')!.addEventListener('click', () => this.#uploadInput.click());
        this.#uploadInput.addEventListener('input', (e) => {
            if (this.#uploadInput.files) {
                for (const file of [...this.#uploadInput.files]) {
                    this.addPicture(file);
                }
            }
        });
        
        // upload through paste
        if (!this.hasAttribute('global-paste')) {
            this.#pictureList.addEventListener('paste', this.#onPaste);
        }
        
        // click on a not yet uploaded picture resumes upload after an error, starting with that picture 
        this.#pictureList.addEventListener('click', (e) => {
            const pictureEl = (e.target as Element).closest('koti-picture');
            if (pictureEl && !pictureEl.getAttribute('picture-id') && !this.#isUploading) {
                this.uploadNextPicture([...this.#pictureList.children].indexOf(pictureEl));
            }
        });
        
        // upload through drag and drop
        this.#pictureList.addEventListener('dragenter', (e) => {
            e.preventDefault();
            this.#pictureList.classList.add('drop-hover');
        });
        this.#pictureList.addEventListener('dragover', (e) => {
            e.preventDefault();
            // dragleave fires also when moving over child elements, keep highlight on
            this.#pictureList.classList.add('drop-hover');
        });
        this.#pictureList.addEventListener('dragleave', (e) => {
            e.preventDefault();
            this.#pictureList.classList.remove('drop-hover');
        });
        this.#pictureList.addEventListener('drop', (e) => {
            e.preventDefault();
            this.#pictureList.classList.remove('drop-hover');
            if (e.dataTransfer) {
                const files = [...e.dataTransfer.items].map(i => i.getAsFile());
                for (const file of files) {
                    if (file) {
                        this.addPicture(file);
                    }
                }
            }
        });
    }

    disconnectedCallback() {
        document.removeEventListener('paste', this.#onPaste);
        window.removeEventListener('beforeunload', this.#onBeforeUnload);
    }
    
    #onPaste = (e: ClipboardEvent) => {
        if (e.clipboardData) {
            // need to convert all items to files first, otherwise items after first one seem to get lost
            const files = [...e.clipboardData.items].map(i => i.getAsFile());
            for (const file of files) {
                if (file) {
                    this.addPicture(file);
                }
            }
        }
    };
    
    // warn when leaving the page with pictures still not uploaded
    #onBeforeUnload = (e: BeforeUnloadEvent) => {
        if (this.#pictures.some(p => typeof p !== 'number')) {
            e.preventDefault();
        }
    };

    /**
     * Enqueues a picture for upload.
     * @param {File} file
     */
    addPicture(file: File) {
        if (file.type !== 'image/jpeg' && file.type !== 'image/png') {
            return;
        }

        this.#pictures.push(file);
        const url = URL.createObjectURL(file);
        
        const pictureEl = document.createElement('koti-picture');
        pictureEl.setAttribute('state', 'pending');
        pictureEl.setAttribute('title', file.name);
        pictureEl.setAttribute('src', url);
        pictureEl.setAttribute('fullscreen-manual-order', 'true');
        this.#pictureList.appendChild(pictureEl);
        
        this.#pictureList.querySelector('.placeholder-heading')?.remove();
        
        if (!this.#isUploading && !this.#hasError) {
            this.uploadNextPicture();
        }
    }

    /**
     * Picks next not yet uploaded picture, if any, and tries to upload it.
     * @param {number} [preferredIndex] picture to start with, if it is not uploaded yet
     */
    uploadNextPicture(preferredIndex?: number) {
        const index = preferredIndex !== undefined && typeof this.#pictures[preferredIndex] === 'object'
            ? preferredIndex
            : this.#pictures.findIndex(blob => typeof blob !== 'number');
        this.#hasError = false;
        if (index !== -1) {
            this.#isUploading = true;
            this.#uploadButton.classList.add('loading');
            this.#errorAlert.style.display = 'none';
            requestAnimationFrame(() => this.uploadPicture(index));
        } else {
            this.#isUploading = false;
            this.#uploadButton.classList.remove('loading');
        }
    }

    /**
     * Uploads a single picture.
     * @param {number} index
     * @returns {Promise<void>}
     */
    async uploadPicture(index: number) {
        const blob = this.#pictures[index];
        if (!blob) {
            return;
        }
        const pictureEl = this.#pictureList.children[index]!;
        const oldSrc = pictureEl.getAttribute('src')!;

        // just in case
        if (typeof blob === 'number') {
            this.uploadNextPicture();
            return;
        }

        try {
            pictureEl.setAttribute('state', 'uploading 0');

            // hash image data on client side
            // XXX should do on server side now that most everything happens there, but since this was already working anyway
            const pictureAsArrayBuffer = await blob.arrayBuffer();
            const rawHash = await crypto.subtle.digest("SHA-1", pictureAsArrayBuffer);
            const hash = Array.from(new Uint8Array(rawHash))
                .map((b) => b.toString(16).padStart(2, '0'))
                .join('');

            // use XMLHttpRequest instead of fetch here to get progress
            const request = new XMLHttpRequest();
            request.responseType = 'json';
            request.upload.addEventListener('progress', (e) => {
                const percent = e.loaded / e.total * 100;
                pictureEl.setAttribute('state', `uploading ${percent.toFixed(0)}`);
            });
            const targetSetId = parseInt(this.getAttribute('target-set-id') || '');
            let targetSetParam = '';
            if (targetSetId > 0) {
                targetSetParam = `?setId=${targetSetId}`;
            } else if (isNaN(targetSetId) && this.getAttribute('target-set')) {
                targetSetParam = `?setName=${encodeURIComponent(this.getAttribute('target-set')!)}`;
            }
            request.open('POST', `/app/Pictures/Upload/${hash}/${encodeURIComponent(blob.name)}${targetSetParam}`);
            await new Promise<void>((resolve) => {
                request.addEventListener('readystatechange', () => {
                    if (request.readyState === XMLHttpRequest.DONE) {
                        resolve();
                    }
                });
                request.send(blob);
            });
            const response = request.response;

            // handle errors
            if (request.status !== 200) {
                pictureEl.setAttribute('state', 'error');
                console.error(`${request.status} ${request.statusText}`, response);
                if (response && response.title) {
                    throw new Error(response.title);
                } else if (request.status) {
                    throw new Error(`${request.status} ${request.statusText}`);
                } else {
                    throw new Error('Network error');
                }
            }
            
            // replace attributes on picture element
            // endpoint could return an HTML snippet and we could outerHTML it, but this would cause flicker
            pictureEl.setAttribute('state', response.isDuplicate ? 'duplicate' : '');
            pictureEl.setAttribute('picture-id', response.id);
            pictureEl.setAttribute('title', response.title);
            pictureEl.setAttribute('src', response.src);
            pictureEl.setAttribute('fullscreen-url', response.fullscreenUrl);
            
            // allow blob to be released
            URL.revokeObjectURL(oldSrc);
            this.#pictures[index] = parseInt(this.#pictureList.children[index]!.getAttribute('picture-id')!);
            
            // continue to the next possible picture, if any
            this.uploadNextPicture();
        } catch (e) {
            pictureEl.setAttribute('state', 'error');
            this.#uploadButton.classList.remove('loading');
            this.#errorAlert.style.display = '';
            this.#errorMessage.textContent = `${blob.name}: ${(e as Error).message || 'Unknown error'}`;
            this.#isUploading = false;
            this.#hasError = true;
            console.error(e);
        }
    }
};
