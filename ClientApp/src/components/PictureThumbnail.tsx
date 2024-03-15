import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';

interface PictureThumbnailProps {
    picture?: Picture;
    isSelected: boolean;
    onOpen: () => void;
    onRetryUpload: () => void;
    onClick: (isCtrl: boolean, isShift: boolean) => void;
    isError?: boolean;
    isLoading?: boolean;
}

/**
 * Shows a single picture in thumbnail mode.  Picture might be already stored on server (have id defined),
 * prepared for upload (id not defined), or not be loaded at all yet (picture not defined).
 *
 * @param picture  Picture, if loaded
 * @param isSelected  Display selection frame
 * @param onOpen  Double click/Enter handler for image
 * @param onClick  Click/Selection handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 */
const PictureThumbnail = ({ picture, isSelected, onOpen, onClick, onRetryUpload, isError, isLoading }: PictureThumbnailProps) => {
    let title: string = '';
    if (picture) {
        title = picture.photographedAt.toLocaleDateString('fi') + '\n' + (picture.title || picture.filename);
        if (picture.placeName) {
            title += '\nLocation: ' + picture.placeName;
        }
    }
    return (
        <div className="me-2 mb-2 position-relative picture-thumbnail" title={title}>
            <img
                height={PICTURE_SIZE_THUMBNAIL}
                width={picture ? Math.round(picture.width / (picture.height / PICTURE_SIZE_THUMBNAIL)) : undefined}
                src={picture?.thumbnailUrl || dummyImageURL} 
                alt="" 
                tabIndex={0}
                onClick={(e) => {
                    onClick(e.ctrlKey, e.shiftKey);
                    e.stopPropagation();
                }}
                onDoubleClick={onOpen}
                onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                        onOpen();
                    }
                    if (e.key === ' ') {
                        onClick(e.ctrlKey, e.shiftKey);
                        e.preventDefault();
                    }
                }}
            />
            <PictureOverlay
                id={picture?.id || null}
                isSelected={isSelected}
                upload={picture?.upload || null}
                isError={!!isError}
                isLoading={!!isLoading}
                onRetryUpload={onRetryUpload}
            />
        </div>
    );
};

export default PictureThumbnail;
