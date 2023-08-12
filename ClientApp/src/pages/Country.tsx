import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Card, CardBody, CardTitle, Container, Spinner } from 'reactstrap';
import { Link, useParams } from 'react-router-dom';
import { errorMessage } from '../util';
import CreateModal from '../components/CreateModal';
import Region from '../model/Region';
import NavBar from '../components/NavBar';
import { useCountriesQuery, useRegionsQuery } from '../data/queries';
import { useCreateRegionMutation } from '../data/mutations';

const Country = () => {
    const countryId = parseInt(useParams()['countryId']!);
    
    const countries = useCountriesQuery();
    const regions = useRegionsQuery(countryId);

    const [isAddRegionModalOpen, setAddRegionModalOpen] = useState(false);

    const country = countries.isSuccess ? countries.data.find(c => c.id === countryId) : null;

    const createRegionMutation = useCreateRegionMutation();
    
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

    return <div>
        <NavBar>
            <h3>{country.flagEmoji} {country.name}</h3>
            <Button color="primary" className="ms-auto" onClick={() => setAddRegionModalOpen(true)}>Add region...</Button>
        </NavBar>
        <Container>
            {createRegionMutation.isError && <Alert color="danger">Creating region: {errorMessage(createRegionMutation.error)}</Alert>}
            {!regions.data.length && <p className="text-muted">No regions defined for this country yet.</p>}
            <div className="d-flex flex-wrap">
                {regions.data.map(r => <div key={r.id} className="w-25 pb-1 pe-1">
                    <Card>
                        <CardBody>
                            <CardTitle tag="h5"><Link to={`/country/${countryId}/region/${r.id}`}>{r.name}</Link></CardTitle>
                        </CardBody>
                    </Card>
                </div>)}
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
