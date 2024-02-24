import * as React from 'react';
import { Alert, Container, Pagination, PaginationItem, PaginationLink, Spinner } from 'reactstrap';
import { useSearchParams } from 'react-router-dom';
import NavBar from '../components/NavBar';
import PicturesList, { PicturesViewMode } from '../components/PicturesList';
import { usePicturesQuery } from '../data/queries';
import { errorMessage } from '../util';
import { GetPicturesOptions } from '../api/pictures';
import { useCallback, useEffect } from 'react';

const THUMBNAILS_PAGE_SIZE = 100;

const Pictures = () => {
    // view options from search params
    const [searchParams, setSearchParams] = useSearchParams();
    const pageString = searchParams.get('page');
    const page = (!pageString || isNaN(parseInt(pageString))) ? 0 : parseInt(pageString) - 1;
    // fixed for now
    const viewMode = PicturesViewMode.THUMBNAILS;
    
    // pictures list
    const pageSize = THUMBNAILS_PAGE_SIZE;
    const pictureQueryOptions: GetPicturesOptions = {
        limit: pageSize,
        offset: page * pageSize
    };
    const picturesQuery = usePicturesQuery(pictureQueryOptions);
    
    // pagination
    const totalPictures = picturesQuery.data ? picturesQuery.data.total : 0;
    const totalPages = Math.ceil(totalPictures / pageSize);
    const setPage = useCallback((page: number) => {
        setSearchParams((params) => {
            if (page) {
                params.set('page', (page + 1).toString());
            } else {
                params.delete('page');
            }
            return params;
        });
        window.scrollTo(0, 0);
    }, []);

    /// keyboard nav ///

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.ctrlKey && e.key === 'Home' && page > 0) {
                e.preventDefault();
                setPage(0);
            }
            if (e.ctrlKey && e.key === 'ArrowLeft' && page > 0) {
                e.preventDefault();
                setPage(page - 1);
            }
            if (e.ctrlKey && e.key === 'ArrowRight' && page < totalPages - 1) {
                e.preventDefault();
                setPage(page + 1);
            }
            if (e.ctrlKey && e.key === 'End' && page < totalPages - 1) {
                e.preventDefault();
                setPage(totalPages - 1);
            }
        };
        document.addEventListener('keydown', handler);
        return () => document.removeEventListener('keydown', handler);
    }, [page, totalPages, setPage]);
    
    /// render ///

    return <div>
        <NavBar>
            <h3>
                <i className="bi bi-image"></i>
                &nbsp;&rsaquo;&nbsp;
                Pictures
                &nbsp;&rsaquo;&nbsp;
                All
            </h3>
            {picturesQuery.data && <h3 className="ms-auto">{picturesQuery.data.total} picture(s)</h3>}
        </NavBar>
        <Container>
            {picturesQuery.isError && <Alert color="danger">Loading pictures: {errorMessage(picturesQuery.error)}</Alert>}
            {picturesQuery.isLoading && !picturesQuery.isSuccess && <h3 className="text-center">
                <Spinner type="grow" /> Loading pictures...
            </h3>}
            {picturesQuery.isSuccess && <>
                {picturesQuery.data.data.length && <PicturesList
                    pictures={picturesQuery.data.data}
                    viewMode={viewMode}
                />}
                {!picturesQuery.data.data.length && <h4 className="text-center">
                    No pictures in the current view.
                </h4>}
                {totalPages > 1 && <Pagination className="m-auto d-flex justify-content-center">
                    <PaginationItem disabled={page === 0}>
                        <PaginationLink onClick={() => setPage(0)}>
                            <i className="bi bi-rewind-fill" title="Ctrl-Home" />
                        </PaginationLink>
                    </PaginationItem>
                    <PaginationItem disabled={page === 0}>
                        <PaginationLink onClick={() => setPage(page - 1)}>
                            <i className="bi bi-caret-left-fill" title="Ctrl-Left" />
                        </PaginationLink>
                    </PaginationItem>
                    {Array.from(Array(totalPages).keys()).map(p => <PaginationItem key={p} active={p === page}>
                        <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
                    </PaginationItem>)}
                    <PaginationItem disabled={page === totalPages - 1}>
                        <PaginationLink onClick={() => setPage(page + 1)}>
                            <i className="bi bi-caret-right-fill" title="Ctrl-Right" />
                        </PaginationLink>
                    </PaginationItem>
                    <PaginationItem disabled={page === totalPages - 1}>
                        <PaginationLink onClick={() => setPage(totalPages - 1)}>
                            <i className="bi bi-fast-forward-fill" title="Ctrl-End" />
                        </PaginationLink>
                    </PaginationItem>
                </Pagination>}
            </>}
        </Container>
    </div>
};

export default Pictures;