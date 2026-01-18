//
// <koti-geo-external-links>: links list in geo tab for post, handles add/remove rows
// TODO: should allow picking post link properly (in a modal) but whatever, this is a little used feature
//
window.customElements.define('koti-geo-external-links', class extends HTMLElement {
    #initialized = false;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#initialized) {
            return;
        }
        this.#initialized = true;
        
        this.querySelector('.add-btn')?.addEventListener('click', () => {
            const numRows = this.querySelectorAll('.remove-btn').length;
            const prefix = this.getPrefix();
            if (!prefix) return;
            
            // create new elements in a temporary div and move them to before the add button parent div.
            // this needs to be kept in sync with _Geo.cshtml of course...
            const newRow = `
                <div>
                    <input type="text" class="path" name="${prefix}[Links][${numRows}][Path]" placeholder="URL (post:123, https://...)" />
                </div>
                <div>
                    <input type="text" class="label" name="${prefix}[Links][${numRows}][Label]" placeholder="Text" />
                </div>
                <div>
                    <button type="button" class="koti-btn remove-btn">Remove</button>
                </div>
            `;
            const divEl = document.createElement('div');
            divEl.innerHTML = newRow;
            
            const insertWhere = this.querySelector('.add-btn')!.parentElement!;
            for (const child of [...divEl.children]) {
                insertWhere.before(child);
            }
        });
        
        this.addEventListener('click', (e) => {
            if (e.target instanceof HTMLButtonElement && e.target.classList.contains('remove-btn')) {
                if (!confirm('Really delete this link?')) return;
                
                // on click on remove button, remove the whole row (three divs)
                const parent = e.target.parentElement!;
                parent.previousElementSibling!.previousElementSibling!.remove();
                parent.previousElementSibling!.remove();
                parent.remove();

                // renumber remaining rows
                const prefix = this.getPrefix();
                this.querySelectorAll('.path').forEach((el, i) =>
                    (el as HTMLInputElement).setAttribute('name', `${prefix}[Links][${i}][Path]`));
                this.querySelectorAll('.label').forEach((el, i) =>
                    (el as HTMLInputElement).setAttribute('name', `${prefix}[Links][${i}][Label]`));
            }
        });
    }
    
    private getPrefix(): string {
        // XXX hacky way to determine the index of this geo point: go up to fieldset and find any named input
        const anyInput = this.closest('fieldset')?.querySelector('input');
        if (!anyInput) return '';
        const match = anyInput.getAttribute('name')!.match(/^Geo\[(\d+)]/);
        if (!match) return '';
        return match[0];
    }
});