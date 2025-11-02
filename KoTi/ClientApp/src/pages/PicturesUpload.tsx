/**
 * <PicturesUpload>: global picture upload page.  Wraps <Upload> component.
 */
import * as React from 'react';
import { useCallback, useState } from 'react';
import { Container } from 'reactstrap';
import NavBar from '../components/NavBar';
import { PicturesViewMode } from '../components/pictureViewCommon';
import ViewModeSwitcher from '../components/ViewModeSwitcher';
import { getDefaultViewMode, setDefaultViewMode } from '../data/localStorage';
import useTitle from '../hooks/useTitle';
import Upload from '../components/Upload';

const PicturesUpload = () => {
    const [viewMode, realSetViewMode] = useState(getDefaultViewMode);
    const setViewMode = useCallback((viewMode: PicturesViewMode) => {
        realSetViewMode(viewMode);
        setDefaultViewMode(viewMode);
    }, [realSetViewMode]);
    
    useTitle('Upload');
    
    /// render ///
    
    return <div className="d-flex flex-column mvh-100">
        <NavBar>
            <h3>
                <i className="bi bi-image"></i>
                &nbsp;&rsaquo;&nbsp;
                Upload
            </h3>
            <ViewModeSwitcher className="ms-2" value={viewMode} setValue={setViewMode} />
        </NavBar>
        <Container className="position-relative flex-grow-1 mh-100">
            <Upload viewMode={viewMode} />
        </Container>
    </div>
};

export default PicturesUpload;