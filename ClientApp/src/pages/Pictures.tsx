import * as React from 'react';
import { useState, useRef } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import NavBar from '../components/NavBar';
import Picture from '../model/Picture';
import PicturesList, { PicturesViewMode } from '../components/PicturesList';
import { usePicturesAllQuery } from '../data/queries';
import { errorMessage } from '../util';

const Pictures = () => {
    const [viewMode, setViewMode] = useState(PicturesViewMode.THUMBNAILS);
    // pictures list
    const picturesQuery = usePicturesAllQuery();

    /// render ///

    return <div>
        <NavBar>
            <h3>
                <i className="bi bi-image"></i>
                &nbsp;&rsaquo;&nbsp;
                Pictures
                &nbsp;&rsaquo;&nbsp;
                All
            </h3>
        </NavBar>
        <Container>
            {picturesQuery.isError && <Alert color="danger">Loading pictures: {errorMessage(picturesQuery.error)}</Alert>}
            {picturesQuery.isLoading && !picturesQuery.isSuccess && <h3 className="text-center">
                <Spinner type="grow" /> Loading pictures...
            </h3>}
            {picturesQuery.isSuccess && <>
                {picturesQuery.data.length && <PicturesList
                    pictures={picturesQuery.data}
                    viewMode={viewMode}
                />}
                {!picturesQuery.data.length && <h4 className="text-center">
                    No pictures in the current view.
                </h4>}
            </>}
        </Container>
    </div>
};

export default Pictures;