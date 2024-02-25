import * as React from 'react';
import Picture, { PICTURE_SIZE_DETAILS } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';

interface PictureDetailsProps {
    picture?: Picture;
    onOpen: () => void;
    onRetryUpload: () => void;
    isError?: boolean;
    isLoading?: boolean;
}

/**
 * Shows a single picture in details mode.  Picture might be already stored on server (have id defined),
 * prepared for upload (id not defined), or not be loaded at all yet (picture not defined).
 *
 * @param picture  Picture, if loaded
 * @param onOpen  Double click/Enter handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 */
const PictureDetails = ({ picture, onOpen, onRetryUpload, isError, isLoading }: PictureDetailsProps) => {
    return (
        <div className="d-flex flex-row mb-2">
            <div className="position-relative me-2">
                <img
                    width={PICTURE_SIZE_DETAILS}
                    height={picture ? Math.round(picture.height / (picture.width / PICTURE_SIZE_DETAILS)) : undefined}
                    src={picture?.detailsUrl || dummyImageURL}
                    alt=""
                    title={picture?.title || ''}
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
            {picture && <div>
                <h5>{picture.filename}, {picture.photographedAt.toLocaleDateString()}</h5>
            </div>}
        </div>
    );
};

export default PictureDetails;
