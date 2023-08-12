import * as React from 'react';
import { useRef, useState } from 'react';
import { Popover, PopoverBody } from 'reactstrap';
import { PLACE_CATEGORY_DESCRIPTIONS, PlaceCategory } from '../model/PlaceCategory';

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

    const doChange = (category: PlaceCategory) => {
        setPopoverOpen(false);
        onChange(category);
    }
    
    return <>
        <button
            className="placecategory"
            title={PLACE_CATEGORY_DESCRIPTIONS[category]}
            style={{backgroundImage: `url(/map-icons/${category}.png)`}}
            id={idRef.current}
            onClick={() => setPopoverOpen(true)}
        />
        <Popover
            target={idRef.current}
            placement="bottom"
            isOpen={isPopoverOpen}
            toggle={() => setPopoverOpen(!isPopoverOpen)}
        >
            <PopoverBody>
                <div className="d-flex flex-wrap flex-row">
                    {isPopoverOpen && Object.entries(PLACE_CATEGORY_DESCRIPTIONS).map(([c, desc]) =>
                        <div key={c} className="placecategory-selector-item">
                            <button
                                className={`me-1 placecategory ${c === category ? 'placecategory-selected' : ''}`}
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