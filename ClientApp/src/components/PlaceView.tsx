import * as React from 'react';
import Place from '../model/Place';
import { Button } from 'reactstrap';
import { useState } from 'react';
import EditableTextarea from './EditableTextarea';
import EditableLinksList from './EditableLinksList';
import MapPointPicker from './MapPointPicker';
import Country from '../model/Country';
import Area from '../model/Area';
import EditableInline from './EditableInline';

interface PlaceProps {
    place: Place;
    isVisible: boolean;
    country: Country;
    area: Area;
    onChange: (place: Place) => void;
    onDelete: () => void;
}

const PlaceView = ({ place, isVisible, country, area, onChange, onDelete }: PlaceProps) => {
    const [isAddingAlias, setIsAddingAlias] = useState(false);
    const [isAddingNotes, setIsAddingNotes] = useState(false);
    const [isAddingLinks, setIsAddingLinks] = useState(false);
    const [isAddingDirections, setIsAddingDirections] = useState(false);
    const [isAddingPublicTransport, setIsAddingPublicTransport] = useState(false);
    const [isAddingSeason, setIsAddingSeason] = useState(false);
    
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
        {(place.alias || isAddingAlias) && <p><i><EditableInline
            value={place.alias}
            initialState={isAddingAlias}
            onStateChange={(state) => setIsAddingAlias(state)}
            onChange={(value) => onChange({ ...place, alias: value })}
        /></i></p>}
        {(place.notes || isAddingNotes) && <EditableTextarea
            value={place.notes}
            initialState={isAddingNotes}
            onStateChange={(state) => setIsAddingNotes(state)}
            onChange={(value) => onChange({ ...place, notes: value })}
        />}
        {(place.links || isAddingLinks) && <EditableLinksList
            value={place.links}
            initialState={isAddingLinks}
            onStateChange={(state) => setIsAddingLinks(state)}
            onChange={(value) => onChange({ ...place, links: value })}
        />}
        {(place.directions || isAddingDirections) && <EditableTextarea
            value={place.directions}
            titleString="Address/directions"
            initialState={isAddingDirections}
            onStateChange={(state) => setIsAddingDirections(state)}
            onChange={(value) => onChange({ ...place, directions: value })}
        />}
        {(place.publicTransport || isAddingPublicTransport) && <EditableTextarea
            value={place.publicTransport}
            titleString="Public transport access"
            initialState={isAddingPublicTransport}
            onStateChange={(state) => setIsAddingPublicTransport(state)}
            onChange={(value) => onChange({ ...place, publicTransport: value })}
        />}
        {(place.season || isAddingSeason) && <EditableTextarea
            value={place.season}
            titleString="Seasonality/opening hours"
            initialState={isAddingSeason}
            onStateChange={(state) => setIsAddingSeason(state)}
            onChange={(value) => onChange({ ...place, season: value })}
        />}
        <div className="clearfix">&nbsp;</div>
        <div className="mt-4 d-flex">
            {!place.alias && !isAddingAlias &&
                <Button color="secondary" className="me-2" onClick={() => setIsAddingAlias(true)}><i className="bi-plus-lg" /> Alias</Button>}
            {!place.notes && !isAddingNotes &&
                <Button color="secondary" className="me-2" onClick={() => setIsAddingNotes(true)}><i className="bi-plus-lg" /> Description</Button>}
            {!place.links && !isAddingLinks &&
                <Button color="secondary" className="me-2" onClick={() => setIsAddingLinks(true)}><i className="bi-plus-lg" /> Links</Button>}
            {!place.directions && !isAddingDirections &&
                <Button color="secondary" className="me-2" onClick={() => setIsAddingDirections(true)}><i className="bi-plus-lg" /> Address</Button>}
            {!place.publicTransport && !isAddingPublicTransport &&
                <Button color="secondary" className="me-2" onClick={() => setIsAddingPublicTransport(true)}><i className="bi-plus-lg" /> Public transport</Button>}
            {!place.season && !isAddingSeason &&
                <Button color="secondary" className="me-2" onClick={() => setIsAddingSeason(true)}><i className="bi-plus-lg" /> Season</Button>}
            <Button color="danger" className="ms-auto" onClick={onDelete}>Delete</Button>
        </div>
    </>;
}

export default PlaceView;