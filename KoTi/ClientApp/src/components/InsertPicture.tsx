/**
 * <InsertPicture>: UI to pick a picture to insert into a post etc.
 * This is a trimmed-down version of <Pictures> page.  Probably can be refactored...
 */
import * as React from 'react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Alert, Spinner } from 'reactstrap';
import { GetPicturesOptions } from '../api/pictures';
import { getPictureFromCache, usePictureQuery, usePictureSetQuery, usePicturesQuery } from '../data/queries';
import { errorMessage } from '../util';
import PictureSetList from './PictureSetList';
import Paginator from './Paginator';
import PictureFullscreen from './PictureFullscreen';
import PicturesList from './PicturesList';
import { PicturesViewMode } from './pictureViewCommon';

interface InsertPictureProps {
    isActive: boolean;
    onSelect: (pictureId: number | null, insertImmediately?: boolean) => void;
}

const PAGE_SIZE = 100;

const InsertPicture = ({ isActive, onSelect }: InsertPictureProps) => {
    const queryClient = useQueryClient();
    const [page, setPage] = useState(0);
    const [currentFullscreen, setCurrentFullscreen] = useState(-1);
    const [setId, setSetId] = useState(0);
    const [selectedIndex, setSelectedIndex] = useState(-1);
    const isFullscreen = currentFullscreen !== -1;
    const preloadRef = useRef<HTMLImageElement[]>([]);

    const offset = page * PAGE_SIZE;
    const pictureQueryOptions: GetPicturesOptions = {
        limit: PAGE_SIZE,
        offset,
        setId
    };
    const pictureSetQuery = usePictureSetQuery(setId);
    const picturesQuery = usePicturesQuery(pictureQueryOptions);
    
    const select = useCallback((index: number) => {
        setSelectedIndex(index);
        if (index === -1) {
            onSelect(null);
        } else {
            onSelect(picturesQuery.data?.data[index] || null);
        }
    }, [setSelectedIndex, onSelect, picturesQuery.data]);
        
    /// pagination ///
    
    const totalPictures = picturesQuery.data ? picturesQuery.data.total : 0;
    const totalPages = Math.ceil(totalPictures / PAGE_SIZE);
    const changePage = useCallback((page: number, fullscreen?: number) => {
        setPage(page);
        select(-1);
        if (!isFullscreen) {
            //window.scrollTo(0, 0);
        }
    }, [setPage, select, isFullscreen]);

    /// fullscreen mode ///
    
    const enterFullscreen = useCallback((index: number) => {
        setCurrentFullscreen(index);
        
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
    }, [setCurrentFullscreen, picturesQuery, queryClient]);
    
    const exitFullscreen = useCallback(() => {
        select(currentFullscreen);
        setCurrentFullscreen(-1);
    }, [select, currentFullscreen]);

    // query would be actually executed only when we have an id
    const fullscreenPictureId = picturesQuery.data && currentFullscreen >= 0 ? picturesQuery.data.data[currentFullscreen] : 0;
    const fullscreenPictureQuery = usePictureQuery(fullscreenPictureId,
        !(picturesQuery.data && currentFullscreen >= 0));

    // keyboard nav in fullscreen
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
                            changePage(page - 1, PAGE_SIZE - 1);
                        }
                    }
                    if (e.key === 'ArrowRight' && (e.target as any).tagName !== 'INPUT') {
                        if (currentFullscreen < picturesQuery.data.data.length - 1) {
                            enterFullscreen(currentFullscreen + 1);
                        } else if (page < totalPages - 1) {
                            changePage(page + 1, 0);
                        }
                    }
                }
            }
        };
        document.addEventListener('keydown', handler);
        return () => document.removeEventListener('keydown', handler);
    }, [page, totalPages, setPage, isFullscreen, exitFullscreen, picturesQuery, currentFullscreen, enterFullscreen, changePage]);

    const open = useCallback((index: number, ctrlKey: boolean) => {
        if (ctrlKey) {
            onSelect(picturesQuery.data?.data[index] || null, true);
        } else {
            enterFullscreen(index);
        }
    }, [picturesQuery.data, enterFullscreen, onSelect]);
    
    /// render ///

    return <div className={`overflow-y-auto overflow-x-hidden pt-2 ${!isActive ? 'd-none' : ''}`}>
        {picturesQuery.isError && <Alert color="danger">Loading pictures: {errorMessage(picturesQuery.error)}</Alert>}
        {pictureSetQuery.data && <PictureSetList
            pictureSet={pictureSetQuery.data}
            onSelect={(setId) => {
                setSetId(setId);
                setPage(0);
                select(-1);
            }}
            disableKeyboardNav={true}
            disableEdit={true}
        />}
        {picturesQuery.isLoading && !picturesQuery.isSuccess && <h3 className="text-center">
            <Spinner type="grow" /> Loading pictures...
        </h3>}
        {picturesQuery.isSuccess && <>
            {picturesQuery.data.data.length > 0 && <PicturesList
                pictures={picturesQuery.data.data}
                selection={selectedIndex}
                viewMode={PicturesViewMode.THUMBNAILS}
                currentIndex={currentFullscreen}
                onOpen={open}
                onSetSelection={(selection) => select(selection.findIndex(s => s))}
            />}
            {!picturesQuery.data.data.length && <h4 className="text-center">
                No pictures in the current view.
            </h4>}
            <Paginator page={page} setPage={changePage} totalPages={totalPages} disableKeyboardNav={true} />
            {isFullscreen && <PictureFullscreen
                picture={fullscreenPictureQuery.data!}
                isError={fullscreenPictureQuery.isError}
                isLoading={fullscreenPictureQuery.isLoading}
            />}
        </>}
    </div>

};

export default InsertPicture;