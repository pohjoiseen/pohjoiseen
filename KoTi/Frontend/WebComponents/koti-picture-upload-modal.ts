//
// <koti-picture-upload-modal>: single picture uploader modal, wraps <koti-picture-uploader>.
//
import htmx from 'htmx.org';

window.customElements.define('koti-picture-upload-modal', class extends HTMLElement {
    #selectedPictureId: number | null = null;
    
    constructor() {
        super();
    }

    connectedCallback() {
        const dialogEl = this.querySelector('dialog')!;
        const selectButtonEl = dialogEl.querySelector('.select-btn')! as HTMLButtonElement;
        
        this.addEventListener('picture-upload-modal:open', () => dialogEl.showModal());

        // on click etc. on a picture, enable/disable "Select" button and set text in it, set hidden input value
        dialogEl.addEventListener('content:select-insertable', (e) => {
            const customEvent = e as CustomEvent<{text: string}>;
            e.stopPropagation();
            if (customEvent.detail?.text) {
                const id = customEvent.detail.text.replace('picture:', '');
                this.#selectedPictureId = parseInt(id);
                selectButtonEl.textContent = 'Select: ' + id;
                selectButtonEl.disabled = false;
            } else {
                selectButtonEl.textContent = 'Select';
                selectButtonEl.disabled = true;
            }
        });

        // on double click etc. on a picture, same and emulate click on Select button
        dialogEl.addEventListener('content:select', (e) => {
            const customEvent = e as CustomEvent<{text: string}>;
            e.stopPropagation();
            const id = customEvent.detail.text.replace('picture:', '');
            this.#selectedPictureId = parseInt(id);
            selectButtonEl.textContent = 'Select: ' + id;
            selectButtonEl.disabled = false;
            selectButtonEl.click();
        });

        // on click on "Select" button simply close dialog and submit picker, value should be already saved in hidden input
        selectButtonEl.addEventListener('click', () => {
            dialogEl.close();
            htmx.trigger(dialogEl, 'picture-upload-modal:selected', { pictureId: this.#selectedPictureId });
        });
        // on click on "Cancel" button close dialog and don't do anything else
        dialogEl.querySelector('.cancel-btn')!.addEventListener('click', () => dialogEl.close());
    }
})