import * as React from 'react';
import Place from '../model/Place';
import { Button } from 'reactstrap';
import { useRef, useState } from 'react';
import { EditableHandle } from './Editable';
import EditableTextarea from './EditableTextarea';
import EditableLinksList from './EditableLinksList';
import MapPointPicker from './MapPointPicker';
import Country from '../model/Country';
import Area from '../model/Area';

interface PlaceProps {
    place: Place;
    isVisible: boolean;
    country: Country;
    area: Area;
    isAddingAlias: boolean;
    onChange: (place: Place) => void;
    onDelete: () => void;
    onAddAlias: () => void;
}

const PlaceView = ({ place, isVisible, country, area, isAddingAlias, onChange, onDelete, onAddAlias }: PlaceProps) => {
    const [isAddingNotes, setIsAddingNotes] = useState(false);
    const [isAddingLinks, setIsAddingLinks] = useState(false);
    const [isAddingDirections, setIsAddingDirections] = useState(false);
    const [isAddingPublicTransport, setIsAddingPublicTransport] = useState(false);
    const [isAddingSeason, setIsAddingSeason] = useState(false);
    const notesRef = useRef<EditableHandle>(null);
    const linksRef = useRef<EditableHandle>(null);
    const directionsRef = useRef<EditableHandle>(null);
    const publicTransportRef = useRef<EditableHandle>(null);
    const seasonRef = useRef<EditableHandle>(null);

    return <>
        {isVisible && <MapPointPicker
            mapType={country.mapType}
            lat={place.lat}
            lng={place.lng}
            zoom={place.zoom}
            parentLat={area.lat}
            parentLng={area.lng}
            parentZoom={area.zoom}
            onChange={(lat, lng, zoom) => onChange({ ...place, lat, lng, zoom })}
        />}
        <EditableTextarea
            value={place.notes}
            editableRef={notesRef}
            onChange={(value) => onChange({ ...place, notes: value })}
            onStateChange={(state) => setIsAddingNotes(state)}
        />
        <EditableLinksList
            value={place.links}
            editableRef={linksRef}
            onChange={(value) => onChange({ ...place, links: value })}
            onStateChange={(state) => setIsAddingLinks(state)}
        />
         <EditableTextarea
            value={place.directions}
            titleString="Address/directions"
            editableRef={directionsRef}
            onChange={(value) => onChange({ ...place, directions: value })}
            onStateChange={(state) => setIsAddingDirections(state)}
        />
        <EditableTextarea
            value={place.publicTransport}
            titleString="Public transport access"
            editableRef={publicTransportRef}
            onChange={(value) => onChange({ ...place, publicTransport: value })}
            onStateChange={(state) => setIsAddingPublicTransport(state)}
        />
        <EditableTextarea
            value={place.season}
            titleString="Seasonality/opening hours"
            editableRef={seasonRef}
            onChange={(value) => onChange({ ...place, season: value })}
            onStateChange={(state) => setIsAddingSeason(state)}
        />
        <div className="clearfix">&nbsp;</div>
        <div className="mt-4 d-flex">
            {!place.alias && !isAddingAlias &&
                <Button color="secondary" className="me-2" onClick={onAddAlias}><i className="bi-plus-lg" /> Alias</Button>}
            {!place.notes && !isAddingNotes  &&
                <Button color="secondary" className="me-2" onClick={() => notesRef.current!.startEditing()}><i className="bi-plus-lg" /> Description</Button>}
            {!place.links && !isAddingLinks  &&
                <Button color="secondary" className="me-2" onClick={() => linksRef.current!.startEditing()}><i className="bi-plus-lg" /> Links</Button>}
            {!place.directions && !isAddingDirections &&
                <Button color="secondary" className="me-2" onClick={() => directionsRef.current!.startEditing()}><i className="bi-plus-lg" /> Address</Button>}
            {!place.publicTransport && !isAddingPublicTransport &&
                <Button color="secondary" className="me-2" onClick={() => publicTransportRef.current!.startEditing()}><i className="bi-plus-lg" /> Public transport</Button>}
            {!place.season && !isAddingSeason &&
                <Button color="secondary" className="me-2" onClick={() => seasonRef.current!.startEditing()}><i className="bi-plus-lg" /> Season</Button>}
            <Button color="danger" className="ms-auto" onClick={onDelete}>Delete</Button>
        </div>
    </>;
}

export default PlaceView;