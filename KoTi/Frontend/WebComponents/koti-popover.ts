//
// <koti-popover>: popover handling, mostly based on browser popover API.  The popover must have
// id and be opened through popovertarget set on some other element.
//
window.customElements.define('koti-popover', class extends HTMLElement {
    constructor() {
        super();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.getAttribute('popover') !== null) return;
        
        this.setAttribute('popover', 'auto');
        
        // on open, add "alternate-position" (top) class if popover would be offscreen with default (bottom)
        // positioning, otherwise remove it.  Use additional "open" class to make sure popover shows only
        // when already positioned. 
        this.addEventListener('toggle', (e) => {
            const owner = document.querySelector(`[popovertarget="${this.id}"]`);
            if (e.newState !== 'open' || !owner) {
                return;
            }

            this.classList.add('open');
            if (owner.getBoundingClientRect().bottom + this.scrollHeight > window.innerHeight) {
                this.classList.add('alternate-position');
            } else {
                this.classList.remove('alternate-position');
            }
        });

        // on closing, remove "open" class to avoid flickers of incorrect positioning
        this.addEventListener('beforetoggle', (e) => {
            if (e.newState !== 'open') {
                this.classList.remove('open');
            }
        });
    }
})