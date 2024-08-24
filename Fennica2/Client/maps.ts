/**
 * Maps support for client side of fennica.pohjoiseen.fi.  In current iteration a rather plain Leaflet map.
 * Things of note:
 * - currently meant to display specifically Finnish map (from Finnish Maanmittauslaitos), we define custom
 *   projection for it
 * - POIs aka geos are mapped to posts, a post can have one or more geos.  They are diplayed as markers
 *   with various icons in a similar style
 * - geos have minimum zoom levels.  We define (on server/generator side) 16 GeoJSON layers with features,
 *   and turn them on/off according to zoom change
 * - clicking on geo/marker displays a popup, in which we load a bit more information from a pregenerated JSON
 *   with post metadata
 */
import L from 'leaflet';
import 'proj4leaflet';
import {GeoJSON} from 'geojson';
import {Geo, PostDefinition} from './contentTypes';
import _ from './l10n';

const MAP_SETTINGS = {
    DEFAULT_LAT: 61.504951,
    DEFAULT_LNG: 24.627933,
    DEFAULT_ZOOM: 4,
    MIN_ZOOM: 2,
    MAX_ZOOM: 13,
    ICON_PATH: '/static/map-icons/',
    ICON_WIDTH: 32,
    ICON_HEIGHT: 37,
    ICON_ANCHOR_X: 16,
    ICON_ANCHOR_Y: 34,
    POPUP_ANCHOR_X: 0,
    POPUP_ANCHOR_Y: -19,
    POPUP_WIDTH: 320,
    POPUP_WIDTH_MOBILE: 250,
    MAP_SOURCE: 'https://avoin-karttakuva.maanmittauslaitos.fi/avoin/wmts/1.0.0/maastokartta/default/ETRS-TM35FIN/{z}/{y}/{x}.png?api-key=27e77bfc-266a-4406-afe0-f7e827c11be3',
    //MAP_SOURCE: 'https://tiles.kartat.kapsi.fi/taustakartta_3067/{z}/{x}/{y}.png',
    MAP_ATTRIBUTION: 'Map &copy; <a href="http://www.maanmittauslaitos.fi/avoindata">Maanmittauslaitos</a>, ' +
        'via <a href="https://www.maanmittauslaitos.fi/karttakuvapalvelu/tekninen-kuvaus-wmts">open WMTS API</a>.  Icons: <a href="https://mapicons.mapsmarker.com">Maps Icons Collection</a>'
};

/**
 * Finnish coordinate system for using Finnish open data map raster layers.
 * Copied from https://dev.solita.fi/2017/12/12/gis-coordinate-systems.html
 * I honestly have no idea what it actually does
 */
function getCRStm35() {
    let crsName, crsOpts: L.Proj.ProjCRSOptions, projDef, zoomLevels;
    zoomLevels = [8192, 4096, 2048, 1024, 512, 256, 128, 64, 32, 16, 8, 4, 2, 1, 0.5, 0.25];
    crsName = 'EPSG:3067';
    projDef = '+proj=utm +zone=35 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs';
    crsOpts = {
        resolutions: zoomLevels,
        origin: [-548576, 8388608],
        bounds: L.bounds([-548576, 8388608], [1548576, 6291456])
    };
    return new L.Proj.CRS(crsName, projDef, crsOpts);
}

/**
 * Turns on/off layers depending on zoom level.
 * 
 * @param {L.Map} map
 * @param {(L.GeoJSON | null)[]} layers
 * @param {number} zoom
 */
function updateLayers(map: L.Map, layers: (L.GeoJSON | null)[], zoom: number) {
    let maxNonEmptyLayer = 0;
    layers.forEach((layer, k) => {
        if (layer) {
            maxNonEmptyLayer = k;
            if (map.hasLayer(layer) && zoom < k) {
                map.removeLayer(layer);
            } else if (!map.hasLayer(layer) && zoom >= k) {
                map.addLayer(layer);
            }
        }
    });
    
    const zoomNoticeEl = map.getContainer().querySelector('.mapview-zoom-notice');
    if (zoomNoticeEl) {
        if (zoom >= maxNonEmptyLayer) {
            (zoomNoticeEl as HTMLElement).style.display = 'none';
        } else {
            (zoomNoticeEl as HTMLElement).style.display = 'block';
        }
    }
}

/**
 * Get post date in localized human-readable format -- same as generator/util.ts,
 * do not pull it here as that's server side code.
 * 
 * @param {string} name
 * @return {string}
 */
function formatPostDate(name: string): string {
    const match = name.match(/([0-9]{4}-[0-9]{2}-[0-9]{2})-/);
    if (match) {
        const date = new Date(match[1]);
        return date.toLocaleDateString(lang, {year: 'numeric', month: 'long', day: 'numeric'});
    }
    return '';
}

/**
 * Renders details of a geo point for the popup.
 * 
 * @param {Geo} geo
 * @param {PostDefinition} post
 * @param {string} name
 * @param {string} url
 * @return {string}
 */
function renderMapPopup(geo: Geo, post: PostDefinition, name: string, url: string): string {
    const image = geo.titleImage || post.titleImage;
    
    let links = `<b>${_('Read more', lang)}</b>: <a href=${url + (geo.anchor ? '#' + geo.anchor : '')}>${post.title}</a>`;
    if (geo.links && geo.links.length) {
        const otherLinks = geo.links.map((link: { label?: string; path?: string; }) => {
            const target = link.path || url + (geo.anchor ? '#' + geo.anchor : '');
            return link.label
                ? `<b><a href="${target}">${link.label}</a></b>`
                : `<b>${_('Read more', lang)}</b>: <a href=${target}>${post.title}</a></b>`
        }).join("<br/>");
        links = `${links}<br><b>${_('See also', lang)}</b>:<br>${otherLinks}`;
    }

    return `<div class="map-popup">
        <h4><a href=${url + (geo.anchor ? '#' + geo.anchor : '')}>
            ${geo.title || post.title}
        </a></h4>
        ${geo.subtitle && `<div class="map-popup-subtitle">${geo.subtitle}</div>`}
        ${image && `<p><img src=${image} /></p>`}
        ${(geo.description || post.description) && `<p>${geo.description || post.description}</p>`}
        <p>
            ${links}<br>
            <b>${_('Published on', lang)}</b>: ${formatPostDate(name)}
        </p>
    </div>`;
}

/**
 * Handle click on a marker -- load and display more information about the place in popup.
 * 
 * @param {L.LeafletMouseEvent} e
 */
function onMarkerClick(e: L.LeafletMouseEvent) {
    // find out what we need to fetch
    const target = e.target as L.Marker;
    if (!target.feature) {
        return;
    }
    const id = target.feature.id as string;
    
    // async fetch
    const fetchFunction = async () => {
        // identify exact geo (by post id and lat/lng)
        const match = id.match(/^(.*)#(.*)-(.*)$/);
        if (!match) {
            throw new Error('Invalid geo id');
        }
        const [ignore, postId, lng, lat] = match;
        // identify where to render
        const containerId = 'map-popup-' + id.replace('#', '-');
        try {
            // get post data
            const response = await fetch(`/${lang}/json/${postId}.json`);
            const post: PostDefinition = await response.json();
            if (!post.geo) {
                throw new Error('No geo points in post');
            }
            // find geo from post
            const geos: Geo[] = Array.isArray(post.geo) ? post.geo : [post.geo];
            const geo = geos.find(g => g.lat.toString() == lat && g.lng.toString() == lng);
            if (!geo) {
                throw new Error('Cannot find geo coords in post');
            }
            const containerEl = document.getElementById(containerId);
            if (containerEl) {
                containerEl.innerHTML = renderMapPopup(geo, post, postId, containerEl.dataset.url!);
            }
        } catch (e) {
            // render error in a div in popup
            const containerEl = document.getElementById(containerId);
            if (containerEl) {
                containerEl.innerHTML = '<p>Error loading details</p>';  // localize?
            }
        }
    }
    fetchFunction();
}

// zoom notice overlay stuff, recipe from https://stackoverflow.com/a/42234061
// this is apparently the easiest way to add a static overlay which would NOT display on top of popups
L.Map.addInitHook(function(this: L.Map) {
    this.createPane('static');
    this.getPane('static')!.style.zIndex = '675';
});
const ZoomNoticeOverlay = L.Layer.extend({
    onAdd: function(map: L.Map) {
        this._map = map;

        const pane = map.getPane('static')!;
        this._container = L.DomUtil.create('div');
        pane.appendChild(this._container);

        this._container.className = 'mapview-zoom-notice';
        this._container.textContent = _('Zoom in to see less notable places', lang);
        
        map.on('move zoom viewreset zoomend moveend', (e) => this._update(e.target), this);

        this._update(map);
    },

    onRemove: function(map: L.Map) {
        L.DomUtil.remove(this._container);
        map.off('move zoom viewreset zoomend moveend', (e) => this._update(e.target), this);
    },

    _update: function(map: L.Map) {
        // Calculate the offset of the top-left corner of the map, relative to
        // the [0,0] coordinate of the DOM container for the map's main pane
        // Add some offset so our overlay appears more or less in the middle of the map
        const offset = map.containerPointToLayerPoint([0, 0])
            .add([map.getPixelBounds().getSize().x / 2 - this._container.offsetWidth / 2, 10]);
        L.DomUtil.setPosition(this._container, offset);
    }
});

const finnishCRS = getCRStm35();

// is there a good way to set this in CSS...  (also we could update on window resize of course)
const popupWidth = window.innerWidth >= 768 ? MAP_SETTINGS.POPUP_WIDTH : MAP_SETTINGS.POPUP_WIDTH_MOBILE;

const lang = document.documentElement.lang;

/**
 * Creates a map.  Called once per map container.
 * 
 * @param {HTMLElement} containerEl
 */
export function initMap(containerEl: HTMLElement) {

    // get map data
    const geoJSONs: GeoJSON[] = JSON.parse(containerEl.dataset.geojson!);
    delete containerEl.dataset.geojson;
    
    // create map
    const map = L.map(containerEl, {
        crs: finnishCRS
    });

    // do not show default "Leaflet" attribution
    // (It is not required as per https://groups.google.com/g/leaflet-js/c/fA6M7fbchOs/m/JTNVhqdc7JcJ) 
    map.attributionControl.setPrefix(false);
    
    // scale control
    L.control.scale({metric: true}).addTo(map);
    
    // tiles from MML
    L.tileLayer(MAP_SETTINGS.MAP_SOURCE, {
        attribution: MAP_SETTINGS.MAP_ATTRIBUTION,
        minZoom: MAP_SETTINGS.MIN_ZOOM,
        maxZoom: MAP_SETTINGS.MAX_ZOOM,
        detectRetina: true
    }).addTo(map);

    // zoom notice
    new ZoomNoticeOverlay().addTo(map);


    // create GeoJSON layers, one per zoom level (but only if GeoJSON is non-empty)
    const geoJSONLayers: (L.GeoJSON | null)[] = geoJSONs.map((geoJSON, k) => {
        if (geoJSON.type !== 'FeatureCollection' || !geoJSON.features.length) {
            return null;
        }
        
        return L.geoJSON(geoJSON, {
            pointToLayer(geoJsonPoint, latLng) {
                // for each point create a marker with an icon of predefined size and click event handler
                return L.marker(latLng, {
                    icon: L.icon({
                        iconUrl: MAP_SETTINGS.ICON_PATH + (geoJsonPoint.properties.icon || 'star') + '.png',
                        iconSize: [MAP_SETTINGS.ICON_WIDTH, MAP_SETTINGS.ICON_HEIGHT],
                        iconAnchor: [MAP_SETTINGS.ICON_ANCHOR_X, MAP_SETTINGS.ICON_ANCHOR_Y],
                        popupAnchor: [MAP_SETTINGS.POPUP_ANCHOR_X, MAP_SETTINGS.POPUP_ANCHOR_Y]
                    })
                })
                    .on('click', onMarkerClick);
            },
            
            onEachFeature(feature, layer) {
                // popup with stub content, onMarkerClick() actually loads 
                // closeButton=false because of https://issuehint.com/issue/Leaflet/Leaflet/8159
                // TODO: fix when Leaflet is updated
                layer.bindPopup(`<div id="map-popup-${(feature.id as string).replace(/[\/#]/g, '-')}" data-url="${feature.properties.url}">
                    <div class="map-popup"><h4>${feature.properties.title}</h4><div id="map-popup-details">Loading...</div></div>
                </div>`, {minWidth: popupWidth, maxWidth: popupWidth, closeButton: false});
            }
        })
    });

    // initialize viewport, fit all layers
    const bounds = L.latLngBounds([]);
    for (const layer of geoJSONLayers) {
        if (!layer) {
            continue;
        }
        bounds.extend(layer.getBounds());
    }
    map.fitBounds(bounds);
    updateLayers(map, geoJSONLayers, map.getZoom());
    
    // update layers on zoom change
    map.on('zoomend', () => updateLayers(map, geoJSONLayers, map.getZoom()));
    
}