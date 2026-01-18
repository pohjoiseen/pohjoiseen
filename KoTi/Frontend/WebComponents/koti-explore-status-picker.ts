//
// <koti-explore-status-picker>: popover to pick explore status (green-yellow-orange-red).
//
import { generateId } from '../Common/common.ts';

const NONE = 0;
const MINIMAL = 1;
const IN_PROGRESS = 2;
const SUFFICIENT = 3;

window.customElements.define('koti-explore-status-picker', class extends HTMLElement {
    #id: string;
    #hiddenInputEl: HTMLInputElement = null!;
    #buttonColorBoxEl: HTMLSpanElement = null!;
    #buttonTextEl: HTMLSpanElement = null!;
    #popoverEl: HTMLDivElement = null!;
    
    readonly #COLORS: { [key: number]: string} = {
        [NONE]: '#DC3545',
        [MINIMAL]: '#FD7E14',
        [IN_PROGRESS]: '#FFC107',
        [SUFFICIENT]: '#198754'
    };
    
    readonly #TEXTS: { [key: number]: string} = {
        [NONE]: 'Not visited',
        [MINIMAL]: 'Visited briefly but not explored',
        [IN_PROGRESS]: 'Partially explored',
        [SUFFICIENT]: 'Sufficiently explored'
    };
    
    constructor() {
        super();
        this.#id = generateId();
    }

    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#hiddenInputEl) return;

        const initialValue = parseInt(this.getAttribute('value') || NONE.toString());
        this.innerHTML = `
            <input type="hidden" name="${this.getAttribute('name')}" value="${initialValue}" />
            <button type="button" class="koti-btn open-btn" popovertarget="${this.#id}-popover">
                <span class="color-box" style="background-color: ${this.#COLORS[initialValue]}"></span>
                <span class="text">${this.#TEXTS[initialValue]}</span>
            </button>
            <koti-popover id="${this.#id}-popover">
                <div>
                    ${[NONE, MINIMAL, IN_PROGRESS, SUFFICIENT].map((i) => `
                        <button type="button" class="koti-btn select-btn" data-value="${i}">
                            <span class="color-box" style="background-color: ${this.#COLORS[i]}"></span>
                            <span>${this.#TEXTS[i]}</span>
                        </button>
                    `).join('\n')}
                </div>
            </koti-popover>
        `;
        this.#hiddenInputEl = this.querySelector('input')!;
        this.#buttonColorBoxEl = this.querySelector('.open-btn .color-box')!;
        this.#buttonTextEl = this.querySelector('.open-btn .text')!;
        this.#popoverEl = this.querySelector('koti-popover')!;

        // on popover open, focus on selected value in the popover.
        // XXX Have to use a small setTimeout to ensure it is actually visible.
        this.#popoverEl.addEventListener('toggle', (e) => {
            if (e.newState === 'open') {
                setTimeout(() =>
                    (this.querySelector(`.select-btn[data-value="${this.#hiddenInputEl.value}"`) as HTMLElement | null)?.focus(), 25);
            }
        });

        // on value button click, hide popover and set selected value into hidden input and button
        this.querySelectorAll('.select-btn').forEach(btn => btn.addEventListener('click', (e) => {
            const value = parseInt((e.currentTarget as HTMLElement).dataset.value!);
            this.#popoverEl.hidePopover();
            this.#hiddenInputEl.value = value.toString();
            this.#buttonColorBoxEl.style.backgroundColor = this.#COLORS[value]!;
            this.#buttonTextEl.textContent = this.#TEXTS[value]!;
        }));
    }
});
