import * as React from 'react';
import { SEARCHABLE_TABLES, SEARCHABLE_TABLES_NAMES_SINGULAR } from '../api/search';
import { Card, CardBody, CardHeader, CardTitle } from 'reactstrap';
import { SearchingAutocomplete } from './SearchingAutocomplete';

interface PictureFiltersProps {
    objectTable: typeof SEARCHABLE_TABLES[number];
    objectId: number | null;
    objectName: string | null;
    onSetObject: (table: string, id: number | null, name: string | null) => void;
}

const PictureFilters = ({ objectTable, objectId, objectName, onSetObject }: PictureFiltersProps) => {
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
                        {(['Places', 'Areas']).map((t) => <label className="form-label me-3" key={t}>
                            <input
                                type="radio"
                                checked={objectTable === t}
                                onChange={() => onSetObject(t, null, null)}
                            />&nbsp;{SEARCHABLE_TABLES_NAMES_SINGULAR[t as typeof SEARCHABLE_TABLES[number]]}
                        </label>)}
                    </div>
                </CardBody>
            </Card>
        </div>
    </div>;

};

export default PictureFilters;