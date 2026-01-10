///
/// <koti-fullscreen-picture>: container for fullscreen pictures.  Meant to be only one.
/// Fullscreen here is not Fullscreen API, but not basic overlay other, rather a modal dialog.
///
import htmx from 'htmx.org';

window.customElements.define('koti-fullscreen-picture', class extends HTMLElement {
    constructor() {
        super();
    }
    
    connectedCallback() {
        const dialogEl = document.createElement('dialog');
        dialogEl.id = 'fullscreen-container';
        this.appendChild(dialogEl);
        
        // on a certain event on document, open dialog as modal and load into it whatever is requested.
        // subsequent interactions are handled from htmx
        document.addEventListener('koti-fullscreen-picture:start-fullscreen', (e) => {
            const customEvent = e as CustomEvent<{src: string}>;
            dialogEl.showModal();
            htmx.ajax('get', customEvent.detail.src, dialogEl);
        });
        
        // Esc or click dismisses fullscreen mode
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && dialogEl.open) {
                dialogEl.close();
            }
        });
        dialogEl.addEventListener('click', () => {
            if (dialogEl.open) {
                dialogEl.close();
            }
        });
    }
})