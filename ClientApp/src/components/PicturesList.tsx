import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL, PictureUploadResult } from '../model/Picture';
import { Spinner } from 'reactstrap';

interface PicturesListProps {
    viewMode: PicturesViewMode;
    pictures: Picture[];
    onRetryUpload?: (index: number) => void;
}

export enum PicturesViewMode {
    THUMBNAILS = 'thumbnails',
    DETAILS = 'details'
}

const PicturesList = ({ viewMode, pictures, onRetryUpload }: PicturesListProps) => {
    return <div className="d-flex flex-wrap">
        {pictures.map((p, key) => <div key={p.id || 'idx' + key} className="me-2 mb-2 position-relative">
            <img height={PICTURE_SIZE_THUMBNAIL} src={p.url} />
            {(!p.id || p.upload === PictureUploadResult.DUPLICATE) && <div className="picture-upload-overlay">
                {typeof p.upload === 'undefined' && <i className="bi bi-three-dots" />}
                {typeof p.upload === 'number' && p.upload > 0 && Math.round(p.upload) + '%'}
                {p.upload === PictureUploadResult.DUPLICATE && 'DUP'}
                {p.upload === PictureUploadResult.FAILED && <button type="button" className="btn" onClick={() => onRetryUpload ? onRetryUpload(key) : null}>
                    <i className="bi bi-exclamation-triangle" />
                </button>}
                {p.upload === PictureUploadResult.UPLOADED && <Spinner type="grow" />}
            </div>}
        </div>)}
    </div>;
}

export default PicturesList;
