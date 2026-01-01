///
/// <koti-content-item> web component, handling diplaying a thumbnail for post/article/etc. 
/// and associated behaviors.  Simpler version of <koti-content-item>.
///

window.customElements.define('koti-content-item', class extends HTMLElement {
    /** @type {HTMLButtonElement} */
    #button;
    /** @type {HTMLImageElement} */
    #img;
    /** @type {HTMLSpanElement} */
    #text;

    constructor() {
        super();
    }

    update() {
        // sync UI state with attributes
        const src = this.getAttribute('src');
        const name = this.getAttribute('name');
        const title = this.getAttribute('title');
        const width = this.getAttribute('width');
        const height = this.getAttribute('height');

        this.#text.innerHTML = `${title}<br><span class="muted">${name}</span>`;
        if (src) {
            this.#img.style.display = '';
            this.#img.src = src;
            this.#img.setAttribute('width', width);
            this.#img.setAttribute('height', height);
        } else {
            this.#img.style.display = 'none';
            this.#img.src = '';
        }
    }

    static get observedAttributes() {
        return ['src', 'title', 'width', 'height'];
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (oldValue !== newValue && this.#button) {
            this.update();
        }
    }

    connectedCallback() {
        // create DOM (button with img and span inside)
        this.#button = document.createElement('button');
        this.#button.classList.add('koti-btn', 'item');
        this.#img = document.createElement('img');
        this.#img.setAttribute('loading', 'lazy');
        this.#text = document.createElement('span');
        this.appendChild(this.#button);
        this.#button.appendChild(this.#img);
        this.#button.appendChild(this.#text);
        this.update();

        // bind custom event listeners
        document.addEventListener('koti-content-item:select', this);
        document.addEventListener('koti-content-item:deselect-all', this);

        // bind event listeners to button

        // on click, select/deselect item; if selected, deselect all others
        // and dispatch content:select-insertable event with link
        this.#button.addEventListener('click', (e) => {
            e.preventDefault();
            if (this.#button.classList.contains('selected')) {
                this.#button.classList.remove('selected');
                document.dispatchEvent(new CustomEvent('content:select-insertable'));
            } else {
                document.dispatchEvent(new CustomEvent('koti-content-item:deselect-all'));
                this.#button.classList.add('selected');
                document.dispatchEvent(new CustomEvent('content:select-insertable', 
                    {detail: {text: this.getAttribute('kind') + ':' + this.getAttribute('id')}}));
            }
        });

        // on double click with Ctrl pressed, not only select but also immediately dispatch content:insert event
        // with a link.
        // on double click WITHOUT Ctrl pressed, open edit view in a new tab
        const onInsert = (e) => {
            if (e.ctrlKey) {
                e.preventDefault();
                e.stopPropagation();
                document.dispatchEvent(new CustomEvent('koti-content-item:deselect-all'));
                this.#button.classList.add('selected');
                document.dispatchEvent(new CustomEvent('content:select-insertable',
                    {detail: {text: this.getAttribute('kind') + ':' + this.getAttribute('id')}}));
                document.dispatchEvent(new CustomEvent('content:insert',
                    {detail: {text: this.getAttribute('kind') + ':' + this.getAttribute('id')}}));
            } else {
                window.open(this.getAttribute('edit-url'), '_blank');
            }
        }
        this.#button.addEventListener('dblclick', onInsert);
        this.#button.addEventListener('keydown', (e) => {
            // on left/right, select sibling elements, if possible
            if (e.key === 'ArrowRight' && this.nextElementSibling?.tagName === 'KOTI-CONTENT-ITEM') {
                this.nextElementSibling.firstElementChild.click();
                this.nextElementSibling.firstElementChild.focus();
            }
            if (e.key === 'ArrowLeft' && this.previousElementSibling?.tagName === 'KOTI-CONTENT-ITEM') {
                this.previousElementSibling.firstElementChild.click();
                this.previousElementSibling.firstElementChild.focus();
            }

            // on Enter same as on double click
            if (e.key === 'Enter') {
                onInsert(e);
            }
        });
    }

    disconnectedCallback() {
        document.removeEventListener('koti-content-item:select', this);
        document.removeEventListener('koti-content-item:deselect-all', this);
    }

    handleEvent(e) {
        if (e.type === 'koti-content-item:select') {
            this.onSelect(e);
        } else if (e.type === 'koti-content-item:deselect-all') {
            this.onDeselectAll();
        }
    }

    /** @param {CustomEvent<{kind: string, id: number}>} e */
    onSelect(e) {
        if (e.detail.kind === this.getAttribute('kind') && e.detail.id.toString() === this.getAttribute('id')) {
            this.#button.classList.add('selected');
            document.dispatchEvent(new CustomEvent('content:select-insertable',
                {detail: {text: this.getAttribute('kind') + ':' + this.getAttribute('id')}}));
        }
    }

    onDeselectAll() {
        this.#button.classList.remove('selected');
    }
})

