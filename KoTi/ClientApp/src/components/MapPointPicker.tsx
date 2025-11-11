import * as React from 'react';
import { MAP_DEFINITIONS, MapType } from '../model/MapDefinitions';
import { Button } from 'reactstrap';
import { MapContainer, TileLayer, useMap, useMapEvents } from 'react-leaflet';
import { useEffect, useState } from 'react';
import { confirmModal } from './ModalContainer';

// default point to start the map with is approximately Vaasa coordinates because I live there :)
export const DEFAULT_LAT = 63.1;
export const DEFAULT_LNG = 21.6;
export const DEFAULT_ZOOM = 3;


interface MapPointPickerProps {
    mapType: MapType;
    lat: number;
    lng: number;
    zoom: number;
    parentLat?: number;
    parentLng?: number;
    parentZoom?: number;
    onChange: (lat: number, lng: number, zoom: number) => void;
}

const MapPointPicker = (props: MapPointPickerProps) => {
    const { mapType, lat, lng, zoom, parentLat, parentLng, parentZoom, onChange } = props;
    
    // initial coords from parents, if exist, otherwise from default point
    const doInitMap = () => {
        if (parentZoom && parentLat && parentLng) {
            onChange(parentLat, parentLng, parentZoom);
        } else {
            onChange(DEFAULT_LAT, DEFAULT_LNG, DEFAULT_ZOOM);
        }
    }
    
    if (!zoom) {
        return <div className="mappointpicker text-end">
            <Button color="primary" onClick={doInitMap}><i className="bi-plus-lg" />&nbsp;Coordinates...</Button>
        </div>
    }
    
    return <div className="mappointpicker">
        <MapContainer
            className="mappointpicker-map"
            center={[lat, lng]}
            zoom={zoom}
            maxZoom={MAP_DEFINITIONS[mapType].maxZoom}
            crs={MAP_DEFINITIONS[mapType].crs}
        >
            <TileLayer
                url={MAP_DEFINITIONS[mapType].source}
                attribution="Maps are hosted externally and copyrighted by their creators"
                detectRetina={true}
            />
            <MapPointPickerControls {...props} />
            <div className="mappointpicker-hline" />
            <div className="mappointpicker-vline" />
        </MapContainer>
    </div>;
};

interface MapPointPickerControlsState {
    lat: number;
    lng: number;
    zoom: number;
}

const MapPointPickerControls = ({ lat, lng, zoom, onChange }: MapPointPickerProps) => {
    const [realMapState, setRealMapState] = useState<MapPointPickerControlsState>({ lat, lng, zoom });
    const map = useMap();
    useMapEvents({
        move: () => {
            //console.log(`move: real lat -> ${map.getCenter().lat}, real lng -> ${map.getCenter().lng}`);
            setRealMapState({ ...realMapState, lat: map.getCenter().lat, lng: map.getCenter().lng });
        },
        zoomend: () => {
            //console.log(`zoom: real zoom -> ${map.getZoom()}`);
            setRealMapState({ ...realMapState, zoom: map.getZoom() });
        }
    });
    // maps for places seem to be broken otherwise
    useEffect(() => {
        setTimeout(() => {
            map.invalidateSize()
        }, 0);
    }, [lat, lng, zoom])
    
    const moved =
        zoom !== realMapState.zoom ||
        Math.abs(lng - realMapState.lng) > 0.00001 ||
        Math.abs(lat - realMapState.lat) > 0.00001;
    //console.log(`render: state = (${lat};${lng};${zoom}), real = (${realMapState.lat};${realMapState.lng};${realMapState.zoom}`);
    
    return <div className="mappointpicker-controls">
        <Button
            color={moved ? 'primary' : 'primary-outline'}
            disabled={!moved}
            onClick={() => onChange(realMapState.lat, realMapState.lng, realMapState.zoom)}>
            Update coordinates
        </Button>
        <Button
            color="danger"
            className="ms-2"
            onClick={async () => {
                if (await confirmModal('Remove coordinates information from this object?')) {
                    onChange(0, 0, 0)
                }
            }}>
            Clear
        </Button>
    </div>;
}

export default MapPointPicker;