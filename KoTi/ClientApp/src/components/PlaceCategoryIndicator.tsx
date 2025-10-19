import * as React from 'react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Popover, PopoverBody } from 'reactstrap';
import { PLACE_CATEGORY_DESCRIPTIONS, PLACE_CATEGORY_SEARCH_STRINGS, PlaceCategory } from '../model/PlaceCategory';

interface PlaceCategoryIndicatorProps {
    category: PlaceCategory;
    onChange: (status: PlaceCategory) => void;
}

let counter = 0;

const PlaceCategoryIndicator = ({ category, onChange }: PlaceCategoryIndicatorProps) => {
    const idRef = useRef('');
    if (!idRef.current) {
        idRef.current = 'placecategory-' + (++counter);
    }
    const [isPopoverOpen, setPopoverOpen] = useState(false);
    const [filter, setFilter] = useState('');
    const [selectedCategory, setSelectedCategory] = useState(category);
    const filterRef = useRef<HTMLInputElement>(null);

    const openPopover = useCallback(() => {
        setFilter('');
        setSelectedCategory(category);
        setPopoverOpen(true);
        requestAnimationFrame(() => filterRef.current?.focus());
    }, [setFilter, setSelectedCategory, setPopoverOpen]);
    
    const doChange = useCallback((category: PlaceCategory) => {
        setPopoverOpen(false);
        onChange(category);
    }, [setPopoverOpen, onChange]);
    
    const filteredCategories = useMemo(() => {
        let fc = Object.entries(PLACE_CATEGORY_DESCRIPTIONS);
        if (filter) {
            const f = filter.toLowerCase();
            fc = fc.filter(([c]) => {
                const searchStrings = PLACE_CATEGORY_SEARCH_STRINGS[c as keyof typeof PLACE_CATEGORY_DESCRIPTIONS];
                return searchStrings.find(s => s.startsWith(f));
            });
        }
        return fc;
    }, [filter]);

    useEffect(() => {
        const keyboardHandler = (e: KeyboardEvent) => {
            if (!filteredCategories.length || e.target !== filterRef.current) {
                return;
            }
            let currentIndex = filteredCategories.findIndex(([c]) => c === selectedCategory);
            let newIndex = -1;
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                if (currentIndex === -1 || currentIndex >= filteredCategories.length - 1) {
                    newIndex = 0;
                } else {
                    newIndex = currentIndex + 1;
                }
            }
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                if (currentIndex === -1 || currentIndex === 0) {
                    newIndex = filteredCategories.length - 1;
                } else {
                    newIndex = currentIndex - 1;
                }
            }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (currentIndex === -1 || currentIndex >= filteredCategories.length - 5) {
                    newIndex = currentIndex % 4;
                } else {
                    newIndex = currentIndex + 4;
                }
            }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                // okay not fully sure if this is not buggy
                if (currentIndex === -1 || currentIndex < 4) {
                    newIndex = filteredCategories.length - (filteredCategories.length % 4) + currentIndex;
                    if (newIndex >= filteredCategories.length) {
                        newIndex -= 4;
                    }
                } else {
                    newIndex = currentIndex - 4;
                }
            }
            if (e.key === 'Enter') {
                doChange(selectedCategory);
            }
            if (newIndex !== -1) {
                setSelectedCategory(filteredCategories[newIndex][0] as keyof typeof PLACE_CATEGORY_DESCRIPTIONS);
            }
        };
        document.addEventListener('keydown', keyboardHandler);
        return () => document.removeEventListener('keydown', keyboardHandler);
    }, [isPopoverOpen, selectedCategory, filteredCategories]);
    
    return <>
        <button
            className="placecategory"
            title={PLACE_CATEGORY_DESCRIPTIONS[category]}
            style={{backgroundImage: `url(/map-icons/${category}.png)`}}
            id={idRef.current}
            onClick={openPopover}
        />
        <Popover
            target={idRef.current}
            placement="bottom"
            isOpen={isPopoverOpen}
            toggle={() => setPopoverOpen(!isPopoverOpen)}
            className="placecategory-popover"
        >
            <PopoverBody>
                <div className="d-flex flex-wrap flex-row placecategory-content">
                    <input
                        className="form-control"
                        value={filter}
                        onChange={e => setFilter(e.target.value)}
                        ref={filterRef}
                    />
                    {isPopoverOpen && filteredCategories.map(([c, desc]) =>
                        <div key={c} className="placecategory-selector-item">
                            <button
                                className={`me-1 placecategory ${c === selectedCategory ? 'placecategory-selected' : ''}`}
                                title={desc}
                                style={{backgroundImage: `url(/map-icons/${c}.png)`}}
                                onClick={() => doChange(c as PlaceCategory)}
                            />
                            <i>{c}</i>
                        </div>)}
                </div>
            </PopoverBody>
        </Popover>
    </>;
};

export default PlaceCategoryIndicator;