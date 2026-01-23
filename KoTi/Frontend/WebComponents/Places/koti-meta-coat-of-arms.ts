//
// <koti-meta-coat-of-arms>: handles uploads/refreshing of a single coat of arms
// in place meta pane.  Not really configurable, just one-off hooking up of
// a few events.
//
export default class MetaCoatOfArmsElement extends HTMLElement {
    #initialized = false;
    
    constructor() {
        super();
    }

    connectedCallback() {
        if (this.#initialized) return;
        this.#initialized = true;
        
        this.querySelector('.upload-btn')?.addEventListener('click',
            () => this.querySelector('dialog')!.showModal());
        this.querySelector('dialog')?.addEventListener('picture-upload-modal:selected', (e) => {
            const event = e as CustomEvent<{pictureId: number}>;
            if (event.detail.pictureId) {
                this.querySelector('input')?.setAttribute('value', event.detail.pictureId.toString());
                this.dispatchEvent(new Event('koti-meta-coat-of-arms:reload'));
            }
        });
    }
};
