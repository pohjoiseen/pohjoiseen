//
// <koti-geo-icon-picker>: popover to pick a map icon.
//
import { generateId } from '../../Common/common.ts';

export default class GeoIconPickerElement extends HTMLElement {
    #id: string;
    #hiddenInputEl: HTMLInputElement = null!;
    #selectedIconImgEl: HTMLImageElement = null!;
    #popoverEl: HTMLDivElement = null!;

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
        'industry.m',
        'industry',
        'island.m',
        'island',
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
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#hiddenInputEl) return;
        
        this.innerHTML = `
            <input type="hidden" name="${this.getAttribute('name')}" value="${this.getAttribute('value')}" />
            <button type="button" class="koti-btn open-btn" popovertarget="${this.#id}-popover">
                <img src="/map-icons/${this.getAttribute('value')}.png" alt="${this.getAttribute('value')}" />
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
        this.#hiddenInputEl = this.querySelector('input')!;
        this.#selectedIconImgEl = this.querySelector('.open-btn img')!;
        this.#popoverEl = this.querySelector('koti-popover')!;
        
        // on popover open, focus on selected icon button in the popover.
        // XXX Have to use a small setTimeout to ensure icon is actually visible.
        this.#popoverEl.addEventListener('toggle', (e) => {
            if (e.newState === 'open') {
                setTimeout(() =>
                    (this.querySelector(`.select-btn[data-icon="${this.#hiddenInputEl.value}"`) as HTMLElement | null)?.focus(), 25);
            } 
        });
        
        // on icon button click, hide popover and set selected icon into hidden input and selected icon image
        this.querySelectorAll('.select-btn').forEach(btn => btn.addEventListener('click', (e) => {
            const selectedIcon = (e.currentTarget as HTMLElement).dataset.icon!;
            this.#popoverEl.hidePopover(); 
            this.#hiddenInputEl.value = selectedIcon;
            this.#selectedIconImgEl.src = `/map-icons/${selectedIcon}.png`;
            this.#selectedIconImgEl.alt = selectedIcon;
        }));
    }
};
