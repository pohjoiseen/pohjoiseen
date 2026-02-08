//
// <koti-geo-icon-picker>: popover to pick a map icon.
//
import { generateId } from '../../Common/common.ts';

export default class GeoIconPickerElement extends HTMLElement {
    #id: string;
    #value: string = '';
    #selectedIconImgEl: HTMLImageElement = null!;
    #popoverEl: HTMLDivElement = null!;
    #internals: ElementInternals;

    readonly #ICONS = [
        'archaeology.m',
        'archaeology',
        'bigcity.m',
        'bigcity',
        'bridge.m',
        'bridge',
        'castle.m',
        'castle',
        'cemetary.m',
        'cemetary',
        'church.m',
        'church',
        'cityarea.m',
        'cityarea',
        'dam.m',
        'dam',
        'forest.m',
        'forest',
        'fortification.m',
        'fortification',
        'hotel.m',
        'hotel',
        'industry.m',
        'industry',
        'island.m',
        'island',
        'library.m',
        'library',
        'lighthouse.m',
        'lighthouse',
        'manor.m',
        'manor',
        'memorial.m',
        'memorial',
        'mine.m',
        'mine',
        'mountain.m',
        'mountain',
        'museum.m',
        'museum',
        'petroglyphs.m',
        'petroglyphs',
        'river.m',
        'river',
        'road.m',
        'road',
        'ship.m',
        'ship',
        'smallcity.m',
        'smallcity',
        'star.m',
        'star',
        'station.m',
        'station',
        'stop.m',
        'stop',
        'touristvillage.m',
        'touristvillage',
        'trail.m',
        'trail',
        'tramway.m',
        'tramway',
        'tree.m',
        'tree',
        'war.m',
        'war',
        'waterfall.m',
        'waterfall',
    ];

    constructor() {
        super();
        this.#id = generateId();
        this.#internals = this.attachInternals();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#popoverEl) return;
        
        this.innerHTML = `
            <button type="button" class="koti-btn open-btn" popovertarget="${this.#id}-popover">
                <img />
            </button>
            <koti-popover id="${this.#id}-popover">
                <div>
                    ${this.#ICONS.map((icon) => `
                        <button type="button" class="koti-btn select-btn" data-icon="${icon}">
                            <img src="/map-icons/${icon}.png" alt="${icon}" />
                            <i class="muted">${icon}</i>
                        </button>
                    `).join('\n')}
                </div>
            </koti-popover>
        `;
        this.#selectedIconImgEl = this.querySelector('.open-btn img')!;
        this.#popoverEl = this.querySelector('koti-popover')!;
        
        this.value = this.getAttribute('value') ?? '';
        
        // on popover open, focus on selected icon button in the popover.
        // XXX Have to use a small setTimeout to ensure icon is actually visible.
        this.#popoverEl.addEventListener('toggle', (e) => {
            if (e.newState === 'open') {
                setTimeout(() =>
                    (this.querySelector(`.select-btn[data-icon="${this.value}"`) as HTMLElement | null)?.focus(), 25);
            } 
        });
        
        // on icon button click, hide popover and set selected icon into hidden input and selected icon image
        this.querySelectorAll('.select-btn').forEach(btn => btn.addEventListener('click', (e) => {
            const selectedIcon = (e.currentTarget as HTMLElement).dataset.icon!;
            this.#popoverEl.hidePopover(); 
            this.value = selectedIcon;
        }));
    }
    
    get value(): string { return this.#value; }
    set value(value: string) {
        this.#value = value;
        this.#internals.setFormValue(value);
        this.setAttribute('value', value);
        this.#selectedIconImgEl.src = `/map-icons/${value}.png`;
        this.#selectedIconImgEl.alt = value;
    }
    
    attributeChangedCallback(name: string, oldValue: string | null, newValue: string | null) {
        if (name === 'value' && oldValue !== newValue) {
            this.value = newValue ?? '';
        }
    }
    
    static get observedAttributes() { return ['value']; }
    static formAssociated = true;
};
