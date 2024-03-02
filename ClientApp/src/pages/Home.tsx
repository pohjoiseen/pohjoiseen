import * as React from 'react';
import { Alert, Card, CardBody, CardTitle, Container, Spinner } from 'reactstrap';
import qs from 'qs';
import { errorMessage } from '../util';
import { Link, useNavigate } from 'react-router-dom';
import NavBar from '../components/NavBar';
import { useCountriesQuery } from '../data/queries';
import SearchString from '../components/SearchString';

const Home = () => {
    const countries = useCountriesQuery();
    const navigate = useNavigate();

    const search = (query: string, tables: string) => {
        navigate('/search?' + qs.stringify({ q: query, tables }));
    };
    
    return <div>
        <NavBar />
        <Container>
            <p>
                Welcome to KoTi <i>(Kohteiden tietokanta, Finn. Place database)</i>!
                This is Alexander Ulyanov's private database of places where he has and hasn't been,
                and pictures from them.
            </p>
            <SearchString onSearch={search} initialTables="Places,Areas,Regions,Countries" />
            <h3>Browse pictures:</h3>
            <div className="d-flex flex-wrap">
                <div className="w-25 pb-1 pe-1">
                    <Card><CardBody><CardTitle>
                        <h2>
                            <i className="bi bi-images"/>
                            &nbsp;
                            <Link to="/pictures">All pictures</Link>
                        </h2>
                    </CardTitle></CardBody></Card>
                </div>
                <div className="w-25 pb-1 pe-1">
                    <Card className="overflow-hidden"><CardBody className="bg-body-secondary"><CardTitle>
                        <h2>
                            <i className="bi bi-upload"/>
                            &nbsp;
                            <Link to="/pictures/upload">Upload</Link>
                        </h2>
                    </CardTitle></CardBody></Card>
                </div>
            </div>
            <h3 className="mt-3">Browse places:</h3>
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
            <div className="mt-4 text-center text-muted small">
                &copy; 2023-2024 Alexander Ulyanov.  Built on ASP.NET Core 8.0 + React + TanStack Query + Bootstrap
            </div>
        </Container>
    </div>
;
};

export default Home;
