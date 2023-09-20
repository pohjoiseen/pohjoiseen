import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { errorMessage } from '../util';
import Area from '../model/Area';
import ExploreStatus from '../model/ExploreStatus';
import NavBar from '../components/NavBar';
import { confirmModal } from '../components/ModalContainer';
import EditableInline from '../components/EditableInline';
import { useAreasQuery, useCountriesQuery, useRegionsQuery } from '../data/queries';
import {
    useUpdateRegionMutation,
    useDeleteRegionMutation,
    useCreateAreaMutation,
    useUpdateAreaMutation,
    useReorderAreasMutation
} from '../data/mutations';
import CreateModal from '../components/CreateModal';
import AreaCard from '../components/AreaCard';

const RegionPage = () => {
    // country/region id from route
    const routeParams = useParams();
    const countryId = parseInt(routeParams['countryId']!);
    const regionId = parseInt(routeParams['regionId']!);
    
    const navigate = useNavigate();

    /// modal state ///

    const [isAddAreaModalOpen, setAddAreaModalOpen] = useState(false);

    /// queries ///
    
    // current country from list of all countries
    const countries = useCountriesQuery();
    const country = countries.isSuccess ? countries.data.find(c => c.id === countryId) : null;
    // current region from list of all regions
    const regions = useRegionsQuery(countryId);
    const region = regions.isSuccess ? regions.data.find(r => r.id === regionId) : null;
    // areas from region
    const areas = useAreasQuery(regionId);

    /// mutations ///

    const updateRegionMutation = useUpdateRegionMutation();
    const deleteRegionMutation = useDeleteRegionMutation();
    const createAreaMutation = useCreateAreaMutation();
    const updateAreaMutation = useUpdateAreaMutation();
    const reorderAreasMutation = useReorderAreasMutation();
    
    /// loading/error messages ///
    
    // do not show anything unless all loaded
    if (countries.isLoading || regions.isLoading || areas.isLoading) {
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
    // loaded successfully but country/region not found
    if (!country || !region) {
        return <Alert color="warning">Country or region not found</Alert>;
    }
    
    /// event handlers ///

    const doDeleteRegion = async () => {
        if (await confirmModal('Really delete this region?  This will delete all areas and all places inside it!')) {
            await deleteRegionMutation.mutateAsync(region);
            navigate(`/country/${countryId}`);
        }
    };

    const doReorderAreas = (from: number, to: number) => {
        const newAreas = [...areas.data];
        newAreas.splice(from, 1);
        newAreas.splice(to, 0, areas.data[from]);
        reorderAreasMutation.mutate(newAreas);
    };

    /// main UI ///

    return <div>
        <NavBar>
            <h3><Link to={`/country/${countryId}`}>{country.flagEmoji} {country.name}</Link></h3>
            <h3>&nbsp;&rsaquo;&nbsp;</h3>
            <EditableInline
                value={region.name}
                onChange={(value) => updateRegionMutation.mutate({ ...region, name: value })}
                viewTag="h3"
                inputClassName="fs-3 p-0 lh-1"
                validation={{ required: true }}
            />
            <Button color="primary" className="ms-auto me-2" onClick={() => setAddAreaModalOpen(true)}>Add area...</Button>
            <Button color="danger" onClick={doDeleteRegion}>Delete</Button>
        </NavBar>
        <Container>
            {createAreaMutation.isError && <Alert color="danger">Creating area: {errorMessage(createAreaMutation.error)}</Alert>}
            {updateAreaMutation.isError && <Alert color="danger">Updating area: {errorMessage(updateAreaMutation.error)}</Alert>}
            {reorderAreasMutation.isError && <Alert color="danger">Reordering areas: {errorMessage(reorderAreasMutation.error)}</Alert>}
            {updateRegionMutation.isError && <Alert color="danger">Updating region: {errorMessage(updateRegionMutation.error)}</Alert>}
            {deleteRegionMutation.isError && <Alert color="danger">Deleting region: {errorMessage(deleteRegionMutation.error)}</Alert>}
            {!areas.data.length && <p className="text-muted">No areas defined for this region yet.</p>}
            <div className="d-flex flex-wrap">
                {areas.data.map((a, index) => <AreaCard
                    key={a.id}
                    area={a} 
                    regionId={regionId} 
                    countryId={countryId}
                    index={index}
                    onReorder={doReorderAreas}
                />)}
                {isAddAreaModalOpen && <CreateModal
                    object={{
                        id: 0,
                        regionId,
                        name: '',
                        exploreStatus: ExploreStatus.None,
                        notes: '',
                        links: '',
                        places: [],
                        order: (areas.data?.length || 0) + 1,
                        lat: 0,
                        lng: 0,
                        zoom: 0
                    } as Area}
                    title="Add area"
                    onClose={() => setAddAreaModalOpen(!isAddAreaModalOpen)}
                    onSubmit={(area) => createAreaMutation.mutate(area)}
                />}
            </div>
        </Container>
    </div>;
};

export default RegionPage;