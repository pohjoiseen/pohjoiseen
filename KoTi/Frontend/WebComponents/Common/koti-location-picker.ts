//
// <koti-location-picker>: simple and self-contained location picker via leaflet and default OSM tiles.
// Must use hx-preserve with htmx, otherwise won't work properly.
//
import L from 'leaflet';

export default class LocationPickerElement extends HTMLElement {
    #map: L.Map = null!;
    #canUpdateLocation = false;
    #internals: ElementInternals;
    
    constructor() {
        super();
        this.#internals = this.attachInternals();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#map) return;
        
        const crossbarH = document.createElement('div');
        crossbarH.classList.add('crossbar-h');
        this.appendChild(crossbarH);
        const crossbarV = document.createElement('div');
        crossbarV.classList.add('crossbar-v');
        this.appendChild(crossbarV);
        
        if (this.getAttribute('lat') == null || this.getAttribute('lng') == null) {
            throw new Error('LocationPickerElement must have lat and lng attributes');
        }
        
        const initialLat = parseFloat(this.getAttribute('lat')!),
            initialLng = parseFloat(this.getAttribute('lng')!),
            initialZoom = parseInt(this.getAttribute('initial-zoom') || '15', 10);

        this.#map = L.map(this);
        this.#map.attributionControl.setPrefix(false);
        L.control.scale({metric: true}).addTo(this.#map);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: 'Map &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a>',
            detectRetina: true
        }).addTo(this.#map);
        this.#map.setView([initialLat, initialLng], initialZoom);
        this.updateValue(initialLat, initialLng);

        // handle map move/zoom by updating values in hidden inputs
        const onMapMove = () => {
            // only do this after map has been actually shown for the first time
            // otherwise map centering might already shift the position a little bit
            // and cause among other things spurious "unsaved changed, really leave?" prompts
            if (this.#canUpdateLocation) {
                this.updateValue(this.#map.getCenter().lat, this.#map.getCenter().lng);
            }
        }
        this.#map.on('moveend', onMapMove);
        this.#map.on('zoomend', onMapMove);
        
        // map must be sized appropriately when revealed
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) return;
                this.#map.invalidateSize();
                setTimeout(() => this.#canUpdateLocation = true, 100);
            });
        });
        observer.observe(this);
    }
    
    private updateValue(lat: number, lng: number) {
        const formData = new FormData();
        const namePrefix = this.getAttribute('name-prefix');
        formData.set(namePrefix ? `${namePrefix}[Lat]` : 'Lat', lat.toFixed(6));
        formData.set(namePrefix ? `${namePrefix}[Lng]` : 'Lng', lng.toFixed(6));
        this.#internals.setFormValue(formData);
    }
    
    getMap = () => this.#map;

    attributeChangedCallback(name: string, oldValue: string | null, newValue: string | null) {
        if (this.#map && oldValue !== newValue) {
            this.#map.setView([parseFloat(this.getAttribute('lat') ?? '0'), parseFloat(this.getAttribute('lng') ?? '0')]);
        }
    }

    static get observedAttributes() { return ['lat', 'lng']; }
    static formAssociated = true;
};
