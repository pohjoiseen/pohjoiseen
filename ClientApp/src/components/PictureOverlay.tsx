import * as React from 'react';
import { Spinner } from 'reactstrap';
import { PictureUploadResult } from '../model/Picture';

interface PictureOverlayProps {
    id: number | null;
    upload: number | null;
    isError: boolean;
    isLoading: boolean;
    onRetryUpload?: () => void;
}

/**
 * Shows a grey transparent overlay with loading percentage or other status icon.
 *
 * @param id  Picture id, if available at this point
 * @param upload  Upload state (percentage or PictureUploadResult)
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 */
const PictureOverlay = ({ id, upload, isError, isLoading, onRetryUpload }: PictureOverlayProps) => {
    if (!id || upload === PictureUploadResult.DUPLICATE || isError || isLoading) {
        return (
            <div className="picture-upload-overlay">
                {typeof upload === 'undefined' && <i className="bi bi-three-dots"/>}
                {typeof upload === 'number' && upload > 0 && Math.round(upload) + '%'}
                {upload === PictureUploadResult.DUPLICATE && 'DUP'}
                {(isError || upload === PictureUploadResult.FAILED) &&
                    <button type="button" className="btn" onClick={onRetryUpload ? onRetryUpload: () => null}>
                        <i className="bi bi-exclamation-triangle"/>
                    </button>}
                {(isLoading || upload === PictureUploadResult.UPLOADED) && <Spinner type="grow"/>}
            </div>
        );
    }
    return null;
};

export default PictureOverlay;
