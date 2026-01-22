//
// <koti-post-geo>: geo tab for post form, handles adding/removing subforms
//
import htmx from 'htmx.org';
import { reindexElements } from '../../Common/common.ts';

export default class PostGeoElement extends HTMLElement {
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
                
                this.querySelectorAll('.geo > fieldset').forEach((subformEl, i) => 
                    reindexElements(subformEl as HTMLElement, 'Geo', i));
            }
        })
    }
    
    countSubforms = () => this.querySelectorAll('.geo > fieldset').length;
};
