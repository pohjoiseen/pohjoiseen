import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';
import { useEffect } from 'react';

interface PictureFullscreenProps {
    picture?: Picture;
    onRetryUpload?: () => void;
    isError?: boolean;
    isLoading?: boolean;
}

/**
 * Shows a single picture in fullscreen mode.  Picture might be already stored on server (have id defined),
 * prepared for upload (id not defined), or not be loaded at all yet (picture not defined).
 *
 * @param picture  Picture, if loaded
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 */
const PictureFullscreen = ({ picture, onRetryUpload, isError, isLoading }: PictureFullscreenProps) => {
    // while this element is mounted, disable scrolling on body while keeping visible scroll position
    // from: https://stackoverflow.com/a/45230674
    useEffect(() => {
        const scrollPosition = window.scrollY;
        document.getElementById('root')!.style.top = -scrollPosition + 'px';
        document.body.classList.add('overlay');
        return () => {
            document.body.classList.remove('overlay');
            window.scrollTo({ top: scrollPosition, left: 0, behavior: 'auto' });
            document.getElementById('root')!.style.top = 0 + 'px';
        };
    }, []);

    return (
        <div className="picture-fullscreen">
            <img
                src={picture?.url || dummyImageURL}
                alt=""
            />
            <PictureOverlay
                id={picture?.id || null}
                upload={picture?.upload || null}
                isError={!!isError}
                isLoading={!!isLoading}
                onRetryUpload={onRetryUpload}
            />
        </div>
    );
};

export default PictureFullscreen;
