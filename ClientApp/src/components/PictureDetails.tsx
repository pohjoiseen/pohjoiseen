import * as React from 'react';
import Picture, { PICTURE_SIZE_DETAILS } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';
import EditableInline from './EditableInline';
import { useUpdatePictureMutation } from '../data/mutations';
import EditableTextarea from './EditableTextarea';

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
    const updatePictureMutation = useUpdatePictureMutation();
    
    return (
        <div className="d-flex flex-row mb-2">
            <div className="position-relative me-2">
                <img
                    width={PICTURE_SIZE_DETAILS}
                    height={picture ? Math.round(picture.height / (picture.width / PICTURE_SIZE_DETAILS)) : undefined}
                    src={picture?.detailsUrl || dummyImageURL}
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
            {picture && <div>
                <EditableInline
                    value={picture.title}
                    placeholder={picture.filename}
                    onChange={(value) => updatePictureMutation.mutate({ ...picture, title: value })}
                    viewTag="h5"
                    inputClassName="fs-5 p-0 lh-1"
                />
                <h5 title={'Uploaded: ' + (picture.uploadedAt ? picture.uploadedAt.toLocaleDateString() : 'not yet')}>{picture.photographedAt.toLocaleDateString()}</h5>
                <EditableTextarea
                    value={picture.description}
                    onChange={(value) => updatePictureMutation.mutate({ ...picture, description: value })}
                    emptyValueString="No description yet."
                />
                <p className="small text-muted">
                    <a target="_blank" href={picture.url}>{picture.filename}</a>
                    &nbsp;&nbsp;&nbsp;
                    {picture.width}x{picture.height}
                    &nbsp;&nbsp;&nbsp;
                    {Math.round(picture.size / 1024)} kB
                    {(picture.camera || picture.lens) && <> 
                        <br/>
                        {picture.camera || ''} {picture.lens || ''}
                    </>}
                </p>
            </div>}
        </div>
    );
};

export default PictureDetails;
