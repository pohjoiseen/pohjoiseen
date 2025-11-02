/**
 * <InsertUpload>: <InsertPane> sub-pane that allows pasting in freshly uploaded pictures.
 */
import * as React from 'react';
import { usePictureSetByNameQuery } from '../data/queries';
import { BLOG_PICTURES_SET } from '../model/PictureSet';
import { Alert, Spinner } from 'reactstrap';
import { errorMessage } from '../util';
import Upload from './Upload';
import { PicturesViewMode } from './pictureViewCommon';

interface InsertUploadProps {
    isActive: boolean;
    onSelect: (pictureId: number | null, insertImmediately?: boolean) => void;
}

const InsertUpload = ({ isActive, onSelect }: InsertUploadProps) => {
    const pictureSetQuery = usePictureSetByNameQuery(BLOG_PICTURES_SET);
    
    if (pictureSetQuery.isSuccess && !pictureSetQuery.data) {
        return <Alert color="danger">Blog picture quick upload features saves pictures to '{BLOG_PICTURES_SET}' folder,
            which is not found. Please create the folder and refresh the page.</Alert>;
    }
    if (pictureSetQuery.isLoading) {
        return <Spinner type="grow" />; 
    }
    
    return <div className={`overflow-y-auto overflow-x-hidden position-relative pt-2 ${!isActive ? 'd-none' : ''}`}>
        {pictureSetQuery.isError && <Alert color="danger">Loading picture set: {errorMessage(pictureSetQuery.error)}</Alert>}
        {pictureSetQuery.isSuccess && !pictureSetQuery.data && <Alert color="warning">
            Blog picture quick upload features saves pictures to '{BLOG_PICTURES_SET}' folder,
            which is not found. Please create the folder. Pictures will be saved to root folder instead.
        </Alert>}
        {pictureSetQuery.isSuccess && pictureSetQuery.data && <Alert color="success">
            Blog pictures uploaded through this panel will be placed into '{BLOG_PICTURES_SET}' folder.
        </Alert>}
        <Upload 
            viewMode={PicturesViewMode.THUMBNAILS}
            disableKeyboardNav={!isActive}
            setId={pictureSetQuery.data?.id}
            onSelect={onSelect}
        />
    </div>
};

export default InsertUpload;
