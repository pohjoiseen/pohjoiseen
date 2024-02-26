import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';

interface PictureThumbnailProps {
    picture?: Picture;
    onOpen: () => void;
    onRetryUpload: () => void;
    isError?: boolean;
    isLoading?: boolean;
}

/**
 * Shows a single picture in thumbnail mode.  Picture might be already stored on server (have id defined),
 * prepared for upload (id not defined), or not be loaded at all yet (picture not defined).
 *
 * @param picture  Picture, if loaded
 * @param onOpen  Double click/Enter handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 */
const PictureThumbnail = ({ picture, onOpen, onRetryUpload, isError, isLoading }: PictureThumbnailProps) => {
    return (
        <div className="me-2 mb-2 position-relative picture-thumbnail">
            <img
                height={PICTURE_SIZE_THUMBNAIL}
                width={picture ? Math.round(picture.width / (picture.height / PICTURE_SIZE_THUMBNAIL)) : undefined}
                src={picture?.thumbnailUrl || dummyImageURL} 
                alt="" 
                title={picture?.title || ''}
                tabIndex={0}
                onDoubleClick={onOpen}
                onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                        onOpen();
                    }
                }}
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

export default PictureThumbnail;
