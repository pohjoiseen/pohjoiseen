///
/// <koti-picture> web component, handling diplaying a picture thumbnail (both for picture list and picture
/// uploader) and associated behaviors.
///

window.customElements.define('koti-picture', class extends HTMLElement {
    /** @type {HTMLButtonElement} */
    #button;
    /** @type {HTMLImageElement} */
    #img;
    /** @type {HTMLSpanElement} */
    #text;
    /** @type {HTMLElement} */
    #parent;
    
    constructor() {
        super();
    }
    
    update() {
        // sync UI state with attributes
        const state = this.getAttribute('state');
        const src = this.getAttribute('src');
        const title = this.getAttribute('title');
        const fullscreenUrl = this.getAttribute('fullscreen-url');
        const width = this.getAttribute('width');
        const height = this.getAttribute('height');
        
        this.#button.classList.toggle('pending', state === 'pending');
        this.#button.classList.toggle('dup', state === 'duplicate');
        this.#button.classList.toggle('error', state === 'error');
        this.#button.classList.toggle('uploading', state?.startsWith('uploading') || false);
        if (fullscreenUrl) {
            this.#button.setAttribute('hx-get', fullscreenUrl);
        } else {
            this.#button.removeAttribute('hx-get');
        }
        this.#button.setAttribute('title', title);
        this.#img.src = src;
        this.#img.setAttribute('width', width);
        this.#img.setAttribute('height', height);
        if (state === 'duplicate') {
            this.#text.textContent = 'DUP';
        } else if (state?.startsWith('uploading')) {
            const percent = state.split(' ')[1];
            this.#text.textContent = `${percent}%`;
        } else if (state === 'error') {
            this.#text.innerHTML = '<i class="bi bi-exclamation-triangle"></i>';
        } else {
            this.#text.textContent = '';
        }
        
        // make sure to reprocess htmx changes
        htmx.process(this.#button);
    }
    
    static get observedAttributes() {
        return ['state', 'src', 'title', 'fullscreen-url', 'width', 'height'];
    }
    
    attributeChangedCallback(name, oldValue, newValue) {
        if (oldValue !== newValue && this.#button) {
            this.update();
        }
    }
    
    connectedCallback() {
        // create DOM (button with img and span inside)
        this.#parent = this.parentElement;
        this.#button = document.createElement('button');
        this.#button.classList.add('koti-btn', 'picture');
        this.#button.setAttribute('hx-target', '#picture-fullscreen-container');
        this.#button.setAttribute('hx-trigger', 'dblclick[!ctrlKey], keydown[key===\'Enter\']');
        this.#button.setAttribute('hx-swap', 'innerHTML');
        this.#button.setAttribute('hx-vals', 'js:{"overrideIds":this.parentNode.getFullscreenOverrideIds()}');
        this.#img = document.createElement('img');
        this.#img.setAttribute('loading', 'lazy');
        this.#text = document.createElement('span');
        this.appendChild(this.#button);
        this.#button.appendChild(this.#img);
        this.#button.appendChild(this.#text);
        this.update();

        // bind custom event listeners
        this.#parent.addEventListener('koti-picture:select', this);
        this.#parent.addEventListener('koti-picture:deselect-all', this);
        
        // bind event listeners to button
        
        // on click, select/deselect picture; if selected, deselect all others
        // and dispatch content:select-insertable event with picture link
        this.#button.addEventListener('click', (e) => {
            e.preventDefault();
            if (this.#button.classList.contains('selected')) {
                this.#button.classList.remove('selected');
                this.#parent.dispatchEvent(new CustomEvent('content:select-insertable', {bubbles: true}));
            } else {
                this.#parent.dispatchEvent(new CustomEvent('koti-picture:deselect-all'));
                this.#button.classList.add('selected');
                if (this.getAttribute('picture-id')) {
                    this.#parent.dispatchEvent(new CustomEvent('content:select-insertable', {bubbles: true, detail: {text: 'picture:' + this.getAttribute('picture-id')}}));
                }
            }
        });

        // on double click with Ctrl pressed, not only select but also immediately dispatch content:insert event
        // with picture link.
        // on double click WITHOUT Ctrl pressed, we will open fullscreen view, but that is handled through htmx,
        // see hx-* attributes assignment above
        const onInsert = (e) => {
            if (e.ctrlKey) {
                e.preventDefault();
                e.stopPropagation();
                this.#parent.dispatchEvent(new CustomEvent('koti-picture:deselect-all'));
                this.#button.classList.add('selected');
                if (this.getAttribute('picture-id')) {
                    this.#parent.dispatchEvent(new CustomEvent('content:select-insertable', {bubbles: true, detail: {text: 'picture:' + this.getAttribute('picture-id')}}));
                    this.#parent.dispatchEvent(new CustomEvent('content:insert', {bubbles: true, detail: {text: 'picture:' + this.getAttribute('picture-id')}}));
                }
            }
        }
        this.#button.addEventListener('dblclick', (e) => {
            if (e.ctrlKey) {
                onInsert(e);
            }
        });
        
        this.#button.addEventListener('keydown', (e) => {
            // on left/right, select sibling pictures, if possible
            if (e.key === 'ArrowRight' && this.nextElementSibling?.tagName === 'KOTI-PICTURE') {
                this.nextElementSibling.firstElementChild.click();
                this.nextElementSibling.firstElementChild.focus();
            }
            if (e.key === 'ArrowLeft' && this.previousElementSibling?.tagName === 'KOTI-PICTURE') {
                this.previousElementSibling.firstElementChild.click();
                this.previousElementSibling.firstElementChild.focus();
            }
            
            // on Enter with Ctrl pressed, same as on double click with Ctrl pressed
            if (e.key === 'Enter' && e.ctrlKey) {
                onInsert(e);
            }
        });
    }
    
    disconnectedCallback() {
        this.#parent.removeEventListener('koti-picture:select', this);
        this.#parent.removeEventListener('koti-picture:deselect-all', this);
    }

    getFullscreenOverrideIds() {
        // for opening fullscreen view, allow to specify order as just ids of all siblings
        if (this.getAttribute('fullscreen-manual-order')) {
            return [...this.parentElement.children]
                .filter(el => el.tagName === 'KOTI-PICTURE' && el.getAttribute('picture-id'))
                .map(el => el.getAttribute('picture-id'))
                .join(',');
        }
        return '';
    }
    
    handleEvent(e) {
        if (e.type === 'koti-picture:select') {
            this.onSelect(e);
        } else if (e.type === 'koti-picture:deselect-all') {
            this.onDeselectAll();
        }
    }
    
    /** @param {CustomEvent<{id: number}>} e */
    onSelect(e) {
        if (e.detail.id.toString() === this.getAttribute('picture-id')) {
            this.#button.classList.add('selected');
            if (this.getAttribute('picture-id')) {
                this.#parent.dispatchEvent(new CustomEvent('content:select-insertable', {bubbles: true, detail: {text: 'picture:' + this.getAttribute('picture-id')}}));
            }
        }
    }
    
    onDeselectAll() {
        this.#button.classList.remove('selected');
    }
})

