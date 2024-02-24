// This is a bit annoyingly complicated, as this component has to support multiple use cases:
// Views:
// - thumbnails
// - details
// - fullscreen
// Pictures provided as:
// - list of ids (for normal views, individual pictures retrieved via queries
//   although normally should be already loaded at this point)
// - list of actual pictures (for upload view, list exists only in memory)
import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL, PictureUploadResult } from '../model/Picture';
import { Spinner } from 'reactstrap';
import { usePictureQuery } from '../data/queries';
import { Fragment } from 'react';

interface PicturesListProps {
    viewMode: PicturesViewMode;
    pictures: Picture[] | number[];
    onRetryUpload?: (index: number) => void;
}

export enum PicturesViewMode {
    THUMBNAILS = 'thumbnails',
    DETAILS = 'details'
}

interface PictureProps {
    picture?: Picture;
    onRetryUpload: () => void;
    isError?: boolean;
    isLoading?: boolean;
}

interface PictureByIdProps {
    id: number;
    onRetryUpload: () => void;
}

interface PictureOverlayProps {
    id: number | null;
    upload: number | null;
    isError: boolean;
    isLoading: boolean;
    onRetryUpload: () => void;
}

const dummyImageURL =
    'data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgi' +
    'Pz4KPHN2ZyB3aWR0aD0iMzAwIiBoZWlnaHQ9IjIwMCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaH' +
    'R0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPgogPHJlY3Qgd2lkdGg9IjMwMCIgaGVpZ2h0PSIy' +
    'MDAiIGZpbGw9IiNBMEEwQTAiLz4KPC9zdmc+';

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
                    <button type="button" className="btn" onClick={onRetryUpload}>
                        <i className="bi bi-exclamation-triangle"/>
                    </button>}
                {(isLoading || upload === PictureUploadResult.UPLOADED) && <Spinner type="grow"/>}
            </div>
        );
    }
    return null;
};

/**
 * Shows a single picture in thumbnail mode.  Picture might be already stored on server (have id defined),
 * prepared for upload (id not defined), or not be loaded at all yet (picture not defined).
 * 
 * @param picture  Picture, if loaded
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 */
const PictureThumbnail = ({ picture, onRetryUpload, isError, isLoading }: PictureProps) => {
    return (
        <div className="me-2 mb-2 position-relative">
            <img height={PICTURE_SIZE_THUMBNAIL} src={picture?.thumbnailUrl || dummyImageURL} alt="" />
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

/**
 * Shows a single picture by id, getting it with a query.
 * 
 * @param id
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 */
const PictureThumbnailById = ({ id, onRetryUpload }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureThumbnail
        picture={pictureQuery.data}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        onRetryUpload={onRetryUpload}
    />;
}

const PicturesList = ({ viewMode, pictures, onRetryUpload }: PicturesListProps) => {
    return <div className="d-flex flex-wrap">
        {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
            {typeof p === 'number'
                ? <PictureThumbnailById id={p} onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null} />
                : <PictureThumbnail picture={p} onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null} />
            }
        </Fragment>)}
    </div>;
}

export default PicturesList;
