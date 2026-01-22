//
// <koti-place-metadata>: metadata tab for place form, handles adding (=unhiding) fields
//
export default class PlaceMetadataElement extends HTMLElement {
    #select: HTMLSelectElement = null!;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        if (this.#select) return;

        this.#select = this.querySelector('.add-field-select') as HTMLSelectElement;
        this.#select.addEventListener('change', (e) => {
            this.querySelector(`[data-meta="${this.#select.value}"]`)?.removeAttribute('hidden');
            this.#select.querySelector(`option[value="${this.#select.value}"]`)?.setAttribute('hidden', 'hidden');
            this.#select.value = '';
        });
    }
};
