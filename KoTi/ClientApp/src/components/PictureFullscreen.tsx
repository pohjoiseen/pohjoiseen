import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';
import { SyntheticEvent, useEffect, useRef } from 'react';

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
    
    // Support copying the image into clipboard using standard Ctrl-C.
    // For this to work we render every picture into a blob in an onload handler, and store the blob into a ref;
    // then on the global copy event we copy the blob with navigator.clipboard.write() API.
    // One problem here is that it takes noticeable time for image to be rendered to canvas
    // (~0.6 sec on latest Edge on my dev PC with 3000x2000 images).  This might be annoying and I don't know
    // a way around it unfortunately.  We set blobRef to null in the beginning of saveBlob(), and if the copy
    // event is triggered before the blob is ready, we just empty the clipboard by writing an empty string,
    // so that at least there wouldn't be any stale picture there
    const blobRef = useRef<Blob | null>(null);
    const saveBlob = (e: SyntheticEvent<HTMLImageElement, Event>) => {
        blobRef.current = null;
        const img = e.currentTarget;
        if (!picture) {
            return;  // shouldn't happen
        }
        const c = document.createElement('canvas');
        const ctx = c.getContext('2d')!;
        c.width = picture.width;
        c.height = picture.height;
        // note that crossOrigin="anonymous" on img is necessary for this operation
        // and that in turn requires CORS to be set up
        ctx.drawImage(img, 0, 0);
        c.toBlob(result => blobRef.current = result);
    };
    
    useEffect(() => {
        const listener = (e: ClipboardEvent) => {
            // as of March 2024 this API is unfortunately still supported only in the latest preview of Firefox
            // not an issue for me personally as I don't use Firefox but at least guard against a crash
            if ('ClipboardItem' in window) {
                e.preventDefault();
                if (blobRef.current) {
                    navigator.clipboard.write([
                        new ClipboardItem({
                            // note that it has to be an image/png; attempting to fetch() the original JPEG directly
                            // and copy it here with image/jpeg type fails ("...not supported on write" error) 
                            'image/png': blobRef.current
                        })
                    ]);
                } else {
                    // no image loaded, or image not yet rendered to canvas
                    navigator.clipboard.writeText('');
                }
            }
        };
        document.addEventListener('copy', listener);
        return () => document.removeEventListener('copy', listener);
    }, []);  // no need to have any dependencies

    return (
        <div className="picture-fullscreen">
            <img
                crossOrigin="anonymous"
                src={picture?.url || dummyImageURL}
                alt=""
                onLoad={saveBlob}
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
