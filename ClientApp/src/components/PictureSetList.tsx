import * as React from 'react';
import PictureSet from '../model/PictureSet';
import { Alert, Card, CardBody, CardTitle } from 'reactstrap';
import { Link, useNavigate } from 'react-router-dom';
import { confirmModal } from './ModalContainer';
import { useDeletePictureSetMutation, useUpdatePictureSetMutation } from '../data/mutations';
import { errorMessage } from '../util';
import { useState } from 'react';
import UpdatePictureSetModal from './UpdatePictureSetModal';

interface PictureSetListProps {
    pictureSet: PictureSet;
}

const PictureSetList = ({ pictureSet }: PictureSetListProps) => {
    const updatePictureSetMutation = useUpdatePictureSetMutation();
    const deletePictureSetMutation = useDeletePictureSetMutation();
    const navigate = useNavigate();
    const [isUpdateModalOpen, setUpdateModalOpen] = useState(false);
    
    const updatePictureSet = async (pictureSet: PictureSet) => {
        try {
            setUpdateModalOpen(false);
            await updatePictureSetMutation.mutateAsync(pictureSet);
        }
        catch (e)
        {
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
            <h4 className="flex-grow-1"><i className="bi bi-folder"/>&nbsp;{pictureSet.name}&nbsp;{pictureSet.isPrivate && <i className="bi bi-shield-lock" />}</h4>
            <button className="btn btn-secondary btn-sm"
                    onClick={() => setUpdateModalOpen(true)}
                    disabled={isSomethingPending}
            >Edit folder...</button>
            {!pictureSet.children?.length && <button className="btn btn-danger btn-sm ms-2"
                    onClick={deletePictureSet}
                    disabled={isSomethingPending}
            >Delete folder</button>}
        </div>}
        <div className="d-flex flex-wrap mb-2">
        {!!pictureSet.id && <div className="w-25 pb-1 pe-1">
                <Card>
                    <CardBody>
                        <CardTitle tag="h5" className="m-0"><i className="bi bi-arrow-90deg-up" /> <Link to={pictureSet.parentId ? `/pictures/folders?folderId=${pictureSet.parentId}` : '/pictures/folders'}>Up folder</Link></CardTitle>
                    </CardBody>
                </Card>
            </div>}
            {pictureSet.children && pictureSet.children.map(ps => <div className="w-25 pb-1 pe-1" key={ps.id}>
                <Card>
                    <CardBody>
                        <CardTitle tag="h5" className="m-0">
                            <i className="bi bi-folder"/>{' '}
                            <Link to={`/pictures/folders?folderId=${ps.id}`}>{ps.name}</Link>{' '}
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