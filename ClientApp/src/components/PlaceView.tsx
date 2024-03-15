/**
 * This is the "inside" view of a place, within an accordion item.  The entire accordion item is PlaceComponent. 
 */
import * as React from 'react';
import Place from '../model/Place';
import { Alert, Button, FormGroup, Label } from 'reactstrap';
import { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { EditableHandle } from './Editable';
import EditableTextarea from './EditableTextarea';
import EditableLinksList from './EditableLinksList';
import MapPointPicker from './MapPointPicker';
import Country from '../model/Country';
import Area from '../model/Area';
import { PICTURE_PREVIEW_NUMBER, usePicturesByPlaceQuery } from '../data/queries';
import { errorMessage } from '../util';
import PicturesList from './PicturesList';
import { PicturesViewMode } from './pictureViewCommon';

interface PlaceProps {
    place: Place;
    isVisible: boolean;
    country: Country;
    area: Area;
    isAddingAlias: boolean;
    onChange: (place: Place) => void;
    onDelete?: () => void;
    onAddAlias: () => void;
}

const PlaceView = ({ place, isVisible, country, area, isAddingAlias, onChange, onDelete, onAddAlias }: PlaceProps) => {
    const pictures = usePicturesByPlaceQuery(place.id, isVisible);
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
    const navigate = useNavigate();

    return <>
        {pictures.isError &&
            <Alert color="danger">Loading pictures: {errorMessage(pictures.error)}</Alert>}
        {pictures && pictures.data && <div className="mb-2">
            {/* TODO: should be possible to open in fullscreen to, but for now just stub out.
                      onSetSelection (click) does the same thing as onOpen (double click) */}
            <PicturesList
                noWrap
                showMore={pictures.data.length > PICTURE_PREVIEW_NUMBER}
                viewMode={PicturesViewMode.THUMBNAILS}
                pictures={pictures.data}
                currentIndex={-1}
                selection={[]}
                onOpen={() => navigate(`/pictures/all?objectTable=Places&objectId=${place.id}&objectName=${place.name}`)}
                onSetSelection={() => navigate(`/pictures/all?objectTable=Places&objectId=${place.id}&objectName=${place.name}`)}
            />
        </div>}
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
        <div className="mt-4 d-flex align-items-center">
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
            <FormGroup check inline className="m-0 me-2">
                <input
                    type="checkbox"
                    className="form-check-input"
                    id={`place-is-private-${place.id}`}
                    checked={place.isPrivate}
                    onChange={(e) => onChange({ ...place, isPrivate: e.target.checked })}
                />
                <Label htmlFor={`place-is-private-${place.id}`} check>Private/Draft</Label>
            </FormGroup>
            <div className="ms-auto text-muted">Last updated: {place.updatedAt.toLocaleString('fi')}</div>
            {onDelete && <Button color="danger" className="ms-2" onClick={onDelete}>Delete</Button>}
        </div>
    </>;
}

export default PlaceView;