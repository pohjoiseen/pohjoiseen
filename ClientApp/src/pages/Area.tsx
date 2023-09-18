import * as React from 'react';
import { useRef, useState } from 'react';
import { Accordion, AccordionBody, AccordionItem, Alert, Button, Container, Spinner } from 'reactstrap';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { UseMutationResult } from '@tanstack/react-query';
import { errorMessage } from '../util';
import { confirmModal } from '../components/ModalContainer';
import { useAreasQuery, useCountriesQuery, usePlacesQuery, useRegionsQuery } from '../data/queries';
import {
    useCreatePlaceMutation,
    useDeleteAreaMutation,
    useDeletePlaceMutation,
    useUpdateAreaMutation,
    useUpdatePlaceMutation
} from '../data/mutations';
import NavBar from '../components/NavBar';
import EditableInline from '../components/EditableInline';
import ExploreStatusIndicator from '../components/ExploreStatusIndicator';
import EditableTextarea from '../components/EditableTextarea';
import EditableLinksList from '../components/EditableLinksList';
import CreateModal from '../components/CreateModal';
import ExploreStatus from '../model/ExploreStatus';
import Area from '../model/Area';
import Country from '../model/Country';
import Place from '../model/Place';
import PlaceView from '../components/PlaceView';
import { PlaceCategory } from '../model/PlaceCategory';
import PlaceCategoryIndicator from '../components/PlaceCategoryIndicator';
import MapPointPicker from '../components/MapPointPicker';
import { EditableHandle } from '../components/Editable';

const AreaPage = () => {
    // country/region id from route
    const routeParams = useParams();
    const countryId = parseInt(routeParams['countryId']!);
    const regionId = parseInt(routeParams['regionId']!);
    const areaId = parseInt(routeParams['areaId']!);
    
    const navigate = useNavigate();
    
    /// UI state ///

    const [placeIdOpen, setPlaceIdOpen] = useState<string>('');
    const [isAddPlaceModalOpen, setAddPlaceModalOpen] = useState(false);

    /// queries ///

    // current country from list of all countries
    const countries = useCountriesQuery();
    const country = countries.isSuccess ? countries.data.find(c => c.id === countryId) : null;
    // current region from list of all regions
    const regions = useRegionsQuery(countryId);
    const region = regions.isSuccess ? regions.data.find(r => r.id === regionId) : null;
    // list of all areas
    const areas = useAreasQuery(regionId);
    const area = areas.isSuccess ? areas.data.find(a => a.id === areaId) : null;
    // places for area
    const places = usePlacesQuery(areaId);

    /// mutations ///

    const updateAreaMutation = useUpdateAreaMutation();
    const deleteAreaMutation = useDeleteAreaMutation();
    const createPlaceMutation = useCreatePlaceMutation();
    const updatePlaceMutation = useUpdatePlaceMutation();
    const deletePlaceMutation = useDeletePlaceMutation();
    
    /// loading/error messages ///

    // do not show anything unless all loaded
    if (countries.isLoading || regions.isLoading || areas.isLoading || places.isLoading) {
        return <NavBar><h3><Spinner type="grow" size="sm" /> Loading...</h3></NavBar>;
    }
    if (countries.isError) {
        return <Alert color="danger">Loading countries: {errorMessage(countries.error)}</Alert>;
    }
    if (regions.isError) {
        return <Alert color="danger">Loading regions: {errorMessage(regions.error)}</Alert>;
    }
    if (areas.isError) {
        return <Alert color="danger">Loading areas: {errorMessage(areas.error)}</Alert>;
    }
    if (places.isError) {
        return <Alert color="danger">Loading places: {errorMessage(places.error)}</Alert>;
    }
    // loaded successfully but country/region not found
    if (!country || !region || !area || region.countryId !== countryId || area.regionId !== regionId) {
        return <Alert color="warning">Country, region or area not found</Alert>;
    }
    
    /// add in separately loaded places into area ///
    
    area.places = places.data;

    /// event handlers ///
    
    const togglePlace = (id: string) => {
        if (placeIdOpen === id) {
            setPlaceIdOpen('');
        } else {
            setPlaceIdOpen(id);
        }
    };
    
    /// main UI ///

    return <div>
        <NavBar>
            <h3><Link to={`/country/${country.id}`}>{country.flagEmoji} {country.name}</Link></h3>
            <h3>&nbsp;&rsaquo;&nbsp;</h3>
            <h3><Link to={`/country/${country.id}/region/${region.id}`}>{region.name}</Link></h3>
            <h3>&nbsp;&rsaquo;&nbsp;</h3>
            <EditableInline
                value={area.name}
                onChange={(value) => updateAreaMutation.mutate({ ...area, name: value })}
                viewTag="h3"
                inputClassName="fs-3 p-0 lh-1"
                validation={{ required: true }}
            />
            <Button color="danger" className="ms-auto" onClick={async () => {
                if (await confirmModal('Really delete this area?  This will delete all places inside it!')) {
                    await deleteAreaMutation.mutateAsync(area);
                    navigate(`/country/${countryId}/region/${regionId}`);
                }
            }}>Delete</Button>
        </NavBar>
        <Container>
            {updateAreaMutation.isError && <Alert color="danger">Updating area: {errorMessage(updateAreaMutation.error)}</Alert>}
            {deleteAreaMutation.isError && <Alert color="danger">Deleting area: {errorMessage(deleteAreaMutation.error)}</Alert>}
            {createPlaceMutation.isError && <Alert color="danger">Creating place: {errorMessage(createPlaceMutation.error)}</Alert>}
            {updatePlaceMutation.isError && <Alert color="danger">Updating place: {errorMessage(updatePlaceMutation.error)}</Alert>}
            {deletePlaceMutation.isError && <Alert color="danger">Deleting place: {errorMessage(deletePlaceMutation.error)}</Alert>}
            <MapPointPicker
                mapType={country.mapType}
                lat={area.lat}
                lng={area.lng}
                zoom={area.zoom}
                onChange={(lat, lng, zoom) => updateAreaMutation.mutate({ ...area, lat, lng, zoom })}
            />
            <h6>
                Explore status:
                &nbsp;
                <ExploreStatusIndicator
                    status={area.exploreStatus}
                    onChange={(status) => updateAreaMutation.mutate({ ...area, exploreStatus: status })}
                />
            </h6>
            <EditableTextarea
                value={area.notes}
                onChange={(value) => updateAreaMutation.mutate({ ...area, notes: value })}
                emptyValueString="No description yet."
            />
            <EditableLinksList
                value={area.links}
                onChange={(value) => updateAreaMutation.mutate({ ...area, links: value })}
            />
            <div className="clearfix" />
            {area.places.length > 0 && <>
                <h6 className="mt-4">Places:</h6>
                {/* toggle prop workaround due to missing typings, see https://github.com/reactstrap/reactstrap/issues/2165 */}
                <Accordion open={placeIdOpen} {...{toggle: togglePlace}}>
                    {area.places.map(p => <PlaceAccordionItem
                        key={p.id}    
                        country={country}
                        area={area}
                        place={p}
                        updatePlaceMutation={updatePlaceMutation}
                        deletePlaceMutation={deletePlaceMutation}
                        placeIdOpen={placeIdOpen}
                        togglePlace={togglePlace}
                        setPlaceIdOpen={setPlaceIdOpen}
                    />)}
                </Accordion>
            </>}
            {area.places.length === 0 && <p className="text-muted mt-4">No places defined.</p>}
            <div className="mt-2 mb-2">
                <Button color="primary" onClick={() => setAddPlaceModalOpen(true)}>Add place...</Button>
            </div>
            {isAddPlaceModalOpen && <CreateModal
                object={{
                    id: 0,
                    areaId,
                    name: '',
                    category: PlaceCategory.Default,
                    exploreStatus: ExploreStatus.None,
                    notes: '',
                    links: '',
                    directions: '',
                    season: '',
                    publicTransport: '',
                    order: (places.data?.length || 0) + 1,
                    lat: 0,
                    lng: 0,
                    zoom: 0,
                } as Place}
                title="Add place"
                onClose={() => setAddPlaceModalOpen(false)}
                onSubmit={async (place) => {
                    place = await createPlaceMutation.mutateAsync(place);
                    setPlaceIdOpen(place.id.toString());
                }}
            />}
        </Container>
    </div>;
};

interface PlaceAccordionItemProps {
    country: Country;
    area: Area;
    place: Place;
    updatePlaceMutation: UseMutationResult<void, unknown, Place>;
    deletePlaceMutation: UseMutationResult<void, unknown, Place>;
    placeIdOpen: string;
    togglePlace: (id: string) => void;
    setPlaceIdOpen: (id: string) => void;
}
const PlaceAccordionItem = ({ area, country, place, updatePlaceMutation, deletePlaceMutation,
                                placeIdOpen, togglePlace, setPlaceIdOpen }: PlaceAccordionItemProps) => {
    const aliasRef = useRef<EditableHandle>(null);
    const [isAddingAlias, setAddingAlias] = useState(false);
    
    return <AccordionItem>
        {/* do not use <AccordionHeader> because it does not allow us to intercept onClick */}
        <div className="accordion-header">
            <div
                tabIndex={0}
                className={`accordion-button place-header ${placeIdOpen === place.id.toString() ? '' : 'collapsed'} ${place.alias ? 'with-alias' : ''}`}
                onClick={(e) => {
                    // click on accordion header should toggle accordion item, but not if it's on
                    // explore status or editable inline
                    const target = e.target as HTMLElement;
                    if (target && ((target.classList && target.classList.contains('accordion-button')) ||
                        (target.tagName && target.tagName === 'H5'))) {
                        togglePlace(place.id.toString());
                    }
                }}
            >
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
            </div>
        </div>
        <AccordionBody accordionId={place.id.toString()}>
            <PlaceView
                place={place}
                isVisible={place.id.toString() === placeIdOpen}
                area={area}
                country={country}
                isAddingAlias={isAddingAlias}
                onAddAlias={() => aliasRef.current!.startEditing()}
                onChange={(place) => updatePlaceMutation.mutate(place)}
                onDelete={async () => {
                    if (await confirmModal('Really delete this place?')) {
                        await deletePlaceMutation.mutateAsync(place);
                        setPlaceIdOpen('');
                    }
                }}
            />
        </AccordionBody>
    </AccordionItem>;
};

export default AreaPage;