///
/// <koti-post-coats-of-arms>: coats of arms form interactivity (add/remove buttons)
///
import htmx from 'htmx.org';

window.customElements.define('koti-post-coats-of-arms', class extends HTMLElement {
    #uploadModal: HTMLElement = null!;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        this.#uploadModal = this.querySelector('koti-picture-upload-modal')!;
        this.addEventListener('click', this);
        this.#uploadModal.addEventListener('picture-upload-modal:selected', (e) => this.onAdded((e as CustomEvent<{pictureId: number}>).detail.pictureId));
    }

    handleEvent(e: Event) {
        if (e.type === 'click') {
            const target = e.target as HTMLElement;
            if (target instanceof HTMLButtonElement && target.classList.contains('remove-coa-btn')) {
                this.onRemove(parseInt(target.getAttribute('data-index')!));
            } else if (target instanceof HTMLElement && target.classList.contains('add-coa-btn')) {
                this.onAdd();
            }
        }
    }
    
    onAdd() {
        this.#uploadModal.dispatchEvent(new CustomEvent('picture-upload-modal:open'));
    }
    
    onAdded(pictureId: number) {
        htmx.ajax('get', '/app/Posts/CoatOfArms', { 
            values: { url: 'picture:' + pictureId.toString(), index: this.querySelectorAll('.post-coatofarms').length },
            target: this.querySelector('.coats-of-arms')!,
            swap: 'beforeend'
        });
    }

    onRemove(index: number) {
        if (!confirm('Really delete this coat of arms?  The picture will remain uploaded.')) return;
        this.querySelector(`.post-coatofarms[data-index="${index}"] + hr`)?.remove();
        this.querySelector(`.post-coatofarms[data-index="${index}"]`)?.remove();
        // carefully reindex remaining elements
        this.querySelectorAll('.post-coatofarms').forEach((el, i) => {
            el.setAttribute('data-index', i.toString());
            el.querySelector('.remove-coa-btn')?.setAttribute('data-index', i.toString());
            el.querySelector('input[type=hidden]')?.setAttribute('name', `CoatsOfArms[${i}][Url]`);
            el.querySelector('input[type=number]')?.setAttribute('name', `CoatsOfArms[${i}][Size]`);
            el.querySelector('input[type=number]')?.setAttribute('id', `coat-of-arms-size-${i}`);
            el.querySelector('label')?.setAttribute('for', `coat-of-arms-size-${i}`);
        });
    }
});
