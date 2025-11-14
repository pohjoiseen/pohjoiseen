import * as React from 'react';
import { useEffect, useState } from 'react';
import { Container, Spinner } from 'reactstrap';
import { useNavigate, useParams } from 'react-router-dom';
import NavBar from '../components/NavBar';
import { getPlace } from '../api/places';
import { getArea } from '../api/areas';
import { getRegion } from '../api/regions';
import useTitle from '../hooks/useTitle';

const PlaceRedirect = () => {
    const placeId = parseInt(useParams()['placeId']!);
    const [error, setError] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        (async () => {
            try {
                const place = await getPlace(placeId);
                const area = await getArea(place.areaId);
                const region = await getRegion(area.regionId);
                const url = `/country/${region.countryId}/region/${region.id}/area/${area.id}?place=${placeId}`;
                navigate(url);
            } catch (e) {
                if (e instanceof Error) {
                    setError(e.message);
                } else {
                    setError('Failed to load place data.');
                }
            }
        })();
    }, [navigate, placeId]);
    
    useTitle(() => error || false, [error]);

    return <div>
        <NavBar>
            <h3>{error || <><Spinner type="grow" /> Loading...</>}</h3> 
        </NavBar>
        <Container/>
    </div>;
}

export default PlaceRedirect;
    