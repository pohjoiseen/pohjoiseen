import * as React from 'react';
import Picture, { PICTURE_SIZE_THUMBNAIL } from '../model/Picture';

interface PicturesListProps {
    viewMode: PicturesViewMode;
    pictures: Picture[];
}

export enum PicturesViewMode {
    THUMBNAILS = 'thumbnails',
    DETAILS = 'details'
}

const PicturesList = ({ viewMode, pictures }: PicturesListProps) => {
    return <div className="d-flex flex-wrap">
        {pictures.map((p, key) => <div key={p.id || 'idx' + key} className="me-2 mb-2">
            <img height={PICTURE_SIZE_THUMBNAIL} src={p.url} />
        </div>)}
    </div>;
}

export default PicturesList;
