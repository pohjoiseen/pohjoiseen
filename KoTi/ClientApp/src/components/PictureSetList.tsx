import * as React from 'react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, Card, CardBody, CardTitle } from 'reactstrap';
import { useNavigate } from 'react-router-dom';
import PictureSet from '../model/PictureSet';
import { confirmModal } from './ModalContainer';
import { useDeletePictureSetMutation, useUpdatePictureSetMutation } from '../data/mutations';
import { errorMessage } from '../util';
import UpdatePictureSetModal from './UpdatePictureSetModal';

interface PictureSetListProps {
    pictureSet: PictureSet;
    onSelect?: (pictureSetId: number) => void;
    disableEdit?: boolean;
    disableKeyboardNav?: boolean;
}

const PictureSetList = ({ pictureSet, onSelect, disableEdit, disableKeyboardNav }: PictureSetListProps) => {
    const updatePictureSetMutation = useUpdatePictureSetMutation();
    const deletePictureSetMutation = useDeletePictureSetMutation();
    const navigate = useNavigate();
    const [isUpdateModalOpen, setUpdateModalOpen] = useState(false);
    const [filter, setFilter] = useState('');
    const filterInputRef = useRef<HTMLInputElement>(null);
    
    const select = useCallback((setId: number | null) => {
        if (onSelect) {
            onSelect(setId || 0);
        } else {
            navigate(setId ? `/pictures/folders?folderId=${setId}` : '/pictures/folders');
        }
    }, [navigate, onSelect])

    useEffect(() => {
        if (filterInputRef.current) {
            filterInputRef.current.focus();
        }

        // navigate up folder with Ctrl-Up
        if (!disableKeyboardNav) {
            const keyboardHandler = (e: KeyboardEvent) => {
                if (e.ctrlKey && e.key === 'ArrowUp') {
                    select(pictureSet.parentId);
                }
            };
            document.addEventListener('keydown', keyboardHandler);
            return () => document.removeEventListener('keydown', keyboardHandler);
        }
    }, [pictureSet, navigate, disableKeyboardNav, select]);
    
    const updatePictureSet = async (pictureSet: PictureSet) => {
        try {
            setUpdateModalOpen(false);
            await updatePictureSetMutation.mutateAsync(pictureSet);
        } catch (e) {
            // no need to catch, error flag will be set by mutation
        }
    };
    
    const deletePictureSet = async () => {
        if (await confirmModal('Really delete this folder? ' + 
            'This will not delete any pictures, but will move them to the parent folder.')) {
            try {
                await deletePictureSetMutation.mutateAsync(pictureSet);
                navigate(pictureSet.parentId ? `/pictures/folders?folderId=${pictureSet.parentId}` : '/pictures/folders');
            } catch (e) {}
        }
    };
    
    const isSomethingPending = updatePictureSetMutation.isLoading || deletePictureSetMutation.isLoading;
    
    return <>
        {updatePictureSetMutation.isError && <Alert color="danger">Updating folder: {errorMessage(updatePictureSetMutation.error)}</Alert>}
        {deletePictureSetMutation.isError && <Alert color="danger">Deleting folder: {errorMessage(deletePictureSetMutation.error)}</Alert>}
        {!!pictureSet.id && <div className="d-flex align-items-center mb-2">
            <h4 className="me-2">
                <a 
                    className="cursor-pointer" 
                    href={pictureSet.parentId ? `/pictures/folders?folderId=${pictureSet.parentId}` : '/pictures/folders'}
                    onClick={(e) => {
                        e.preventDefault();
                        select(pictureSet.parentId);
                    }}><i className="bi bi-arrow-90deg-up"/></a></h4>
            <h4 className="flex-grow-1"><i
                className="bi bi-folder"/>&nbsp;{pictureSet.name}&nbsp;{pictureSet.isPrivate &&
                <i className="bi bi-shield-lock"/>}</h4>
            {!disableEdit && <>
                <button className="btn btn-secondary btn-sm"
                        onClick={() => setUpdateModalOpen(true)}
                        disabled={isSomethingPending}
                >Edit folder...
                </button>
                {!pictureSet.children?.length && <button className="btn btn-danger btn-sm ms-2"
                                                         onClick={deletePictureSet}
                                                         disabled={isSomethingPending}
                >Delete folder</button>}
            </>}
        </div>}
        {pictureSet.children && pictureSet.children.length > 0 && <div className="d-flex align-items-center mb-2">
            <div>Find:</div>
            <input
                className="form-control flex-grow-1 ms-1 me-1"    
                value={filter}
                onChange={e => setFilter(e.target.value)}
                ref={filterInputRef}
            />
        </div>}
        <div className="d-flex flex-wrap mb-2">
            {pictureSet.children && pictureSet.children
                .filter(ps => !filter.length || ps.name.toLocaleLowerCase().includes(filter.toLocaleLowerCase()))
                .map(ps => <div className="w-25 pb-1 pe-1" key={ps.id}>
                    <Card className={ps.isPrivate ? 'is-private' : ''}>
                        <CardBody>
                            <div className={`picture-set-thumbnails ${ps.thumbnailUrls.length ? 'mb-2' : ''}`}>
                                {ps.thumbnailUrls.map(url => <a
                                    className="cursor-pointer"
                                    href={`/pictures/folders?folderId=${ps.id}`}
                                    onClick={(e) => {
                                        e.preventDefault();
                                        select(ps.id);
                                    }} 
                                    key={url}>
                                    <img src={url} alt="" />
                                </a>)}
                            </div>
                            <CardTitle tag="h5" className="m-0 d-flex align-items-start">
                                <i className="bi bi-folder d-inline-block me-2"/>{' '}
                                <a 
                                    className="cursor-pointer"
                                    href={`/pictures/folders?folderId=${ps.id}`}
                                    onClick={(e) => {
                                        e.preventDefault();
                                        select(ps.id);
                                    }}
                                >{ps.name}</a>{' '}
                                {ps.isPrivate && <i className="bi bi-shield-lock" />}
                            </CardTitle>
                        </CardBody>
                    </Card>
                </div>)}
        </div>
        {isUpdateModalOpen && <UpdatePictureSetModal
            pictureSet={pictureSet}
            onClose={() => setUpdateModalOpen(false)}
            onSubmit={updatePictureSet}
        />}
    </>;
};

export default PictureSetList;