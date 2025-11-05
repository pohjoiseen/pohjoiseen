/**
 * <GeoIconPicker>: almost the same as PlaceCategoryIndicator but for picking icon for a blog geo.
 * Duplication duplication...
 */
import * as React from 'react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Popover, PopoverBody } from 'reactstrap';

interface GeoIconPicker {
    icon: string;
    onChange: (icon: string) => void;
}

let counter = 0;

const ICONS = [
    'archaeology.m',
    'archaeology',
    'bigcity.m',
    'bigcity',
    'bridge.m',
    'bridge',
    'castle.m',
    'castle',
    'cemetary.m',
    'cemetary',
    'church.m',
    'church',
    'cityarea.m',
    'cityarea',
    'dam.m',
    'dam',
    'forest.m',
    'forest',
    'fortification.m',
    'fortification',
    'industry.m',
    'industry',
    'island.m',
    'island',
    'lighthouse.m',
    'lighthouse',
    'manor.m',
    'manor',
    'memorial.m',
    'memorial',
    'mine.m',
    'mine',
    'mountain.m',
    'mountain',
    'museum.m',
    'museum',
    'petroglyphs.m',
    'petroglyphs',
    'river.m',
    'river',
    'road.m',
    'road',
    'ship.m',
    'ship',
    'smallcity.m',
    'smallcity',
    'star.m',
    'star',
    'station.m',
    'station',
    'stop.m',
    'stop',
    'touristvillage.m',
    'touristvillage',
    'trail.m',
    'trail',
    'tramway.m',
    'tramway',
    'tree.m',
    'tree',
    'war.m',
    'war',
    'waterfall.m',
    'waterfall',
];

const GeoIconPicker = ({ icon, onChange }: GeoIconPicker) => {
    const idRef = useRef('');
    if (!idRef.current) {
        idRef.current = 'iconpicker-' + (++counter);
    }
    const [isPopoverOpen, setPopoverOpen] = useState(false);
    const [filter, setFilter] = useState('');
    const [selected, setSelected] = useState(icon);
    const filterRef = useRef<HTMLInputElement>(null);

    const openPopover = useCallback(() => {
        setFilter('');
        setSelected(icon);
        setPopoverOpen(true);
        requestAnimationFrame(() => filterRef.current?.focus());
    }, [setFilter, setSelected, setPopoverOpen]);

    const doChange = useCallback((icon: string) => {
        setPopoverOpen(false);
        onChange(icon);
    }, [setPopoverOpen, onChange]);

    const filteredIcons = useMemo(() => {
        let fi = ICONS;
        if (filter) {
            const f = filter.toLowerCase();
            fi = fi.filter((fi) => fi.startsWith(f));
        }
        return fi;
    }, [filter]);

    useEffect(() => {
        const keyboardHandler = (e: KeyboardEvent) => {
            if (!filteredIcons.length || e.target !== filterRef.current) {
                return;
            }
            let currentIndex = filteredIcons.findIndex((i) => i === selected);
            let newIndex = -1;
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                if (currentIndex === -1 || currentIndex >= filteredIcons.length - 1) {
                    newIndex = 0;
                } else {
                    newIndex = currentIndex + 1;
                }
            }
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                if (currentIndex === -1 || currentIndex === 0) {
                    newIndex = filteredIcons.length - 1;
                } else {
                    newIndex = currentIndex - 1;
                }
            }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (currentIndex === -1 || currentIndex >= filteredIcons.length - 5) {
                    newIndex = currentIndex % 4;
                } else {
                    newIndex = currentIndex + 4;
                }
            }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                // okay not fully sure if this is not buggy
                if (currentIndex === -1 || currentIndex < 4) {
                    newIndex = filteredIcons.length - (filteredIcons.length % 4) + currentIndex;
                    if (newIndex >= filteredIcons.length) {
                        newIndex -= 4;
                    }
                } else {
                    newIndex = currentIndex - 4;
                }
            }
            if (e.key === 'Enter') {
                doChange(selected);
            }
            if (newIndex !== -1) {
                setSelected(filteredIcons[0]);
            }
        };
        document.addEventListener('keydown', keyboardHandler);
        return () => document.removeEventListener('keydown', keyboardHandler);
    }, [isPopoverOpen, selected, filteredIcons]);

    return <>
        <button
            className="placecategory"
            title={icon}
            style={{backgroundImage: `url(${process.env.REACT_APP_PREVIEW_HOST}/map-icons/${icon}.png)`}}
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
                    {isPopoverOpen && filteredIcons.map((i) =>
                        <div key={i} className="placecategory-selector-item">
                            <button
                                className={`me-1 placecategory ${i === selected ? 'placecategory-selected' : ''}`}
                                style={{backgroundImage: `url(${process.env.REACT_APP_PREVIEW_HOST}/map-icons/${i}.png)`}}
                                onClick={() => doChange(i)}
                            />
                            <i>{i}</i>
                        </div>)}
                </div>
            </PopoverBody>
        </Popover>
    </>;
};

export default GeoIconPicker;