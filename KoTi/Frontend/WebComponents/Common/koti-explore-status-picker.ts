//
// <koti-explore-status-picker>: popover to pick explore status (green-yellow-orange-red).
//
import { generateId } from '../../Common/common.ts';

const NONE = 0;
const MINIMAL = 1;
const IN_PROGRESS = 2;
const SUFFICIENT = 3;

export default class ExploreStatusPickerElement extends HTMLElement {
    #internals: ElementInternals;
    #id: string;
    #value: number = NONE;
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
        this.#internals = this.attachInternals();
    }

    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#popoverEl) return;

        const initialValue = parseInt(this.getAttribute('value') || NONE.toString());
        this.innerHTML = `
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
        this.#buttonColorBoxEl = this.querySelector('.open-btn .color-box')!;
        this.#buttonTextEl = this.querySelector('.open-btn .text')!;
        this.#popoverEl = this.querySelector('koti-popover')!;
        this.value = initialValue;

        // on popover open, focus on selected value in the popover.
        // XXX Have to use a small setTimeout to ensure it is actually visible.
        this.#popoverEl.addEventListener('toggle', (e) => {
            if (e.newState === 'open') {
                setTimeout(() =>
                    (this.querySelector(`.select-btn[data-value="${this.#value}"`) as HTMLElement | null)?.focus(), 25);
            }
        });

        // on value button click, hide popover and set selected value into hidden input and button
        this.querySelectorAll('.select-btn').forEach(btn => btn.addEventListener('click', (e) => {
            const value = parseInt((e.currentTarget as HTMLElement).dataset.value!);
            this.#popoverEl.hidePopover();
            this.value = value;
            this.#buttonColorBoxEl.style.backgroundColor = this.#COLORS[value]!;
            this.#buttonTextEl.textContent = this.#TEXTS[value]!;
        }));
    }
    
    attributeChangedCallback(name: string, oldValue: string | null, newValue: string | null) {
        if (name === 'value' && oldValue !== newValue) {
            this.value = parseInt(newValue ?? NONE.toString());
            this.#buttonColorBoxEl.style.backgroundColor = this.#COLORS[parseInt(newValue ?? NONE.toString())]!;
            this.#buttonTextEl.textContent = this.#TEXTS[parseInt(newValue ?? NONE.toString())]!;
        }
    }
    
    get value(): number { return this.#value; }
    set value(value: number) {
        this.#value = value;
        this.#internals.setFormValue(value.toString());
        this.setAttribute('value', value.toString());
    }
    
    static get observedAttributes() { return ['value']; }
    static formAssociated = true;
};
