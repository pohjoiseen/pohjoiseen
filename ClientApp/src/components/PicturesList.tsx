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
    onOpen: (index: number) => void;
    onRetryUpload?: (index: number) => void;
}

interface PictureByIdProps {
    id: number;
    onOpen?: () => void;
    onRetryUpload: () => void;
    onEditPlace?: (placeId: number) => void;
}

/**
 * Shows a single picture by id, getting it with a query.
 * 
 * @param id
 * @param onOpen  Double click/Enter handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 */
const PictureThumbnailById = ({ id, onOpen, onRetryUpload }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureThumbnail
        picture={pictureQuery.data}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        onOpen={onOpen!}
        onRetryUpload={onRetryUpload}
    />;
}

const PicturesListThumbnails = ({ pictures, onOpen, onRetryUpload }: PicturesListProps) => {
    return <div className="d-flex flex-wrap">
        {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
            {typeof p === 'number'
                ? <PictureThumbnailById
                    id={p}
                    onOpen={() => onOpen(key)}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                />
                : <PictureThumbnail
                    picture={p}
                    onOpen={() => onOpen(key)}
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
 * @param onOpen  Double click/Enter handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param onEditPlace  View/Modify place button handler
 */
const PictureDetailsById = ({ id, onOpen, onRetryUpload, onEditPlace }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureDetails
        picture={pictureQuery.data}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        onOpen={onOpen!}
        onRetryUpload={onRetryUpload}
        onEditPlace={onEditPlace!}
    />;
}

const PicturesListDetails = ({ pictures, onOpen, onRetryUpload }: PicturesListProps) => {
    const [editedPlaceId, setEditedPlaceId] = useState(0);
    
    return <div>
        {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
            {typeof p === 'number'
                ? <PictureDetailsById
                    id={p}
                    onOpen={() => onOpen(key)}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                    onEditPlace={(placeId) => setEditedPlaceId(placeId)}
                />
                : <PictureDetails
                    isNotYetUploaded
                    picture={p}
                    onOpen={() => onOpen(key)}
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
