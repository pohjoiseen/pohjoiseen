/**
 * Client-side code entry point.  We use client JS only for image galleries (via Glider.js) and maps (via Leaflet).
 */

// Styles (Leaflet, Glider.js, own)
import 'leaflet/dist/leaflet.css';
import 'glider-js/glider.css';
import './style.css';

import Glider from 'glider-js';
import {initMap} from './maps';

// Init image galleries
document.querySelectorAll('.glider-contain').forEach(galleryRoot => {
    const galleryRootEl = galleryRoot as HTMLElement;
    const id = galleryRootEl.dataset.id;
    new Glider(galleryRootEl.querySelector('.glider') as HTMLElement, {
        slidesToShow: 1,
        scrollLock: true,
        draggable: true,
        dots: '#glider-dots-' + id,
        arrows: {
            prev: '#glider-prev-' + id,
            next: '#glider-next-' + id
        }
    });
});
       
// Init maps
document.querySelectorAll('.leaflet-container')
    .forEach(leafletRoot => initMap(leafletRoot as HTMLElement, (leafletRoot as HTMLElement).dataset['map'] as any));