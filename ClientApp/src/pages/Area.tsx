import * as React from 'react';
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Accordion, Alert, Button, Container, Spinner } from 'reactstrap';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { errorMessage } from '../util';
import { confirmModal } from '../components/ModalContainer';
import {
    PICTURE_PREVIEW_NUMBER,
    useAreasQuery,
    useCountriesQuery,
    usePicturesByAreaQuery,
    usePlacesQuery,
    useRegionsQuery
} from '../data/queries';
import {
    useCreatePlaceMutation,
    useDeleteAreaMutation,
    useDeletePlaceMutation,
    useReorderPlacesMutation,
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
import Place from '../model/Place';
import { PlaceCategory } from '../model/PlaceCategory';
import MapPointPicker from '../components/MapPointPicker';
import PlaceComponent from '../components/PlaceComponent';
import PicturesList from '../components/PicturesList';
import { PicturesViewMode } from '../components/pictureViewCommon';

const AreaPage = () => {
    // country/region id from route
    const routeParams = useParams();
    const countryId = parseInt(routeParams['countryId']!);
    const regionId = parseInt(routeParams['regionId']!);
    const areaId = parseInt(routeParams['areaId']!);
    
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();
    
    /// UI state ///

    const [isAddPlaceModalOpen, setAddPlaceModalOpen] = useState(false);
    const filter = searchParams.get('filter') || '';
    const setFilter = useCallback((filter: string) => setSearchParams(params => {
        if (filter) {
            params.set('filter', filter);
        } else {
            params.delete('filter');
        }
        return params;
    }, { replace: true }), [setSearchParams]);
    const placeIdOpen = searchParams.get('place') || '';
    const setPlaceIdOpen = useCallback((placeIdOpen: string) => setSearchParams(params => {
        if (placeIdOpen) {
            params.set('place', placeIdOpen);
        } else {
            params.delete('place');
        }
        return params;
    }, { replace: true }), [setSearchParams]);
    
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
    // picture for area
    const pictures = usePicturesByAreaQuery(areaId);

    /// mutations ///

    const updateAreaMutation = useUpdateAreaMutation();
    const deleteAreaMutation = useDeleteAreaMutation();
    const createPlaceMutation = useCreatePlaceMutation();
    const updatePlaceMutation = useUpdatePlaceMutation();
    const deletePlaceMutation = useDeletePlaceMutation();
    const reorderPlacesMutation = useReorderPlacesMutation();

    /// scroll filter into view if first opened with a filter ///

    const placesFilterInputRef = useRef<HTMLInputElement>(null);
    const [alreadyScrolled, setAlreadyScrolled] = useState(false);
    useLayoutEffect(() => {
        console.log('placeIdOpen = ', placeIdOpen, '; alreadyScrolled = ', alreadyScrolled, '; area = ', area, '; places.data = ', places.data, '; filter = ', filter, '; ref = ', placesFilterInputRef.current);
        if (!placeIdOpen || alreadyScrolled || !area || !places.data) {
            return;
        }
        console.log('running effect');
        if (!filter) {
            const place = places.data.find(p => p.id.toString() === placeIdOpen);
            if (place) {
                setFilter(place.name);
            }
        }
        requestAnimationFrame(() => placesFilterInputRef.current?.scrollIntoView());
    }, [setAlreadyScrolled, placeIdOpen, area, places.data, alreadyScrolled]);

    /// loading/error messages ///

    // do not show anything unless all loaded
    if (countries.isLoading || regions.isLoading || areas.isLoading || places.isLoading || pictures.isLoading) {
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
    if (pictures.isError) {
        return <Alert color="danger">Loading pictures: {errorMessage(pictures.error)}</Alert>;
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
    
    const reorderPlaces = (from: number, to: number) => {
        const newPlaces = [...area.places];
        newPlaces.splice(from, 1);
        newPlaces.splice(to, 0, area.places[from]);
        reorderPlacesMutation.mutate(newPlaces);
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
            {updateAreaMutation.isError && <Alert color="danger" className="alert-fixed">Updating area: {errorMessage(updateAreaMutation.error)}</Alert>}
            {deleteAreaMutation.isError && <Alert color="danger" className="alert-fixed">Deleting area: {errorMessage(deleteAreaMutation.error)}</Alert>}
            {createPlaceMutation.isError && <Alert color="danger" className="alert-fixed">Creating place: {errorMessage(createPlaceMutation.error)}</Alert>}
            {updatePlaceMutation.isError && <Alert color="danger" className="alert-fixed">Updating place: {errorMessage(updatePlaceMutation.error)}</Alert>}
            {deletePlaceMutation.isError && <Alert color="danger" className="alert-fixed">Deleting place: {errorMessage(deletePlaceMutation.error)}</Alert>}
            {reorderPlacesMutation.isError && <Alert color="danger" className="alert-fixed">Reordering places: {errorMessage(reorderPlacesMutation.error)}</Alert>}
            <MapPointPicker
                mapType={country.mapType}
                lat={area.lat}
                lng={area.lng}
                zoom={area.zoom}
                onChange={(lat, lng, zoom) => updateAreaMutation.mutate({ ...area, lat, lng, zoom })}
            />
            {pictures.data.length > 0 && <div className="mb-2">
                {/* TODO: should be possible to open in fullscreen to, but for now just stub out.
                    onSetSelection (click) does the same thing as onOpen (double click) */}
                <PicturesList
                    noWrap
                    showMore={pictures.data.length > PICTURE_PREVIEW_NUMBER}
                    viewMode={PicturesViewMode.THUMBNAILS}
                    pictures={pictures.data}
                    currentIndex={-1}
                    selection={[]}
                    link={`/pictures/all?objectTable=Areas&objectId=${area.id}&objectName=${area.name}`}
                    onOpen={() => navigate(`/pictures/all?objectTable=Areas&objectId=${area.id}&objectName=${area.name}`)}
                    onSetSelection={() => navigate(`/pictures/all?objectTable=Areas&objectId=${area.id}&objectName=${area.name}`)}
                />
            </div>}
            <h6>
                Explore status:
                &nbsp;
                <ExploreStatusIndicator
                    status={area.exploreStatus}
                    onChange={(status) => updateAreaMutation.mutate({ ...area, exploreStatus: status })}
                />
                <br/>
                Last updated: {area.updatedAt.toLocaleString('fi')}
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
                <label className="mt-4 mb-2 fs-6 fw-bold d-flex align-items-center">
                    Places:
                    <input
                        type="text"
                        className="form-control ms-2"
                        value={filter}
                        onChange={e => setFilter(e.target.value)}
                        placeholder="Filter..."
                        ref={placesFilterInputRef}
                    />
                </label>
                {/* toggle prop workaround due to missing typings, see https://github.com/reactstrap/reactstrap/issues/2165 */}
                <Accordion open={placeIdOpen} {...{toggle: togglePlace}}>
                    {area.places
                        .filter(p => filter ? p.name.toLocaleLowerCase().includes(filter.toLocaleLowerCase()) : true)
                        .map((p, i) => <PlaceComponent
                        key={p.id}    
                        country={country}
                        area={area}
                        place={p}
                        index={i}
                        updatePlaceMutation={updatePlaceMutation}
                        deletePlaceMutation={deletePlaceMutation}
                        isOpen={placeIdOpen === p.id.toString()}
                        onSetIsOpen={(isOpen) => setPlaceIdOpen(isOpen ? p.id.toString() : '')}
                        allowDnD={!filter}
                        onReorder={reorderPlaces}
                    />)}
                </Accordion>
            </>}
            {area.places.length === 0 && <p className="text-muted mt-4">{filter ? 'No matching places.' : 'No places defined.'}</p>}
            <div className="mt-2 mb-2">
                <Button color="primary" onClick={() => setAddPlaceModalOpen(true)}>Add place...</Button>
            </div>
            {isAddPlaceModalOpen && <CreateModal
                object={{
                    id: 0,
                    areaId,
                    name: '',
                    alias: '',
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
                    isPrivate: false,
                    rating: 0,
                    updatedAt: new Date(),
                    tags: []
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

export default AreaPage;