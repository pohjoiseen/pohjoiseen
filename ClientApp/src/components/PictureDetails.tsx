import * as React from 'react';
import Picture, { PICTURE_SIZE_DETAILS } from '../model/Picture';
import { dummyImageURL } from './pictureViewCommon';
import PictureOverlay from './PictureOverlay';
import EditableInline from './EditableInline';
import { useCreatePlaceMutation, useUpdatePictureMutation } from '../data/mutations';
import EditableTextarea from './EditableTextarea';
import { EditableSearchingAutocomplete } from './SearchingAutocomplete';
import { useCallback, useState } from 'react';
import CreatePlaceModal from './CreatePlaceModal';
import { PlaceCategory } from '../model/PlaceCategory';
import ExploreStatus from '../model/ExploreStatus';
import Place from '../model/Place';
import Rating from './Rating';
import { FormGroup, Label } from 'reactstrap';
import TagSelector from './TagSelector';

interface PictureDetailsProps {
    picture?: Picture;
    isSelected: boolean;
    onOpen: () => void;
    onRetryUpload: () => void;
    onEditPlace?: (placeId: number) => void;
    onClick: (isCtrl: boolean, isShift: boolean) => void;
    isError?: boolean;
    isLoading?: boolean;
    isNotYetUploaded?: boolean;
}

/**
 * Shows a single picture in details mode.  Picture might be already stored on server (have id defined),
 * prepared for upload (id not defined), or not be loaded at all yet (picture not defined).
 *
 * @param picture  Picture, if loaded
 * @param isSelected  Display selection frame
 * @param onOpen  Double click/Enter handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param onEditPlace  View/modify place button handler
 * @param onClick  Click/Selection handler for image
 * @param isError  Display error icon
 * @param isLoading  Display loading spinner
 * @param isNotYetUploaded  Show smaller set of read-only data
 */
const PictureDetails = ({ picture, isSelected, onOpen, onRetryUpload, onEditPlace, onClick, isError, isLoading, isNotYetUploaded }: PictureDetailsProps) => {
    const updatePictureMutation = useUpdatePictureMutation();
    const createPlaceMutation = useCreatePlaceMutation();
    const [showCreatePlaceModal, setCreatePlaceModal] = useState('');
    
    const createPlace = useCallback(async (name: string, areaId: number) => {
        setCreatePlaceModal('');
        if (!picture) {
            return;
        }
        const newPlace: Place = {
            id: 0,
            areaId,
            name,
            alias: '',
            category: PlaceCategory.Default,
            exploreStatus: ExploreStatus.None,
            notes: '',
            links: '',
            directions: '',
            season: '',
            publicTransport: '',
            order: 0,
            lat: 0,
            lng: 0,
            zoom: 0,
            isPrivate: false,
            rating: 0,
            updatedAt: new Date(),
            tags: []
        };
        const place = await createPlaceMutation.mutateAsync(newPlace);
        await updatePictureMutation.mutateAsync({ ...picture, placeId: place.id, placeName: place.name });
    }, [picture, setCreatePlaceModal, createPlaceMutation, updatePictureMutation]);
    
    return (
        <div className="d-flex flex-row mb-2">
            <div className="position-relative me-2 picture-details-wrapper">
                <img
                    width={PICTURE_SIZE_DETAILS}
                    height={picture ? Math.round(picture.height / (picture.width / PICTURE_SIZE_DETAILS)) : undefined}
                    src={picture?.detailsUrl || dummyImageURL}
                    alt=""
                    title={picture?.title || ''}
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
            {picture && (isNotYetUploaded
                ? <div className="flex-grow-1">
                    <h5 className="mb-4">
                        <i className="text-muted">{picture.filename}</i><br/>
                        {/* TODO: locale should probably not be hardcoded */}
                        <span title={picture.photographedAt.toLocaleString('fi')}>{picture.photographedAt.toLocaleDateString('fi')}</span><br/>
                    </h5>
                    <p className="small text-muted">
                        {picture.width}x{picture.height}
                        &nbsp;&nbsp;&nbsp;
                        {Math.round(picture.size / 1024)} kB
                        {(picture.camera || picture.lens) && <>
                            <br/>
                            {picture.camera || ''} {picture.lens || ''}
                        </>}
                    </p>
                </div>
                : <div className={`flex-grow-1 ${picture.isPrivate ? 'is-private' : ''}`}>
                    <EditableInline
                        value={picture.title}
                        placeholder={picture.filename}
                        onChange={(value) => updatePictureMutation.mutate({ ...picture, title: value })}
                        viewTag="h5"
                        inputClassName="fs-5 p-0 lh-1"
                    />
                    {/* TODO: locale should probably not be hardcoded */}
                    <h5 title={`Created: ${picture.photographedAt.toLocaleString('fi')}; uploaded: ${picture.uploadedAt ? picture.uploadedAt.toLocaleString('fi') : 'not yet'}`}>
                        {picture.photographedAt.toLocaleDateString('fi')}
                    </h5>
                    <div className="d-flex mb-2">
                        <Rating value={picture.rating} onChange={(value) => updatePictureMutation.mutate({ ...picture, rating: value })} />
                        <FormGroup check inline className="ms-4">
                            <input
                                type="checkbox"
                                className="form-check-input"
                                id={`picture-is-private-${picture.id}`}
                                checked={picture.isPrivate}
                                onChange={(e) => updatePictureMutation.mutate({ ...picture, isPrivate: e.target.checked })}
                            />
                            <Label htmlFor={`picture-is-private-${picture.id}`} check>Private</Label>
                        </FormGroup>
                    </div>
                    <EditableTextarea
                        value={picture.description}
                        onChange={(value) => updatePictureMutation.mutate({ ...picture, description: value })}
                        emptyValueString="No description yet."
                    />
                    <div className="d-flex align-items-center mt-2 mb-2">
                        <div className="me-1">Location:</div>
                        <div className="flex-grow-1 fw-medium">
                            <EditableSearchingAutocomplete
                                id={picture.placeId}
                                title={picture.placeName!}
                                placeholder="Not set"
                                table="Places"
                                addNewText="Add new place: "
                                onAddNew={(title) => setCreatePlaceModal(title)}
                                onSelect={(id, title) => updatePictureMutation.mutate({ ...picture, placeId: id, placeName: title })}
                            />
                        </div>
                    </div>
                    {picture.placeId && onEditPlace && <a
                        href={'/place/' + picture.placeId}
                        className="btn btn-secondary btn-sm"
                        onClick={(e) => {
                            onEditPlace(picture.placeId!);
                            e.preventDefault();
                        }}
                    >
                        View/Modify place
                    </a>}
                    <div className="d-flex align-items-center mt-2 mb-2">
                        <div className="me-2">Tags:</div>
                        <TagSelector tags={picture.tags} onChange={(tags) => updatePictureMutation.mutate({ ...picture, tags })} />
                    </div>
                    <p className="small text-muted mt-3 mb-0">
                        <a target="_blank" href={picture.url}>{picture.filename}</a>
                        &nbsp;&nbsp;&nbsp;
                        {picture.width}x{picture.height}
                        &nbsp;&nbsp;&nbsp;
                        {Math.round(picture.size / 1024)} kB
                        {(picture.camera || picture.lens) && <> 
                            <br/>
                            {picture.camera || ''} {picture.lens || ''}
                        </>}
                        <br/>Last updated: {picture.updatedAt.toLocaleString('fi')}
                    </p>
                </div>)}
            {showCreatePlaceModal && <CreatePlaceModal
                defaultName={showCreatePlaceModal} 
                onClose={() => setCreatePlaceModal('')} 
                onSubmit={createPlace}
            />}
        </div>
    );
};

export default PictureDetails;
