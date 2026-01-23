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
        
        this.querySelectorAll('[data-meta]').forEach((el) => {
            // make sure hidden fields also are disabled (so they won't submit anything)
            if (el.getAttribute('hidden')) {
                el.setAttribute('disabled', 'disabled');
            }
            
            // create a remove button for every field
            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
            removeBtn.className = 'remove-btn koti-btn danger';
            el.appendChild(removeBtn);
        });
        
        // remove button puts hidden and disabled back on fields
        this.addEventListener('click', (e) => {
            let target = e.target as HTMLElement;
            if (target.parentElement?.classList.contains('remove-btn')) {
                target = target.parentElement;    
            }
            
            if (target.classList.contains('remove-btn')) {
                target.parentElement!.setAttribute('disabled', 'disabled');
                target.parentElement!.setAttribute('hidden', 'hidden');
                this.#select.querySelector(`option[value="${target.parentElement?.dataset.meta}"]`)?.removeAttribute('hidden');
            }
        });

        // picking a field from select removes hidden/disabled on the field and hides the option instead
        this.#select = this.querySelector('.add-field-select') as HTMLSelectElement;
        this.#select.addEventListener('change', (e) => {
            const target = this.querySelector(`[data-meta="${this.#select.value}"]`);
            target?.removeAttribute('hidden');
            target?.removeAttribute('disabled');
            this.#select.querySelector(`option[value="${this.#select.value}"]`)?.setAttribute('hidden', 'hidden');
            this.#select.value = '';
        });
    }
};
