import * as React from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useCallback, useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import NavBar from '../components/NavBar';
import { errorMessage } from '../util';
import SearchString from '../components/SearchString';
import { useSearchQuery } from '../data/queries';
import Paginator from '../components/Paginator';
import { getUrl, SEARCHABLE_TABLES_NAMES } from '../api/search';
import useTitle from '../hooks/useTitle';

const PAGE_SIZE = 25;

const Search = () => {
    const [searchParams, setSearchParams] = useSearchParams();
    const query = searchParams.get('q') || '';
    const tables = searchParams.get('tables') || '';
    const pageString = searchParams.get('page');
    const page = (!pageString || isNaN(parseInt(pageString))) ? 0 : parseInt(pageString) - 1;
    const setQuery = useCallback((query: string, tables: string) => setSearchParams((params) => {
        params.set('q', query);
        if (tables) {
            params.set('tables', tables);
        } else {
            params.delete('tables');
        }
        params.delete('page');
        return params;
    }), [setSearchParams]);
    const setPage = useCallback((page: number) => setSearchParams((params) => {
        window.scrollTo(0, 0);
        params.set('page', (page + 1).toString());
        return params;
    }), [setSearchParams]);
    const offset = page * PAGE_SIZE;

    const searchQuery = useSearchQuery({
        q: query,
        tables,
        limit: PAGE_SIZE,
        offset
    });

    const total = searchQuery.data ? searchQuery.data.total : 0;
    const totalPages = Math.ceil(total / PAGE_SIZE);
    
    const navigate = useNavigate();
    const [isNavigating, setIsNavigating] = useState(false);
    const navigateToResult = async (tableName: string, tableId: number) => {
        setIsNavigating(true);
        const url = await getUrl(tableName, tableId);
        navigate(url);
    };
    
    useTitle(() => query ? `Search: '${query}'` : 'Search', [query]);

    return <div>
        <NavBar>
            <h3>Search</h3>
            {isNavigating && <h3 className="ms-auto"><Spinner type="grow" size="sm"/></h3>}
        </NavBar>
        <Container>
            <SearchString initialValue={query} initialTables={tables} onSearch={setQuery} />
            {!query && <h4 className="text-center">Enter a query for full-text search.</h4>}
            {query && searchQuery.isLoading && <h3 className="text-center">
                <Spinner type="grow" /> Searching
            </h3>}
            {query && searchQuery.isError && <Alert color="danger">Search: {errorMessage(searchQuery.error)}</Alert>}
            {query && !searchQuery.isLoading && searchQuery.data && !!total && <>
                <h5 className="text-center mb-4">{total} result(s)</h5>
                <ol start={offset + 1}>
                    {searchQuery.data.data.map((result, key) => <li key={key} className="mb-2">
                        <a href="javascript:void(0)" onClick={() => navigateToResult(result.tableName, result.tableId)}>
                            <b>{result.title}</b>
                        </a> <i className="text-muted">({SEARCHABLE_TABLES_NAMES[result.tableName].toLowerCase()})</i>
                        <br/>
                        <span className="small" dangerouslySetInnerHTML={{ __html: result.text }} />
                    </li>)}
                </ol>
                <Paginator page={page} setPage={setPage} totalPages={totalPages} />
            </>}
            {query && !searchQuery.isLoading && searchQuery.data && !total && <h4 className="text-center">No results.</h4>}
        </Container>
    </div>;
};

export default Search;