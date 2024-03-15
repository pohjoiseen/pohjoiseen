import * as React from 'react';
import { ReactNode, useCallback, useEffect, useRef, useState } from 'react';
import {
    Alert,
    Container,
    DropdownItem,
    DropdownMenu,
    DropdownToggle,
    Spinner,
    UncontrolledDropdown
} from 'reactstrap';
import { useSearchParams } from 'react-router-dom';
import NavBar from '../components/NavBar';
import PicturesList from '../components/PicturesList';
import { getPictureFromCache, usePictureQuery, usePictureSetQuery, usePicturesQuery } from '../data/queries';
import { errorMessage } from '../util';
import { GetPicturesOptions } from '../api/pictures';
import { PicturesViewMode } from '../components/pictureViewCommon';
import PictureFullscreen from '../components/PictureFullscreen';
import { useQueryClient } from '@tanstack/react-query';
import {
    useCreatePictureSetMutation, useMovePicturesToPictureSetMutation,
    useUpdatePictureMutation
} from '../data/mutations';
import Paginator from '../components/Paginator';
import CreateModal from '../components/CreateModal';
import PictureSet from '../model/PictureSet';
import PictureSetList from '../components/PictureSetList';
import MoveToPictureSetModal from '../components/MoveToPictureSetModal';
import { SEARCHABLE_TABLES } from '../api/search';
import PictureFilters from '../components/PictureFilters';
import ViewModeSwitcher from '../components/ViewModeSwitcher';
import { getDefaultViewMode, setDefaultViewMode } from '../data/localStorage';

const PAGE_SIZES: { [mode in PicturesViewMode ]: number } = {
    [PicturesViewMode.THUMBNAILS]: 100,
    [PicturesViewMode.DETAILS]: 25,
};

const QUERY_PARAMS = {
    PAGE: 'page',
    VIEW_MODE: 'mode',
    FULLSCREEN: 'fullscreen',
    SET_ID: 'folderId',
    OBJECT_TABLE: 'objectTable',
    OBJECT_ID: 'objectId',
    OBJECT_NAME: 'objectName'
}

const Pictures = ({ sets }: { sets: boolean }) => {
    const queryClient = useQueryClient();
    
    // view options from search params
    const [searchParams, setSearchParams] = useSearchParams();
    const pageString = searchParams.get(QUERY_PARAMS.PAGE);
    const page = (!pageString || isNaN(parseInt(pageString))) ? 0 : parseInt(pageString) - 1;
    const viewModeString = searchParams.get(QUERY_PARAMS.VIEW_MODE);
    const viewMode: PicturesViewMode = viewModeString ? viewModeString as PicturesViewMode : getDefaultViewMode();
    const fullscreenString = searchParams.get(QUERY_PARAMS.FULLSCREEN);
    const currentFullscreen = (!fullscreenString || isNaN(parseInt(fullscreenString))) ? -1 : parseInt(fullscreenString);
    const isFullscreen = currentFullscreen !== -1;
    const preloadRef = useRef<HTMLImageElement[]>([]);
    const setIdString = searchParams.get(QUERY_PARAMS.SET_ID);
    const setId = sets ? (setIdString ? parseInt(setIdString) : 0) : null;
    const objectTableString = searchParams.get(QUERY_PARAMS.OBJECT_TABLE) || '';
    const objectTable: typeof SEARCHABLE_TABLES[number] = objectTableString ? objectTableString as typeof SEARCHABLE_TABLES[number] : 'Areas';
    const objectIdString = searchParams.get(QUERY_PARAMS.OBJECT_ID);
    const objectId = objectIdString ? parseInt(objectIdString) : null;
    const objectName = searchParams.get(QUERY_PARAMS.OBJECT_NAME);
    
    const setObjectFilter = useCallback((table: string, id: number | null, name: string | null) => {
        setSearchParams((params) => {
            if (id) {
                params.set(QUERY_PARAMS.OBJECT_TABLE, table);
                params.set(QUERY_PARAMS.OBJECT_ID, id.toString());
                params.set(QUERY_PARAMS.OBJECT_NAME, name || '')
            } else {
                params.set(QUERY_PARAMS.OBJECT_TABLE, table);
                params.delete(QUERY_PARAMS.OBJECT_ID);
                params.delete(QUERY_PARAMS.OBJECT_NAME);
            }
            return params;
        });
    }, [setSearchParams]);
    
    // pictures list
    const pageSize = PAGE_SIZES[viewMode];
    const offset = page * pageSize;
    const pictureQueryOptions: GetPicturesOptions = {
        limit: pageSize,
        placeId: objectTable === 'Places' && objectId ? objectId : undefined,
        areaId: objectTable === 'Areas' && objectId ? objectId : undefined,
        offset
    };
    if (typeof setId === 'number') {
        pictureQueryOptions.setId = setId;
    }
    const pictureSetQuery = usePictureSetQuery(setId);
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
        setSelection([]);
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
        setSelection([]);
        setDefaultViewMode(mode);
    }, [setSearchParams, offset, getPageForOffset]);

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
    }, [setSearchParams, picturesQuery, queryClient]);
    
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
    }, [page, totalPages, setPage, isFullscreen, exitFullscreen, picturesQuery, currentFullscreen, enterFullscreen, pageSize]);

    /// selection ///

    const [selection, setSelection] = useState<boolean[]>([]);
    const numSelected = selection.filter(s => s).length;
    
    /// picture sets ///
    
    const [isAddPictureSetModalOpen, setAddPictureSetModalOpen] = useState(false);
    const [isMovePictureToPictureSetModalOpen, setMovePictureToPictureSetModalOpen] = useState(false);
    const createPictureSetMutation = useCreatePictureSetMutation();
    const moveToPictureSetMutation = useMovePicturesToPictureSetMutation();
    const movePicturesToPictureSet = useCallback((pictureSetId: number | null) => {
        moveToPictureSetMutation.mutate({
            id: pictureSetId,
            pictureIds: picturesQuery.data ? picturesQuery.data.data.filter((id, k) => selection[k]) : []
        });
        setSelection([]);
    }, [moveToPictureSetMutation, picturesQuery.data, selection, setSelection]);
    
    /// render ///
    
    let title: ReactNode;
    if (!sets) {
        title = 'All pictures';
    } else if (pictureSetQuery.isLoading) {
        title = <><Spinner type="grow" size="sm"/> Loading...</>;
    } else if (pictureSetQuery.isError) {
        title = 'Failed to load folder';
    } else {
        title = 'Pictures by folder';
    }
    
    return <div>
        <NavBar>
            <h3>
            <i className="bi bi-image"></i>
                &nbsp;&rsaquo;&nbsp;
                {title}
            </h3>
            <h3 className="ms-auto">{picturesQuery.data && <>{picturesQuery.data.total} picture(s)</>}</h3>
            {sets && pictureSetQuery.data && <button className="btn btn-primary ms-2"
                onClick={() => setAddPictureSetModalOpen(true)}
            ><i className="bi-plus-lg" /> Create folder...</button>}
            {numSelected > 0 && sets && <UncontrolledDropdown className="ms-2">
                <DropdownToggle caret>{numSelected} selected</DropdownToggle>
                <DropdownMenu>
                    <DropdownItem onClick={() => setMovePictureToPictureSetModalOpen(true)}>Move to folder...</DropdownItem>
                </DropdownMenu>
            </UncontrolledDropdown>}
            <ViewModeSwitcher className="ms-2" value={viewMode} setValue={setViewMode} />
        </NavBar>
        <Container onClick={() => setSelection([])}>
            {picturesQuery.isError && <Alert color="danger">Loading pictures: {errorMessage(picturesQuery.error)}</Alert>}
            {createPictureSetMutation.isError && <Alert color="danger">Creating folder: {errorMessage(createPictureSetMutation.error)}</Alert>}
            {updatePictureMutation.isError && <Alert color="danger" className="alert-fixed">Updating picture: {errorMessage(updatePictureMutation.error)}</Alert>}
            {sets && pictureSetQuery.data && <PictureSetList pictureSet={pictureSetQuery.data} />}
            {!sets && <PictureFilters 
                objectTable={objectTable}
                objectId={objectId}
                objectName={objectName} 
                onSetObject={setObjectFilter}
            />}
            {picturesQuery.isLoading && !picturesQuery.isSuccess && <h3 className="text-center">
                <Spinner type="grow" /> Loading pictures...
            </h3>}
            {picturesQuery.isSuccess && <>
                {picturesQuery.data.data.length > 0 && <PicturesList
                    pictures={picturesQuery.data.data}
                    selection={selection}
                    viewMode={viewMode}
                    currentIndex={currentFullscreen}
                    onOpen={enterFullscreen}
                    onSetSelection={setSelection}
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
            {isAddPictureSetModalOpen && <CreateModal
                object={{
                    id: 0,
                    parentId: setId || null,
                    name: '',
                    isPrivate: false
                } as PictureSet}
                title="Create folder"
                onClose={() => setAddPictureSetModalOpen(false)}
                onSubmit={(pictureSet)  => createPictureSetMutation.mutate(pictureSet)}
            />}
            {isMovePictureToPictureSetModalOpen && <MoveToPictureSetModal
                onClose={() => setMovePictureToPictureSetModalOpen(false)}
                onSubmit={movePicturesToPictureSet}
            />}
        </Container>
    </div>
};

export default Pictures;