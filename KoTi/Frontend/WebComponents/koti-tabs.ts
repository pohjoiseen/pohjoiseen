//
// <koti-tabs>: tab component, must include one or more <koti-tab> children.  Handles storing selected tab state
// across page reloads.  <koti-tab> elements must have "name" attribute, one of them may be "selected".
// Currently tabs must be defined statically, won't be updated if anything changes.
//
import { generateId } from '../Common/common.ts';

window.customElements.define('koti-tabs', class extends HTMLElement {
    #id: string = '';
    #tabs: { [name: string]: HTMLElement } = {};
    #selected: string = '';
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#id) return;
        
        this.#id = this.getAttribute('component-id') || generateId();
        const fieldName = this.getAttribute('field-name');
        const localStorageKey = this.getAttribute('component-id') ? window.location.pathname + '#tabs#' + this.#id : null;
        
        // store tabs
        for (const child of this.children) {
            if (child.tagName !== 'KOTI-TAB') continue;
            if (!child.getAttribute('name')) {
                throw new Error('koti-tab must have name attribute');
            }
            this.#tabs[child.getAttribute('name')!] = child as HTMLElement;
            if (child.getAttribute('selected')) {
                this.#selected = child.getAttribute('name')!;
            }
        }
        if (!Object.entries(this.#tabs).length) {
            throw new Error('koti-tabs must contain at least one koti-tab child');
        }
        if (!this.#selected) {
            this.#selected = Object.keys(this.#tabs)[0]!;
        }

        // recall last selected tab from localStorage, if possible
        if (localStorageKey) {
            const savedValue = window.localStorage.getItem(localStorageKey);
            if (savedValue && savedValue in this.#tabs) {
                this.#selected = savedValue;
            }
        }

        // wrap into appropriate markup
        for (const [name, tab] of Object.entries(this.#tabs)) {
            const nameForId = name.replace(/[^a-zA-Z0-9]/g, '-');
            // div wrapping tab header and content
            const wrapperEl = document.createElement('div');
            // hidden radio input which actually selects the tab
            const inputEl = document.createElement('input');
            inputEl.type = 'radio';
            inputEl.name = fieldName || this.#id;
            inputEl.id = `${this.#id}-${nameForId}`;
            if (name === this.#selected) inputEl.setAttribute('checked', 'checked');
            // on change update local storage.  Actually displaying the tab is handled purely through CSS
            inputEl.addEventListener('change', (e) => {
                if (e.target instanceof HTMLInputElement && e.target.checked) {
                    this.#selected = name;
                }
                if (localStorageKey) {
                    window.localStorage.setItem(localStorageKey, name);
                }
            });
            wrapperEl.appendChild(inputEl);
            // tab header button
            const labelEl = document.createElement('label');
            labelEl.setAttribute('for', inputEl.id);
            labelEl.textContent = name;
            labelEl.tabIndex = 0;
            wrapperEl.appendChild(labelEl);
            // wrap tab content.  This will cause connectedCallback() on children web components to re-fire!
            tab.replaceWith(wrapperEl);
            wrapperEl.appendChild(tab);
        }
    }
});