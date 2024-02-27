import * as React from 'react';
import { ElementRef, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { AsyncTypeahead, Typeahead } from 'react-bootstrap-typeahead';
import { SEARCHABLE_TABLES, SearchResult } from '../api/search';
import { useSearchQuery } from '../data/queries';
import { Edit, Editable, EditableContext } from './Editable';

interface SearchingAutocompleteProps {
    id: number | null;
    title: string | null;
    placeholder: string;
    table: typeof SEARCHABLE_TABLES[number];
    onSelect: (id: number | null, title: string | null) => void;
}

export const SearchingAutocomplete = ({ id, title, placeholder, table, onSelect, inEditableContext }:
                                          SearchingAutocompleteProps & { inEditableContext?: boolean }) => {
    const [searchString, setSearchString] = useState('');
    const searchQuery = useSearchQuery({
        q: searchString ? searchString.trim() + '*' : '',
        tables: table
    });
    
    const renderMenuItemChildren = useCallback((option: any) => {
        const result = option as SearchResult;
        return <div>
            <h6>{result.title}</h6>
            <div className="small" dangerouslySetInnerHTML={{ __html: result.text }} />
        </div>;
    }, []);
    
    const shouldClose = useRef(false);
    const onChange = useCallback((selected: any) => {
        if (selected.length) {
            onSelect(selected[0].tableId, selected[0].title);
        } else {
            onSelect(null, null);
        }
        // onChange can fire continuously, somewhat awkwardly make sure we close the control only when Enter was pressed
        if (shouldClose.current && inEditableContext) {
            editableContext.onEndEdit();
        }
    }, [onSelect]);
    
    const ref = useRef<ElementRef<typeof Typeahead>>(null);
    const elemId = useRef('autocomplete' + Math.random())
    const editableContext = useContext(EditableContext);

    useEffect(() => {
        if (inEditableContext) {
            ref.current?.focus();
        }
    }, []);
    
    return <AsyncTypeahead
        id={elemId.current}
        isLoading={searchQuery.isFetching}
        onSearch={setSearchString}
        placeholder={placeholder}
        options={searchQuery.data?.data || []}
        defaultSelected={id ? [{ tableName: table, tableId: id, title: title }] : []}
        labelKey="title"
        filterBy={() => true}
        renderMenuItemChildren={renderMenuItemChildren}
        onKeyDown={(e) => {
            if (e.key === 'Escape' && inEditableContext) {
                editableContext.onEndEdit();
            }
            if (e.key === 'Enter' && inEditableContext) {
                shouldClose.current = true;
            }
        }}
        onBlur={() => {
            if (inEditableContext) {
                editableContext.onEndEdit();
            }
        }}
        onChange={onChange}
        ref={ref}
    />;
};

const EdiableSearchingView = ({ title, placeholder }: { title: string | null, placeholder?: string }) => {
    const editableContext = useContext(EditableContext);
    return <>
        <div
            className="me-1"
            tabIndex={0}
            onFocus={() => editableContext.onStartEdit()}
        >
            {title ? title : <i className="text-muted">{placeholder || 'Not set'}</i>}
        </div>
        <Edit />
    </>;
};

export const EditableSearchingAutocomplete = ({ id, title, placeholder, table, onSelect }: SearchingAutocompleteProps) => {
    return <Editable
        className="d-flex align-items-end editable-autocomplete"
        viewUI={<EdiableSearchingView title={id ? title : null} placeholder={placeholder} />}
        editUI={<SearchingAutocomplete
            id={id}
            title={title}
            placeholder={placeholder}
            table={table}
            onSelect={onSelect}
            inEditableContext
        />}
    />;
};
