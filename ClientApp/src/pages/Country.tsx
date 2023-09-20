import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { useParams } from 'react-router-dom';
import { errorMessage } from '../util';
import CreateModal from '../components/CreateModal';
import Region from '../model/Region';
import NavBar from '../components/NavBar';
import { useCountriesQuery, useRegionsQuery } from '../data/queries';
import { useCreateRegionMutation, useReorderRegionsMutation } from '../data/mutations';
import RegionCard from '../components/RegionCard';

const Country = () => {
    const countryId = parseInt(useParams()['countryId']!);
    
    const countries = useCountriesQuery();
    const regions = useRegionsQuery(countryId);

    const [isAddRegionModalOpen, setAddRegionModalOpen] = useState(false);

    const country = countries.isSuccess ? countries.data.find(c => c.id === countryId) : null;

    const createRegionMutation = useCreateRegionMutation();
    const reorderRegionsMutation = useReorderRegionsMutation();
    
    if (countries.isError) {
        return <Alert color="danger">Loading countries: {errorMessage(countries.error)}</Alert>;
    }
    if (regions.isError) {
        return <Alert color="danger">Loading regions: {errorMessage(regions.error)}</Alert>;
    }
    if (countries.isLoading || regions.isLoading) {
        return <NavBar><h3><Spinner type="grow" size="sm" /> Loading...</h3></NavBar>;
    }
    if (!country) {
        return <Alert color="warning">Country not found</Alert>;
    }
    
    const doReorderRegions = (from: number, to: number) => {
        const newRegons = [...regions.data];
        newRegons.splice(from, 1);
        newRegons.splice(to, 0, regions.data[from]);
        reorderRegionsMutation.mutate(newRegons);
    };

    return <div>
        <NavBar>
            <h3>{country.flagEmoji} {country.name}</h3>
            <Button color="primary" className="ms-auto" onClick={() => setAddRegionModalOpen(true)}>Add region...</Button>
        </NavBar>
        <Container>
            {createRegionMutation.isError && <Alert color="danger">Creating region: {errorMessage(createRegionMutation.error)}</Alert>}
            {reorderRegionsMutation.isError && <Alert color="danger">Reordering regions: {errorMessage(reorderRegionsMutation.error)}</Alert>}
            {!regions.data.length && <p className="text-muted">No regions defined for this country yet.</p>}
            <div className="d-flex flex-wrap">
                {regions.data.map((r, index) => <RegionCard key={r.id} region={r} index={index} onReorder={doReorderRegions} />)}
            </div>
            {isAddRegionModalOpen && <CreateModal
                object={{
                    id: 0,
                    name: '',
                    countryId,
                    order: (regions.data?.length || 0) + 1
                } as Region}
                title="Add region"
                onClose={() => setAddRegionModalOpen(false)}
                onSubmit={(region) => createRegionMutation.mutate(region)}
            />}
        </Container>
    </div>;
};

export default Country;
