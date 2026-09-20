//
// <koti-redirect-form>: glue for "add redirect" page.  When target is a post or an article, catches selection
// in the post/article list and stores it into the hidden target input.
//
export default class RedirectFormElement extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        const formEl = this.querySelector('form')!;
        const targetInputEl = this.querySelector('input[type=hidden][name=urlTo]') as HTMLInputElement | null;
        const selectedTargetEl = this.querySelector('.selected-target');
        const saveButtonEl = document.getElementById('redirect-save-button') as HTMLButtonElement;
        
        // nothing to do for plain URLs
        if (!targetInputEl) return;

        // on click etc. on a post/article, remember it and enable/disable Save button
        this.addEventListener('content:select-insertable', (e) => {
            const text = (e as CustomEvent<{text: string}>).detail?.text || '';
            targetInputEl.value = text;
            saveButtonEl.disabled = !text;
            if (selectedTargetEl) {
                const title = this.querySelector('koti-content-item .selected')?.parentElement?.getAttribute('title');
                selectedTargetEl.textContent = text ? `Selected: ${title || text}` : '';
            }
        });

        // on double click/Enter with Ctrl, also submit right away
        this.addEventListener('content:insert', () => formEl.requestSubmit());
    }
};
