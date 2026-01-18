//
// <koti-dnd-order>: drag-and-drop order handling
//
import { reindexElements } from '../Common/common.ts';

window.customElements.define('koti-dnd-order', class extends HTMLElement {
    #initialized = false;
    #draggedEl: HTMLElement | null = null;
    #blankImg: HTMLImageElement;
    
    constructor() {
        super();
        this.#blankImg = new Image();
        this.#blankImg.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAUEBAAAACwAAAAAAQABAAACAkQBADs=';
    }

    connectedCallback() {
        if (this.#initialized) {
            return;
        }
        this.#initialized = true;
        
        // all children of this element can be dragged around
        this.addEventListener('dragstart', (e) => {
            // find the immediate child that is being dragged
            let draggedEl = e.target as HTMLElement;
            while (draggedEl.parentElement !== this) {
                if (!draggedEl.parentElement) {
                    return;
                }
                draggedEl = draggedEl.parentElement;
            }
            
            // store the original index in dataTransfer (as type), store the actual element in this object instance
            // use blank gif as drag image (i.e. no image)
            e.dataTransfer?.setDragImage(this.#blankImg, 0, 0);
            const index = [...draggedEl.parentElement!.children].indexOf(draggedEl);
            e.dataTransfer?.items.add('dummy', 'dnd-order/' + index);
            this.#draggedEl = draggedEl;
            this.#draggedEl.classList.add('dragging');
        });
        
        // handle actual reorder not on drop but on dragover
        this.addEventListener('dragover', (e) => {
            e.preventDefault();
            
            // restore dragged element orignal index from dataTransfer (from type)
            const item = [...e.dataTransfer!.items].find(i => i.type.startsWith('dnd-order/'));
            if (!item) return;
            const draggedIndex = parseInt(item.type.substring('dnd-order/'.length));

            // find index of the element being dragged over
            let underCursorEl = e.target as HTMLElement;
            while (underCursorEl.parentElement !== this) {
                if (!underCursorEl.parentElement) {
                    return;
                }
                underCursorEl = underCursorEl.parentElement;
            }
            let underCursorIndex = [...underCursorEl.parentElement!.children].indexOf(underCursorEl);

            if (draggedIndex === underCursorIndex) {
                return;
            }
            
            // make behavior more natural, breakpoints should be in the middle of elements being dragged over
            // rather than at the top of them
            const offsetOnUnderCursorEl = e.clientY - underCursorEl.getBoundingClientRect().top;
            if (draggedIndex > underCursorIndex && offsetOnUnderCursorEl < underCursorEl.offsetHeight / 2) {
                underCursorIndex--;
                underCursorEl = underCursorEl.previousElementSibling as HTMLElement;
                if (!underCursorEl) {
                    // dragged to the beginning
                    this.insertAdjacentElement('afterbegin', this.#draggedEl!);
                    return;
                }
            }
            if (draggedIndex < underCursorIndex && offsetOnUnderCursorEl > underCursorEl.offsetHeight / 2) {
                underCursorIndex++;
                underCursorEl = underCursorEl.nextElementSibling as HTMLElement;
                if (!underCursorEl) {
                    // dragged to the end
                    this.insertAdjacentElement('beforeend', this.#draggedEl!);
                    return;
                }
            }
            
            // move element
            underCursorEl.insertAdjacentElement(draggedIndex > underCursorIndex ? 'afterend' : 'beforebegin', this.#draggedEl!);
        });
        
        this.addEventListener('dragend', () => {
            this.#draggedEl?.classList.remove('dragging');
            this.#draggedEl = null;
            
            const prefix = this.getAttribute('field-prefix');
            if (prefix) {
                [...this.children].forEach((child, i) => reindexElements(child as HTMLElement, prefix, i));
            }
        });
    }
});
