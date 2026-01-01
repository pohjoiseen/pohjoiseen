///
/// <koti-picture-uploader>: friendly picture upload panel.  This is implemented entirely in JS without htmx,
/// with the basic unit being <koti-picture> also used in server-side picture picker.  This uploads pictures
/// one by one, from file input, drag and drop, or pasting, shows progress and handles errors, allows retry in case
/// of errors, allows to view uploaded pictures in fullscreen.
///
window.customElements.define('koti-picture-uploader', class extends HTMLElement {
    /** @type {(File | number)[]} list of blobs to be uploaded or picture ids already uploaded */
    #pictures = [];
    #isUploading = false;
    
    // DOM elements
    #pictureList;
    #uploadButton;
    #errorAlert;
    #errorMessage;
    /** @type {HTMLInputElement} */
    #uploadInput;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        // add DOM and store important elements
        this.innerHTML = `
            <div class="picture-uploader">
                <div class="picture-list">
                    <h2 class="placeholder-heading">Paste or drop photos here</h2>            
                </div>
                <div class="footer">
                    <div class="alert" style="display: none; margin-bottom: 5px;">
                        <span>Error uploading photo: <span class="error-message"></span></span>
                        <button class="koti-btn primary retry-button">Retry</button>
                    </div>
                    <button class="koti-btn primary upload-button with-indicator">
                        <span class="spinner"></span>
                        <i class="bi bi-upload"></i> Choose photo(s) to upload...
                    </button>
                    <input type="file" name="files" accept="image/jpeg, image/png" multiple="multiple" hidden="hidden" class="upload-hidden-button">
                    <div style="margin-top: 5px;">
                        Uploaded pictures will go to <b>${this.getAttribute('target-set')}</b> folder.
                    </div>
                </div>
            </div>
        `;
        this.#pictureList = this.querySelector('.picture-list');
        this.#uploadButton = this.querySelector('.upload-button');
        this.#uploadInput = this.querySelector('.upload-hidden-button');
        this.#errorAlert = this.querySelector('.alert');
        this.#errorMessage = this.querySelector('.error-message');
        this.querySelector('.retry-button').addEventListener('click', () => this.uploadNextPicture());
        
        // upload through file input
        this.querySelector('.upload-button').addEventListener('click', () => this.#uploadInput.click());
        this.#uploadInput.addEventListener('input', (e) => {
            for (const file of [...this.#uploadInput.files]) {
                this.addPicture(file);
            }
        });
        
        // upload through paste
        this.#pictureList.addEventListener('paste', (e) => {
            const files = [...e.clipboardData.items].map(i => i.getAsFile());
            for (const file of files) {
                if (file) {
                    this.addPicture(file);
                }
            }
        });
        
        // upload through drag and drop
        this.#pictureList.addEventListener('dragenter', (e) => {
            e.preventDefault();
            this.#pictureList.classList.add('drop-hover');
        });
        this.#pictureList.addEventListener('dragover', (e) => {
            e.preventDefault();
        });
        this.#pictureList.addEventListener('dragleave', (e) => {
            e.preventDefault();
            this.#pictureList.classList.remove('drop-hover');
        });
        this.#pictureList.addEventListener('drop', (e) => {
            e.preventDefault();
            this.#pictureList.classList.remove('drop-hover');
            const files = [...e.dataTransfer.items].map(i => i.getAsFile());
            for (const file of files) {
                if (file) {
                    this.addPicture(file);
                }
            }
        });
    }

    /**
     * Enqueues a picture for upload.
     * @param {File} file
     */
    addPicture(file) {
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
        
        if (!this.#isUploading) {
            this.uploadNextPicture();
        }
    }

    /**
     * Picks next not yet uploaded picture, if any, and tries to upload it.
     */
    uploadNextPicture() {
        const index = this.#pictures.findIndex(blob => typeof blob !== 'number');
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
    async uploadPicture(index) {
        const blob = this.#pictures[index];
        const pictureEl = this.#pictureList.children[index];
        const oldSrc = pictureEl.getAttribute('src');

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
            request.open('POST', `/app/Pictures/Upload/${hash}/${blob.name}?setName=${encodeURIComponent(this.getAttribute('target-set'))}`);
            await new Promise((resolve) => {
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
            this.#pictures[index] = parseInt(this.#pictureList.children[index].getAttribute('picture-id'));
            
            // continue to the next possible picture, if any
            this.uploadNextPicture();
        } catch (e) {
            pictureEl.setAttribute('state', 'error');
            this.#uploadButton.classList.remove('loading');
            this.#errorAlert.style.display = '';
            this.#errorMessage.textContent = e.message;
            this.#isUploading = false;
            console.error(e);
        }
    }
});
