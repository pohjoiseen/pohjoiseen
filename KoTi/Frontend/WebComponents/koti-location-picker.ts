//
// <koti-location-picker>: simple and self-contained location picker via leaflet and default OSM tiles.
// Must use hx-preserve with htmx, otherwise won't work properly.
//
import L from 'leaflet';

window.customElements.define('koti-location-picker', class extends HTMLElement {
    #latInput: HTMLInputElement = null!;
    #lngInput: HTMLInputElement = null!;
    #map: L.Map = null!;
    
    constructor() {
        super();
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
        
        if (!this.getAttribute('lat-field') || !this.getAttribute('lng-field') ||
            !this.getAttribute('initial-lat') || !this.getAttribute('initial-lng')) {
            throw new Error('initial-lat, initial-lng, lat-field and lng-field attributes must be set on <koti-location-picker> element.');    
        }
        
        const initialLat = parseFloat(this.getAttribute('initial-lat')!),
            initialLng = parseFloat(this.getAttribute('initial-lng')!),
            initialZoom = parseInt(this.getAttribute('initial-zoom') || '15', 10);
        
        this.#latInput = document.createElement('input');
        this.#latInput.setAttribute('type', 'hidden');
        this.#latInput.setAttribute('name', this.getAttribute('lat-field')!);
        this.#latInput.setAttribute('value', initialLat.toFixed(6));
        this.appendChild(this.#latInput);
        
        this.#lngInput = document.createElement('input');
        this.#lngInput.setAttribute('type', 'hidden');
        this.#lngInput.setAttribute('name', this.getAttribute('lng-field')!);
        this.#lngInput.setAttribute('value', initialLng.toFixed(6));
        this.appendChild(this.#lngInput);

        this.#map = L.map(this);
        this.#map.attributionControl.setPrefix(false);
        L.control.scale({metric: true}).addTo(this.#map);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: 'Map &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a>',
            detectRetina: true
        }).addTo(this.#map);
        this.#map.setView([initialLat, initialLng], initialZoom);

        // handle map move/zoom by updating values in hidden inputs
        const onMapMove = () => {
            this.#latInput.value = this.#map.getCenter().lat.toFixed(6)
            this.#lngInput.value = this.#map.getCenter().lng.toFixed(6);
        }
        this.#map.on('moveend', onMapMove);
        this.#map.on('zoomend', onMapMove);
        
        // map must be sized appropriately when revealed
        const observer = new IntersectionObserver(() => {
            this.#map.invalidateSize();
        });
        observer.observe(this);
    }
    
    getMap = () => this.#map;
});