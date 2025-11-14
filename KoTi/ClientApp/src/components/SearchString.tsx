import * as React from 'react';
import { useEffect, useRef, useState } from 'react';
import { SEARCHABLE_TABLES, SEARCHABLE_TABLES_NAMES } from '../api/search';

interface SearchStringProps {
    initialValue?: string;
    initialTables?: string;
    onSearch: (value: string, tables: string) => void;
}

const SearchString = ({ initialValue, initialTables, onSearch }: SearchStringProps) => {
    const [value, setValue] = useState(initialValue || '');
    const [tables, setTables] = useState(initialTables ? initialTables.split(',') : []);
    const inputRef = useRef<HTMLInputElement | null>(null);

    useEffect(() => {
        inputRef.current?.focus();
    }, []);
    
    const doSearch = () => {
        if (value.length) {
            onSearch(value, tables.join(','));
        }
    };

    return <div className="row mt-4 mb-4">
        <div className="col-8 offset-2">
            <div className="d-flex flex-row">
                <input
                    className="form-control-lg flex-grow-1 me-2"
                    value={value}
                    onChange={(e) => setValue(e.target.value)}
                    onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                            doSearch();
                        }
                    }}
                    ref={inputRef}
                />
                <button
                    type="button"
                    className="btn btn-lg btn-primary"
                    onClick={doSearch}
                >Search
                </button>
            </div>
            <div>
                {SEARCHABLE_TABLES.map(t => <label className="form-label me-3" key={t}>
                    <input
                        type="checkbox"
                        checked={tables.includes(t)}
                        onChange={(e) => {
                            let newTables: string[];
                            if (e.target.checked) {
                                newTables = [...tables, t];
                            } else {
                                newTables = tables.filter(_t => _t !== t);
                            }
                            setTables(newTables);
                            if (value.length) {
                                onSearch(value, newTables.join(','));
                            }
                        }}
                    />&nbsp;{SEARCHABLE_TABLES_NAMES[t]}
                </label>)}
            </div>
        </div>
    </div>;
};

export default SearchString;