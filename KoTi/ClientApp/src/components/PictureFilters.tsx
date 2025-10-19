import * as React from 'react';
import { SEARCHABLE_TABLES, SEARCHABLE_TABLES_NAMES_SINGULAR } from '../api/search';
import { Card, CardBody, CardHeader, CardTitle } from 'reactstrap';
import { SearchingAutocomplete } from './SearchingAutocomplete';
import Tag from '../model/Tag';
import TagSelector from './TagSelector';
import Rating from './Rating';

interface PictureFiltersProps {
    objectTable: typeof SEARCHABLE_TABLES[number];
    objectId: number | null;
    objectName: string | null;
    tags: Tag[];
    minRating: number;
    onSetObject: (table: string, id: number | null, name: string | null) => void;
    onSetTags: (tags: Tag[]) => void;
    onSetRating: (rating: number) => void;
}

const PictureFilters = ({ objectTable, objectId, objectName, tags, minRating, onSetObject, onSetTags, onSetRating }: PictureFiltersProps) => {
    return <div className="row mt-4 mb-4">
        <div className="col-8 offset-2">
            <Card>
                <CardHeader>
                    <CardTitle tag="h5" className="m-0">Filter by:</CardTitle>
                </CardHeader>
                <CardBody>
                    <h6>Location:</h6>
                    <SearchingAutocomplete
                        id={objectId}
                        title={objectName}
                        placeholder="Any"
                        table={objectTable}
                        onSelect={(id, name) => onSetObject(objectTable, id, name)}
                    />
                    <div>
                        {(['Countries', 'Regions', 'Places', 'Areas']).map((t) => <label className="form-label me-3" key={t}>
                            <input
                                type="radio"
                                checked={objectTable === t}
                                onChange={() => onSetObject(t, null, null)}
                            />&nbsp;{SEARCHABLE_TABLES_NAMES_SINGULAR[t as typeof SEARCHABLE_TABLES[number]]}
                        </label>)}
                    </div>
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