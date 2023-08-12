import * as React from 'react';
import { Alert, Button, Card, CardBody, CardTitle, Container, Spinner } from 'reactstrap';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { errorMessage } from '../util';
import ExploreStatusIndicator from '../components/ExploreStatusIndicator';
import Area from '../model/Area';
import ExploreStatus from '../model/ExploreStatus';
import { useState } from 'react';
import NavBar from '../components/NavBar';
import { confirmModal } from '../components/ModalContainer';
import EditableInline from '../components/EditableInline';
import { useAreasQuery, useCountriesQuery, useRegionsQuery } from '../data/queries';
import {
    useCreateAreaMutation,
    useDeleteRegionMutation,
    useUpdateAreaMutation,
    useUpdateRegionMutation
} from '../data/mutations';
import CreateModal from '../components/CreateModal';

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
            {updateRegionMutation.isError && <Alert color="danger">Updating region: {errorMessage(updateRegionMutation.error)}</Alert>}
            {deleteRegionMutation.isError && <Alert color="danger">Deleting region: {errorMessage(deleteRegionMutation.error)}</Alert>}
            {!areas.data.length && <p className="text-muted">No areas defined for this region yet.</p>}
            <div className="d-flex flex-wrap">
                {areas.data.map(a => <div key={a.id} className="w-25 pb-1 pe-1">
                    <Card>
                        <CardBody>
                            <CardTitle tag="h5" className="d-flex align-items-start">
                                <div className="flex-shrink-0">
                                <ExploreStatusIndicator
                                    status={a.exploreStatus}
                                    onChange={(status) => updateAreaMutation.mutate({ ...a, exploreStatus: status })}
                                />
                                </div>
                                <div>&nbsp;</div>
                                <div><Link to={`/country/${countryId}/region/${regionId}/area/${a.id}`}>{a.name}</Link></div>
                            </CardTitle>
                        </CardBody>
                    </Card>
                </div>)}
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