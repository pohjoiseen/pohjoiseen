import * as React from 'react';
import { Alert, Button, Modal, ModalBody, ModalFooter, ModalHeader, Spinner } from 'reactstrap';
import { useEffect, useRef, useState } from 'react';
import Area from '../model/Area';
import Country from '../model/Country';
import { getPlace } from '../api/places';
import { getArea } from '../api/areas';
import { getRegion } from '../api/regions';
import { getCountry } from '../api/countries';
import PlaceView from './PlaceView';
import { usePlacesQuery } from '../data/queries';
import { useUpdatePlaceMutation } from '../data/mutations';
import { errorMessage } from '../util';
import ExploreStatusIndicator from './ExploreStatusIndicator';
import PlaceCategoryIndicator from './PlaceCategoryIndicator';
import EditableInline from './EditableInline';
import { EditableHandle } from './Editable';
import { Link } from 'react-router-dom';
import Rating from './Rating';

interface PlaceModalProps {
    id: number;
    onSave: (placeName: string) => void;
    onClose: () => void;
}

const PlaceModal = ({ id, onSave, onClose }: PlaceModalProps) => {
    const [isLoading, setIsLoading] = useState(true);
    const [loadingError, setLoadingError] = useState('');
    const areaRef = useRef<Area | null>(null);
    const countryRef = useRef<Country | null>(null);
    const placesForArea = usePlacesQuery(areaRef.current ? areaRef.current.id : 0);
    const place = placesForArea.data ? placesForArea.data.find(p => p.id === id) : null;
    const updatePlaceMutation = useUpdatePlaceMutation();
    
    // we need country and area to display PlaceView, and we need area ID to load places
    // TODO: possibly reorganize entire country-region-area-place hierarchy in a better way
    // for now just get this data by raw requests, do not go through react-query
    useEffect(() => {
        (async () => {
            try {
                const place = await getPlace(id);
                const area = await getArea(place.areaId);
                const region = await getRegion(area.regionId);
                const country = await getCountry(region.countryId);
                areaRef.current = area;
                countryRef.current = country;
            } catch (e) {
                if (e instanceof Error) {
                    setLoadingError(e.message);
                } else {
                    setLoadingError('Failed to load place data.');
                }
            }
            setIsLoading(false);
        })();
    }, []);
    
    const doClose = () => place ? onSave(place.name) : onClose();

    const aliasRef = useRef<EditableHandle>(null);
    const [isAddingAlias, setAddingAlias] = useState(false);
    
    // render part is basically a simplified version of PlaceComponent
    return (
        <Modal isOpen={true} toggle={doClose} className="modal-wide">
            <ModalHeader toggle={doClose} tag="div" className="modal-header-w100">
                {place && <>
                    <div className="d-flex align-items-center w-100">
                        <ExploreStatusIndicator
                            status={place.exploreStatus}
                            onChange={(status) => updatePlaceMutation.mutate({ ...place, exploreStatus: status })}
                        />
                        &nbsp;
                        <PlaceCategoryIndicator
                            category={place.category}
                            onChange={(category) => updatePlaceMutation.mutate({ ...place, category })}
                        />
                        &nbsp;
                        <div className="d-flex flex-column">
                            <EditableInline
                                value={place.name}
                                viewTag="h5"
                                viewClassName="m-0"
                                onChange={(value) => updatePlaceMutation.mutate({ ...place, name: value })}
                                validation={{ required: true }}
                            />
                            <EditableInline
                                value={place.alias}
                                viewClassName="fst-italic"
                                editableRef={aliasRef}
                                onStateChange={(state) => setAddingAlias(state)}
                                onChange={(value) => updatePlaceMutation.mutate({ ...place, alias: value })}
                            />
                        </div>
                        <h5 className="m-0">(
                            <Link to={`/country/${countryRef.current?.id}/region/${areaRef.current?.regionId}/area/${areaRef.current?.id}`}>{areaRef.current?.name}</Link>
                        )</h5>
                        {place.isPrivate && <i className="bi bi-shield-lock ms-2" />}
                        <div className="flex-grow-1" />
                        <Rating
                            className="me-3"
                            value={place.rating}
                            onChange={(value) => updatePlaceMutation.mutate({ ...place, rating: value })}
                        />
                    </div>
                </>}
                {!place && 'Edit place'}
            </ModalHeader>
            <ModalBody className={place && place.isPrivate ? 'is-private' : ''}>
                {(isLoading || placesForArea.isLoading) &&
                    <h3 className="text-center"><Spinner type="grow" size="sm"/> Loading...</h3>}
                {loadingError && <Alert color="danger">Loading data for places: {loadingError}</Alert>}
                {placesForArea.isError &&
                    <Alert color="danger">Loading places: {errorMessage(placesForArea.error)}</Alert>}
                {updatePlaceMutation.isError &&
                    <Alert color="danger">Updating place: {errorMessage(updatePlaceMutation.error)}</Alert>}
                {place && <PlaceView
                    place={place}
                    isVisible={true}
                    country={countryRef.current!}
                    area={areaRef.current!}
                    isAddingAlias={isAddingAlias}
                    onAddAlias={() => aliasRef.current!.startEditing()}
                    onChange={(place) => updatePlaceMutation.mutate(place)}
                />}
                {!place && !isLoading && !placesForArea.isLoading && !loadingError && <Alert color="danger">Place not found</Alert>}
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={doClose}>Close</Button>
            </ModalFooter>
        </Modal>
);

};

export default PlaceModal;