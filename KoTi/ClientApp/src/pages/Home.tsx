import * as React from 'react';
import { Alert, Card, CardBody, CardTitle, Container, Spinner } from 'reactstrap';
import qs from 'qs';
import { errorMessage } from '../util';
import { Link, useNavigate } from 'react-router-dom';
import NavBar from '../components/NavBar';
import { useCountriesQuery, useStatsQuery } from '../data/queries';
import SearchString from '../components/SearchString';
import useTitle from '../hooks/useTitle';

const Home = () => {
    const stats = useStatsQuery();
    const countries = useCountriesQuery();
    const navigate = useNavigate();
    
    useTitle();

    const search = (query: string, tables: string) => {
        navigate('/search?' + qs.stringify({ q: query, tables }));
    };
    
    return <div>
        <NavBar />
        <Container>
            <p className="text-center">
                This is <b>KoTi</b> <i>(Kohteiden tietokanta, Finn. Place database)</i>,
                the backoffice application for <code>pohjoiseen.fi</code> and a personal photo and PoI database.
            </p>
            <SearchString onSearch={search} initialTables="Places,Areas,Regions,Countries,PictureSets,Posts,Articles" />
            {stats.isError && <Alert color="danger">Loading stats: {errorMessage(countries.error)}</Alert>}
            {stats.isLoading && <div className="text-center"><Spinner type="grow" size="sm"/> Loading...</div>}
            {!stats.isLoading && <>
                <h3>Blog{stats.data && <> (last published: <b>{new Date(stats.data?.databaseLastPublishedAt).toString().replace(/ \([^)]+\)$/, '')}</b>)</>}:</h3>
                <div className="d-flex flex-wrap">
                    <div className="w-25 pb-1 pe-1">
                        <Card><CardBody><CardTitle>
                            <h2>
                                <i className="bi bi-file-earmark-richtext"/>
                                &nbsp;
                                <Link to="/blog">{stats.data ? `${stats.data.totalPosts} post(s)` : 'Loading...'}</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                    <div className="w-25 pb-1 pe-1">
                        <Card><CardBody><CardTitle>
                            <h2>
                                <i className="bi bi-file-earmark"/>
                                &nbsp;
                                <Link to="/articles">{stats.data ? `${stats.data.totalArticles} article(s)` : 'Loading...'}</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                    <div className="w-25 pb-1 pe-1">
                        <Card><CardBody><CardTitle>
                            <h2>
                                <i className="bi bi-arrow-90deg-right"/>
                                &nbsp;
                                <Link to="/redirects">Redirects</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                    <div className="w-25 pb-1 pe-1">
                        <Card><CardBody><CardTitle>
                            <h2 className="m-0">
                                <i className="bi bi-check-lg"/>
                                &nbsp;
                                <Link to="/publish">Publish</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                </div>
                <h3 className="mt-3">Pictures{stats.data && <>
                    {' '}(total: <b>{stats.data.totalPictures}</b>,
                    non-geolocated: <b>{stats.data.totalPicturesWithNoLocation}</b>)
                </>}</h3>
                <div className="d-flex flex-wrap">
                    <div className="w-25 pb-1 pe-1">
                        <Card><CardBody><CardTitle>
                            <h2>
                                <i className="bi bi-images"/>
                                &nbsp;
                                <Link to="/pictures/all">All pictures</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                    <div className="w-25 pb-1 pe-1">
                        <Card><CardBody><CardTitle>
                            <h2>
                                <i className="bi bi-folder"/>
                                &nbsp;
                                <Link to="/pictures/folders">By folder</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                    <div className="w-25 pb-1 pe-1">
                        <Card className="overflow-hidden"><CardBody><CardTitle>
                            <h2>
                                <i className="bi bi-upload"/>
                                &nbsp;
                                <Link to="/pictures/upload">Upload</Link>
                            </h2>
                        </CardTitle></CardBody></Card>
                    </div>
                </div>
                <h3 className="mt-3">Places{stats.data && <>
                    {' '}(<b>{stats.data.totalPlaces}</b>)
                </>}:</h3>
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
            </>}
            <div className="mt-4 text-center text-muted small"> 
                &copy; 2023-2025 Alexander Ulyanov.  Built on ASP.NET Core 9.0 + SQLite + React + TanStack Query + Bootstrap<br/>
                Version {process.env.REACT_APP_VERSION}, revision {process.env.REACT_APP_GIT_SHA} from {process.env.REACT_APP_GIT_DATE}
                {stats.data && <> <i className="bi bi-dot" /> Database size: {(stats.data.databaseSize / 1048576).toFixed(1)} MB</>}
            </div>
        </Container>
    </div>
;
};

export default Home;
