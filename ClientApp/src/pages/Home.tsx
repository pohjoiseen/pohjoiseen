import * as React from 'react';
import { Alert, Card, CardBody, CardTitle, Container, Spinner } from 'reactstrap';
import { errorMessage } from '../util';
import { Link } from 'react-router-dom';
import NavBar from '../components/NavBar';
import { useCountriesQuery } from '../data/queries';

const Home = () => {
    const countries = useCountriesQuery();

    return <div>
        <NavBar />
        <Container>
            <p>
                Welcome to KoTi <i>(Kohteiden tietokanta, Finn. Place database)</i>!
                This is Alexander Ulyanov's private database of places where he has and hasn't been,
                and pictures from them.
            </p>
            <h3>Pictures</h3>
            <div className="d-flex flex-wrap">
                <div className="w-25 pb-1 pe-1">
                    <Card>
                        <CardBody>
                            <CardTitle>
                                <h2>
                                    <i className="bi bi-upload" />
                                    &nbsp;
                                    <Link to="/pictures/upload">Upload</Link>
                                </h2>
                            </CardTitle>
                        </CardBody>
                    </Card>
                </div>
            </div>
            <h3 className="mt-3">Countries</h3>
            {countries.isError && <Alert color="danger">{errorMessage(countries.error)}</Alert>}
            {countries.isLoading && <div><Spinner type="grow" size="sm"/> Loading...</div>}
            {countries.isSuccess && <div className="d-flex flex-wrap">
                {countries.data.map(c => <div key={c.id} className="w-25 pb-1 pe-1">
                    <Card>
                        <CardBody>
                            <CardTitle>
                                <h2>
                                {c.flagEmoji}
                                    &nbsp;
                                    <Link to={`/country/${c.id}`}>{c.name}</Link>
                                </h2>
                            </CardTitle>
                        </CardBody>
                    </Card>
                </div>)}
            </div>}
        </Container>
    </div>
;
};

export default Home;
