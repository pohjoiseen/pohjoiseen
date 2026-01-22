//
// <koti-rating>: rating component, 1-5 clickable stars.
//
export default class RatingElement extends HTMLElement {
    #internals: ElementInternals;
    #value = 0;
    #initialized = false;
    
    constructor() {
        super();
        this.#internals = this.attachInternals();
    }
    
    connectedCallback() {
        if (this.#initialized) return;
        this.#initialized = true;
        
        // create star spans
        this.#value = parseInt(this.getAttribute('value') ?? '0');
        this.#internals.setFormValue(this.#value.toString());
        for (let i = 0; i < 5; i++) {
            const star = document.createElement('span');
            star.dataset.index = (i + 1).toString();
            star.classList.add('bi', this.#value > i ? 'bi-star-fill' : 'bi-star');
            this.appendChild(star);
        }
        
        // change on click
        this.addEventListener('click', (e) => {
            const target = e.target as HTMLElement;
            if (target.dataset.index !== undefined) {
                let newValue = parseInt(target.dataset.index);
                if (newValue === this.#value) newValue = 0;
                this.value = newValue;
            }
        });
    }
    
    attributeChangedCallback(name: string, oldValue: string | null, newValue: string | null) {
        if (name === 'value' && this.#initialized && oldValue !== newValue) {
            this.value = parseInt(newValue ?? '0');
        }
    }
    
    get value(): number { return this.#value; }
    set value(value: number) {
        this.#value = value;
        this.#internals.setFormValue(value.toString());
        this.setAttribute('value', value.toString());
        for (let i = 0; i < 5; i++) {
            const star = this.children[i] as HTMLElement;
            if (this.#value > i) {
                star.classList.remove('bi-star');
                star.classList.add('bi-star-fill');
            } else {
                star.classList.remove('bi-star-fill');
                star.classList.add('bi-star');
            }
        }
    }
    
    static get observedAttributes() { return ['value']; }
    static formAssociated = true;
};
