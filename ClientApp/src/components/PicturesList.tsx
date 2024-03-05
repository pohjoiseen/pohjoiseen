// This is a bit annoyingly complicated, as this component has to support multiple use cases:
// Views:
// - thumbnails
// - details
// Pictures provided as:
// - list of ids (for normal views, individual pictures retrieved via queries
//   although normally should be already loaded at this point)
// - list of actual pictures (for upload view, list exists only in memory)
import * as React from 'react';
import { Fragment, useRef, useState } from 'react';
import Picture from '../model/Picture';
import { usePictureQuery } from '../data/queries';
import { PicturesViewMode } from './pictureViewCommon';
import PictureThumbnail from './PictureThumbnail';
import PictureDetails from './PictureDetails';
import { DomEvent } from 'leaflet';
import on = DomEvent.on;
import PlaceModal from './PlaceModal';
import { useUpdatePictureMutation } from '../data/mutations';

interface PicturesListProps {
    viewMode: PicturesViewMode;
    pictures: (Picture | number)[];
    currentIndex: number;
    selection: boolean[];
    onOpen: (index: number) => void;
    onRetryUpload?: (index: number) => void;
    onSetSelection: (selection: boolean[]) => void;
}

interface PictureByIdProps {
    id: number;
    isSelected: boolean;
    onOpen?: () => void;
    onClick: (ctrlKey: boolean, shiftKey: boolean) => void;
    onRetryUpload: () => void;
    onEditPlace?: (placeId: number) => void;
}

const handleClick = (index: number, lastIndex: number, ctrlKey: boolean, shiftKey: boolean, selection: boolean[], onSetSelection: (selection: boolean[]) => void) => {
    if (shiftKey) {
        const newSelection = [...selection];
        if (index < lastIndex) {
            const tmp = lastIndex;
            lastIndex = index;
            index = tmp;
        }
        for (let i = lastIndex; i <= index; i++) {
            newSelection[i] = ctrlKey ? !newSelection[i] : true;
        }
        onSetSelection(newSelection);
    } else if (ctrlKey) {
        const newSelection = [...selection];
        newSelection[index] = !newSelection[index];
        onSetSelection(newSelection);
    } else {
        const newSelection: boolean[] = [];
        newSelection[index] = true;
        onSetSelection(newSelection);
    }
};

/**
 * Shows a single picture by id, getting it with a query.
 * 
 * @param id
 * @param isSelected
 * @param onOpen  Double click/Enter handler for image
 * @param onClick  Click/Space handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 */
const PictureThumbnailById = ({ id, isSelected, onOpen, onClick, onRetryUpload }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureThumbnail
        picture={pictureQuery.data}
        isSelected={isSelected}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        onOpen={onOpen!}
        onClick={onClick}
        onRetryUpload={onRetryUpload}
    />;
}

const PicturesListThumbnails = ({ pictures, onOpen, onRetryUpload, selection, onSetSelection }: PicturesListProps) => {
    const lastIndex = useRef(0);

    return <div className="d-flex flex-wrap">
        {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
            {typeof p === 'number'
                ? <PictureThumbnailById
                    id={p}
                    isSelected={selection[key]}
                    onOpen={() => onOpen(key)}
                    onClick={(ctrlKey, shiftKey) => {
                        handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                        lastIndex.current = key;
                    }}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                />
                : <PictureThumbnail
                    picture={p}
                    isSelected={selection[key]}
                    onOpen={() => onOpen(key)}
                    onClick={(ctrlKey, shiftKey) => {
                        handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                        lastIndex.current = key;
                    }}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                />
            }
        </Fragment>)}
    </div>;
};

/**
 * Shows a single picture by id, getting it with a query.
 *
 * @param id
 * @param isSelected
 * @param onOpen  Double click/Enter handler for image
 * @param onClick  Click/Space handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param onEditPlace  View/Modify place button handler
 */
const PictureDetailsById = ({ id, isSelected, onOpen, onClick, onRetryUpload, onEditPlace }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureDetails
        picture={pictureQuery.data}
        isSelected={isSelected}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        onOpen={onOpen!}
        onClick={onClick}
        onRetryUpload={onRetryUpload}
        onEditPlace={onEditPlace!}
    />;
}

const PicturesListDetails = ({ pictures, onOpen, onRetryUpload, selection, onSetSelection }: PicturesListProps) => {
    const [editedPlaceId, setEditedPlaceId] = useState(0);
    const lastIndex = useRef(0);
    
    return <div>
        {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
            {typeof p === 'number'
                ? <PictureDetailsById
                    id={p}
                    isSelected={selection[key]}
                    onOpen={() => onOpen(key)}
                    onClick={(ctrlKey, shiftKey) => {
                        handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                        lastIndex.current = key;
                    }}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                    onEditPlace={(placeId) => setEditedPlaceId(placeId)}
                />
                : <PictureDetails
                    isNotYetUploaded
                    isSelected={selection[key]}
                    picture={p}
                    onOpen={() => onOpen(key)}
                    onClick={(ctrlKey, shiftKey) => {
                        handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                        lastIndex.current = key;
                    }}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                />
            }
        </Fragment>)}
        {!!editedPlaceId && <PlaceModal
            id={editedPlaceId}
            onSave={(placeName: string) => {
                // TODO: should update place name in pictures, but that would be quite tricky.
                // Just keep showing old name if place name was updated
                setEditedPlaceId(0);
            }}
            onClose={() => setEditedPlaceId(0)}
        />}
    </div>;
};

const PicturesList = (props: PicturesListProps) => {
    switch (props.viewMode) {
        case PicturesViewMode.THUMBNAILS:
            return <PicturesListThumbnails {...props} />;
            
        case PicturesViewMode.DETAILS:
            return <PicturesListDetails {...props} />;
            
        default:
            throw new Error('Unknown picture list view mode');
    }
}

export default PicturesList;
