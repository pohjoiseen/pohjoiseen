//
// <koti-post-geo>: geo tab for post form, handles adding/removing subforms
//
import htmx from 'htmx.org';

window.customElements.define('koti-post-geo', class extends HTMLElement {
    #container: HTMLElement = null!;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#container) return;
        
        this.#container = this.querySelector('.geo')!;
        
        // add button requests an empty form from server and adds it to the end of the list
        this.querySelector('.add-geo-btn')?.addEventListener('click', () => {
            htmx.ajax('get', '/app/Posts/Geo', {
                values: { index: this.countSubforms() },
                target: this.#container,
                swap: 'beforeend'
            });
        });
        
        // remove button deletes subform and renumbers remaining ones
        this.addEventListener('click', (e) => {
            if (e.target instanceof HTMLButtonElement && e.target.classList.contains('remove-geo-btn')) {
                if (!confirm('Really delete this geo point?')) return;
                
                e.target.closest('fieldset')?.nextElementSibling?.remove();  // <hr>
                e.target.closest('fieldset')?.remove(); // subform itself
                
                // replace name, id, for atributes on <input>, <textarea>, <label> and also <koti-location-picker>
                // (the latter has id which is important for hx-preserve).  This is a bit hacky, especially since we are
                // changing these attributes from under a few other web components, but should work fine.
                this.querySelectorAll('.geo > fieldset').forEach((subformEl, i) => {
                    subformEl.querySelectorAll('input, textarea, label, koti-location-picker').forEach((el) => {
                        const name = el.getAttribute('name');
                        if (name) el.setAttribute('name', name.replace(/^Geo\[\d+]/, 'Geo[' + i + ']'));
                        const id = el.getAttribute('id');
                        if (id) el.setAttribute('id', id.replace(/-\d+$/, '-' + i));
                        const labelFor = el.getAttribute('for');
                        if (labelFor) el.setAttribute('for', labelFor.replace(/-\d+$/, '-' + i));
                    });
                });
            }
        })
    }
    
    countSubforms = () => this.querySelectorAll('.geo > fieldset').length;
});
