import * as React from 'react';
import { Card, CardBody, CardHeader, CardTitle } from 'reactstrap';
import Tag from '../model/Tag';
import TagSelector from './TagSelector';
import Rating from './Rating';

interface PictureFiltersProps {
    tags: Tag[];
    minRating: number;
    onSetObject: (table: string, id: number | null, name: string | null) => void;
    onSetTags: (tags: Tag[]) => void;
    onSetRating: (rating: number) => void;
}

const PictureFilters = ({ tags, minRating, onSetObject, onSetTags, onSetRating }: PictureFiltersProps) => {
    return <div className="row mt-4 mb-4">
        <div className="col-8 offset-2">
            <Card>
                <CardHeader>
                    <CardTitle tag="h5" className="m-0">Filter by:</CardTitle>
                </CardHeader>
                <CardBody>
                    <h6>Tags:</h6>
                    <TagSelector tags={tags} onChange={onSetTags} />
                    <div className="d-flex align-items-center mt-3">
                        <h6 className="me-2 mb-0">Minimum rating:</h6>
                        <Rating value={minRating} onChange={onSetRating} />
                    </div>
                </CardBody>
            </Card>
        </div>
    </div>;

};

export default PictureFilters;