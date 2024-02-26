import * as React from 'react';
import { useCallback, useEffect, useRef } from 'react';
import {
    Alert,
    Container,
    DropdownItem,
    DropdownMenu,
    DropdownToggle,
    Pagination,
    PaginationItem,
    PaginationLink,
    Spinner,
    UncontrolledDropdown
} from 'reactstrap';
import { useSearchParams } from 'react-router-dom';
import NavBar from '../components/NavBar';
import PicturesList from '../components/PicturesList';
import { getPictureFromCache, usePictureQuery, usePicturesQuery } from '../data/queries';
import { errorMessage } from '../util';
import { GetPicturesOptions } from '../api/pictures';
import { PicturesViewMode } from '../components/pictureViewCommon';
import PictureFullscreen from '../components/PictureFullscreen';
import { useQueryClient } from '@tanstack/react-query';
import { useUpdatePictureMutation } from '../data/mutations';
import Paginator from '../components/Paginator';

const PAGE_SIZES: { [mode in PicturesViewMode ]: number } = {
    [PicturesViewMode.THUMBNAILS]: 100,
    [PicturesViewMode.DETAILS]: 25,
};

const QUERY_PARAMS = {
    PAGE: 'page',
    VIEW_MODE: 'mode',
    FULLSCREEN: 'fullscreen'
}

const Pictures = () => {
    const queryClient = useQueryClient();
    
    // view options from search params
    const [searchParams, setSearchParams] = useSearchParams();
    const pageString = searchParams.get(QUERY_PARAMS.PAGE);
    const page = (!pageString || isNaN(parseInt(pageString))) ? 0 : parseInt(pageString) - 1;
    const viewModeString = searchParams.get(QUERY_PARAMS.VIEW_MODE);
    const viewMode: PicturesViewMode = viewModeString ? viewModeString as PicturesViewMode : PicturesViewMode.THUMBNAILS;
    const fullscreenString = searchParams.get(QUERY_PARAMS.FULLSCREEN);
    const currentFullscreen = (!fullscreenString || isNaN(parseInt(fullscreenString))) ? -1 : parseInt(fullscreenString);
    const isFullscreen = currentFullscreen !== -1;
    const preloadRef = useRef<HTMLImageElement[]>([]);

    // pictures list
    const pageSize = PAGE_SIZES[viewMode];
    const offset = page * pageSize;
    const pictureQueryOptions: GetPicturesOptions = {
        limit: pageSize,
        offset
    };
    const picturesQuery = usePicturesQuery(pictureQueryOptions);
    
    const updatePictureMutation = useUpdatePictureMutation();
    
    /// pagination ///
    
    const totalPictures = picturesQuery.data ? picturesQuery.data.total : 0;
    const totalPages = Math.ceil(totalPictures / pageSize);
    const setPage = useCallback((page: number, fullscreen?: number) => {
        setSearchParams((params) => {
            if (page) {
                params.set(QUERY_PARAMS.PAGE, (page + 1).toString());
            } else {
                params.delete(QUERY_PARAMS.PAGE);
            }
            if (fullscreen !== undefined && fullscreen >= 0) {
                params.set(QUERY_PARAMS.FULLSCREEN, fullscreen.toString());
            } else {
                params.delete(QUERY_PARAMS.FULLSCREEN);
            }
            return params;
        });
        if (!isFullscreen) {
            window.scrollTo(0, 0);
        }
    }, [setSearchParams, isFullscreen]);

    /// switching modes ///
    
    const getPageForOffset = useCallback((mode: PicturesViewMode, offset: number) => {
        let page = Math.floor(offset / PAGE_SIZES[mode]);
        if (page >= Math.ceil(totalPictures / PAGE_SIZES[mode])) {
            page = Math.ceil(totalPictures / PAGE_SIZES[mode]) - 1;
        }
        return page;
    }, [totalPictures]);

    const setViewMode = useCallback((mode: PicturesViewMode) => {
        const page = getPageForOffset(mode, offset);
        setSearchParams((params) => {
            params.set(QUERY_PARAMS.VIEW_MODE, mode);
            params.delete(QUERY_PARAMS.FULLSCREEN);
            if (page) {
                params.set(QUERY_PARAMS.PAGE, (page + 1).toString());
            } else {
                params.delete(QUERY_PARAMS.PAGE);
            }
            return params;
        });
    }, [setSearchParams, viewMode, offset, currentFullscreen, getPageForOffset]);

    /// fullscreen mode ///
    
    const enterFullscreen = useCallback((index: number) => {
        setSearchParams((params) => {
            params.set(QUERY_PARAMS.FULLSCREEN, index.toString());
            return params;
        });
        
        if (picturesQuery.isFetching || !picturesQuery.data) {
            return;
        }
        
        // preload 5 pictures after and before the newly opened one.
        // Save them in a ref so browser keeps a reference to them.
        // Won't work when changing pages (where there would be an extra delay to load new pictures anyway)
        // but let's keep it as is for now.  Use query cache directly, load only what's in there
        const preloadImages: HTMLImageElement[] = [];
        for (let i = 1; i <= 5; i++) {
            let k = index + i;
            if (k < picturesQuery.data.data.length) {
                const picture = getPictureFromCache(queryClient, picturesQuery.data.data[k]);
                if (picture) {
                    const img = new Image();
                    img.src = picture.url;
                    preloadImages.push(img);
                }
            }
            k = index - 1;
            if (k > 0) {
                const picture = getPictureFromCache(queryClient, picturesQuery.data.data[k]);
                if (picture) {
                    const img = new Image();
                    img.src = picture.url;
                    preloadImages.push(img);
                }
            }
        }
        preloadRef.current = preloadImages;
    }, [setSearchParams, picturesQuery]);
    
    const exitFullscreen = useCallback(() => {
        setSearchParams((params) => {
            params.delete(QUERY_PARAMS.FULLSCREEN);
            return params;
        });
    }, [setSearchParams]);
    
    // query would be actually executed only when we have an id
    const fullscreenPictureId = picturesQuery.data && currentFullscreen >= 0 ? picturesQuery.data.data[currentFullscreen] : 0;
    const fullscreenPictureQuery = usePictureQuery(fullscreenPictureId,
        !(picturesQuery.data && currentFullscreen >= 0));
    
    /// keyboard nav ///

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            // fullscreen mode
            if (isFullscreen) {
                if (e.key === 'Escape') {
                    e.preventDefault();
                    exitFullscreen();
                }
                // left/right keyboard nav
                if (picturesQuery.data) {
                    if (e.key === 'ArrowLeft' && (e.target as any).tagName !== 'INPUT') {
                        if (currentFullscreen > 0) {
                            enterFullscreen(currentFullscreen - 1);
                        } else if (page > 0) {
                            setPage(page - 1, pageSize - 1);
                        }
                    }
                    if (e.key === 'ArrowRight' && (e.target as any).tagName !== 'INPUT') {
                        if (currentFullscreen < picturesQuery.data.data.length - 1) {
                            enterFullscreen(currentFullscreen + 1);
                        } else if (page < totalPages - 1) {
                            setPage(page + 1, 0);
                        }
                    }
                }
            }
        };
        document.addEventListener('keydown', handler);
        return () => document.removeEventListener('keydown', handler);
    }, [page, totalPages, setPage, isFullscreen, exitFullscreen, picturesQuery]);
    
    /// render ///

    return <div>
        <NavBar>
            <h3>
                <i className="bi bi-image"></i>
                &nbsp;&rsaquo;&nbsp;
                All pictures
            </h3>
            <h3 className="ms-auto">{picturesQuery.data && <>{picturesQuery.data.total} picture(s)</>}</h3>
            <UncontrolledDropdown className="ms-2">
                <DropdownToggle caret>View</DropdownToggle>
                <DropdownMenu>
                    <DropdownItem onClick={() => setViewMode(PicturesViewMode.THUMBNAILS)}>Thumbnails</DropdownItem>
                    <DropdownItem onClick={() => setViewMode(PicturesViewMode.DETAILS)}>Details</DropdownItem>
                </DropdownMenu>
            </UncontrolledDropdown>
        </NavBar>
        <Container>
            {picturesQuery.isError && <Alert color="danger">Loading pictures: {errorMessage(picturesQuery.error)}</Alert>}
            {updatePictureMutation.isError && <Alert color="danger" className="alert-fixed">Updating picture: {errorMessage(updatePictureMutation.error)}</Alert>}
            {picturesQuery.isLoading && !picturesQuery.isSuccess && <h3 className="text-center">
                <Spinner type="grow" /> Loading pictures...
            </h3>}
            {picturesQuery.isSuccess && <>
                {picturesQuery.data.data.length > 0 && <PicturesList
                    pictures={picturesQuery.data.data}
                    viewMode={viewMode}
                    currentIndex={currentFullscreen}
                    onOpen={enterFullscreen}
                />}
                {!picturesQuery.data.data.length && <h4 className="text-center">
                    No pictures in the current view.
                </h4>}
                <Paginator page={page} setPage={setPage} totalPages={totalPages} disableKeyboardNav={isFullscreen} />
                {isFullscreen && <PictureFullscreen
                    picture={fullscreenPictureQuery.data}
                    isError={fullscreenPictureQuery.isError}
                    isLoading={fullscreenPictureQuery.isLoading}
                />}
            </>}
        </Container>
    </div>
};

export default Pictures;