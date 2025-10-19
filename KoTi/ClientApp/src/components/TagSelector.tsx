import * as React from 'react';
import { useCallback, useRef, useState } from 'react';
import { AsyncTypeahead } from 'react-bootstrap-typeahead';
import Tag from '../model/Tag';
import { useTagsQuery } from '../data/queries';
import { useCreateTagMutation } from '../data/mutations';

interface TagSelectorProps {
    tags: Tag[];
    onChange: (tags: Tag[]) => void;
}

export const TagSelector = ({ tags, onChange }: TagSelectorProps) => {
    const [searchString, setSearchString] = useState('');
    const tagsQuery = useTagsQuery(searchString);
    const createTagMutation = useCreateTagMutation();
    const idRef = useRef('tag-selector-' + Math.random());
    
    const change = useCallback(async (selected: any /* Tags */) => {
        selected = await Promise.all(selected.map(async (tag: Tag | { customOption: true, name: string }) => {
            if ('customOption' in tag) {
                return await createTagMutation.mutateAsync({ id: 0, name: tag.name, isPrivate: false });
            } else {
                return tag;
            }
        }));
        onChange(selected);
    }, [onChange, createTagMutation]);
    
    return <AsyncTypeahead
        id={idRef.current}
        isLoading={tagsQuery.isFetching || createTagMutation.isLoading}
        onSearch={setSearchString}
        placeholder={createTagMutation.isLoading ? "Saving..." : "Select tags..."}
        options={tagsQuery.data || []}
        selected={tags}
        allowNew={true}
        newSelectionPrefix="Create tag: "
        labelKey="name"
        filterBy={() => true}
        multiple
        onChange={change}
    />;
};

export default TagSelector;