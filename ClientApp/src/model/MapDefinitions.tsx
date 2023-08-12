import L from 'leaflet';
import 'proj4leaflet';

export enum MapType {
    DefaultOSM = 'default',
    Finland = 'finland'
}

/**
 * Finnish coordinate system for using Finnish open data map raster layers.
 * Copied from https://dev.solita.fi/2017/12/12/gis-coordinate-systems.html
 * I honestly have no idea what it actually does
 */
const getCRStm35 = () => {
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
};

export const MAP_CRS: {[mapType in MapType]: L.Proj.CRS | undefined} = {
    [MapType.DefaultOSM]: undefined,
    [MapType.Finland]: getCRStm35()
};

export const MAP_SOURCE: {[mapType in MapType]: string} = {
    [MapType.DefaultOSM]: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
    [MapType.Finland]: 'https://avoin-karttakuva.maanmittauslaitos.fi/avoin/wmts/1.0.0/maastokartta/default/ETRS-TM35FIN/{z}/{y}/{x}.png?api-key=27e77bfc-266a-4406-afe0-f7e827c11be3'
};
