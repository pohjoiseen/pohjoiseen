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
import Picture, { PICTURE_SIZE_THUMBNAIL } from '../model/Picture';
import { usePictureQuery } from '../data/queries';
import { dummyImageURL, PicturesViewMode } from './pictureViewCommon';
import PictureThumbnail from './PictureThumbnail';
import PictureDetails, { PictureDetailsCopyPaste, PictureDetailsCopyPasteContext } from './PictureDetails';
import PlaceModal from './PlaceModal';

interface PicturesListProps {
    viewMode: PicturesViewMode;
    noWrap?: boolean;  // currently only for thumbnails
    showMore?: boolean;  // currently only for thumbnails
    pictures: (Picture | number)[];
    currentIndex: number;
    selection: boolean[] | number;
    link?: string;
    onOpen: (index: number, ctrlKey: boolean) => void;
    onRetryUpload?: (index: number) => void;
    onSetSelection: (selection: boolean[]) => void;
}

interface PictureByIdProps {
    id: number;
    isSelected: boolean;
    link?: string;
    onOpen?: (ctrlKey: boolean) => void;
    onClick: (ctrlKey: boolean, shiftKey: boolean) => void;
    onCopy?: (details: PictureDetailsCopyPaste) => void;
    onRetryUpload: () => void;
    onEditPlace?: (placeId: number) => void;
}

const handleClick = (index: number, lastIndex: number, ctrlKey: boolean, shiftKey: boolean, selection: number | boolean[], onSetSelection: (selection: boolean[]) => void) => {
    if (typeof selection === 'number') {
        const newSelection: boolean[] = [];
        newSelection[index] = true;
        onSetSelection(newSelection);
        return;
    }
    
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
 * @param link
 * @param onOpen  Double click/Enter handler for image
 * @param onClick  Click/Space handler for image
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 */
const PictureThumbnailById = ({ id, isSelected, link, onOpen, onClick, onRetryUpload }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureThumbnail
        picture={pictureQuery.data}
        isSelected={isSelected}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        link={link}
        onOpen={onOpen!}
        onClick={onClick}
        onRetryUpload={onRetryUpload}
    />;
}

const PicturesListThumbnails = ({ pictures, noWrap, showMore, link, onOpen, onRetryUpload, selection, onSetSelection }: PicturesListProps) => {
    const lastIndex = useRef(0);

    return <div className={noWrap ? "d-flex overflow-x-auto" : "d-flex flex-wrap"}>
        {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
            {typeof p === 'number'
                ? <PictureThumbnailById
                    id={p}
                    isSelected={typeof selection === 'number' ? selection === key : selection[key]}
                    link={link}
                    onOpen={(ctrlKey) => onOpen(key, ctrlKey)}
                    onClick={(ctrlKey, shiftKey) => {
                        handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                        lastIndex.current = key;
                    }}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                />
                : <PictureThumbnail
                    picture={p}
                    isSelected={typeof selection === 'number' ? selection === key : selection[key]}
                    link={link}
                    onOpen={(ctrlKey) => onOpen(key, ctrlKey)}
                    onClick={(ctrlKey, shiftKey) => {
                        handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                        lastIndex.current = key;
                    }}
                    onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                />
            }
        </Fragment>)}
        {showMore && <div className="me-2 mb-2 position-relative picture-thumbnail" onClick={() => onOpen(-1, false)}>
            <img src={dummyImageURL} height={PICTURE_SIZE_THUMBNAIL} width={PICTURE_SIZE_THUMBNAIL * 3 / 2} alt="" />
            <div className="picture-upload-overlay shaded">
                <i className="bi bi-three-dots" />
            </div>
        </div>}
    </div>;
};

/**
 * Shows a single picture by id, getting it with a query.
 *
 * @param id
 * @param isSelected
 * @param onOpen  Double click/Enter handler for image
 * @param onClick  Click/Space handler for image
 * @param onCopy  Copy common properties
 * @param onRetryUpload  Click handler for error icon (not used otherwise)
 * @param onEditPlace  View/Modify place button handler
 */
const PictureDetailsById = ({ id, isSelected, onOpen, onClick, onCopy, onRetryUpload, onEditPlace }: PictureByIdProps) => {
    const pictureQuery = usePictureQuery(id);
    return <PictureDetails
        picture={pictureQuery.data}
        isSelected={isSelected}
        isError={pictureQuery.isError}
        isLoading={pictureQuery.isLoading && !pictureQuery.isSuccess}
        onOpen={onOpen!}
        onClick={onClick}
        onCopy={onCopy!}
        onRetryUpload={onRetryUpload}
        onEditPlace={onEditPlace!}
    />;
}

const PicturesListDetails = ({ pictures, onOpen, onRetryUpload, selection, onSetSelection }: PicturesListProps) => {
    const [editedPlaceId, setEditedPlaceId] = useState(0);
    const lastIndex = useRef(0);
    
    const copyPasteContext = useRef<PictureDetailsCopyPaste>({
        title: null,
        description: null,
        placeId: null,
        placeName: null,
        tags: null
    });
    
    return <div>
        <PictureDetailsCopyPasteContext.Provider value={copyPasteContext.current}>
            {pictures.map((p, key) => <Fragment key={typeof p === 'number' ? p : (p.id || 'idx' + key)}>
                {typeof p === 'number'
                    ? <PictureDetailsById
                        id={p}
                        isSelected={typeof selection === 'number' ? selection === key : selection[key]}
                        onOpen={(ctrlKey) => onOpen(key, ctrlKey)}
                        onClick={(ctrlKey, shiftKey) => {
                            handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                            lastIndex.current = key;
                        }}
                        onCopy={context => copyPasteContext.current = context}
                        onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                        onEditPlace={(placeId) => setEditedPlaceId(placeId)}
                    />
                    : <PictureDetails
                        isNotYetUploaded
                        isSelected={typeof selection === 'number' ? selection === key : selection[key]}
                        picture={p}
                        onOpen={(ctrlKey) => onOpen(key, ctrlKey)}
                        onClick={(ctrlKey, shiftKey) => {
                            handleClick(key, lastIndex.current, ctrlKey, shiftKey, selection, onSetSelection);
                            lastIndex.current = key;
                        }}
                        onCopy={context => copyPasteContext.current = context}
                        onRetryUpload={() => onRetryUpload ? onRetryUpload(key) : null}
                    />
                }
            </Fragment>)}
            {!!editedPlaceId && <PlaceModal
                id={editedPlaceId}
                onSave={(/* placeName: string */) => {
                    // TODO: should update place name in pictures, but that would be quite tricky.
                    // Just keep showing old name if place name was updated
                    setEditedPlaceId(0);
                }}
                onClose={() => setEditedPlaceId(0)}
            />}
        </PictureDetailsCopyPasteContext.Provider>
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
