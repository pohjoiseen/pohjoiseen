/**
 * This is the "outside" view of a place as an accordion item, with header and content.  The content goes to PlaceView.
 */
import * as React from 'react';
import { useRef, useState } from 'react';
import { AccordionBody, AccordionItem } from 'reactstrap';
import { UseMutationResult } from '@tanstack/react-query';
import { Identifier, XYCoord } from 'dnd-core';
import { useDrag, useDrop } from 'react-dnd';
import { EditableHandle } from './Editable';
import ExploreStatusIndicator from './ExploreStatusIndicator';
import PlaceCategoryIndicator from './PlaceCategoryIndicator';
import EditableInline from './EditableInline';
import PlaceView from './PlaceView';
import { confirmModal } from './ModalContainer';
import Country from '../model/Country';
import Area from '../model/Area';
import Place from '../model/Place';
import DnDTypes from '../model/DnDTypes';
import Rating from './Rating';

interface PlaceComponentProps {
    country: Country;
    area: Area;
    place: Place;
    index: number;
    allowDnD: boolean;
    isOpen: boolean;
    onSetIsOpen: (isOpen: boolean) => void;
    onReorder: (from: number, to: number) => void;
    updatePlaceMutation: UseMutationResult<void, unknown, Place>;
    deletePlaceMutation: UseMutationResult<void, unknown, Place>;
}

interface PlaceDnDItem {
    id: number;
    index: number;
}

const PlaceComponent = ({ area, country, place, index, isOpen, allowDnD, onSetIsOpen, onReorder,
                          updatePlaceMutation, deletePlaceMutation }: PlaceComponentProps) => {
    const ref = useRef<HTMLElement>(null);
    const aliasRef = useRef<EditableHandle>(null);
    const [isAddingAlias, setAddingAlias] = useState(false);
    
    /// DnD shenanigans ///
    // DnD implementation basically copied from sample
    // https://codesandbox.io/p/sandbox/github/react-dnd/react-dnd/tree/gh-pages/examples_ts/04-sortable/simple
    
    const [{ handlerId }, drop] = useDrop<PlaceDnDItem, void, { handlerId: Identifier | null }>({
       accept: DnDTypes.PLACE,
       collect: (monitor) => ({ handlerId: monitor.getHandlerId() }),
       hover: (item, monitor) => {
           if (!ref.current) {
               return;
           }
           const dragIndex = item.index;
           const hoverIndex = index;
           if (dragIndex === hoverIndex) {
               return;
           }

           // Determine rectangle on screen
           const hoverBoundingRect = ref.current?.getBoundingClientRect();

           // Get vertical middle
           const hoverMiddleY =
               (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;

           // Determine mouse position
           const clientOffset = monitor.getClientOffset();

           // Get pixels to the top
           const hoverClientY = (clientOffset as XYCoord).y - hoverBoundingRect.top;

           // Only perform the move when the mouse has crossed half of the items height
           // When dragging downwards, only move when the cursor is below 50%
           // When dragging upwards, only move when the cursor is above 50%

           // Dragging downwards
           if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) {
               return;
           }

           // Dragging upwards
           if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) {
               return;
           }

           // Time to actually perform the action
           onReorder(dragIndex, hoverIndex);

           // Note: we're mutating the monitor item here!
           // Generally it's better to avoid mutations,
           // but it's good here for the sake of performance
           // to avoid expensive index searches.
           item.index = hoverIndex;
           
           // Scroll viewport if close to edge
           const scrollArea = 200;
           if (window.innerHeight > scrollArea * 3) {
               if (monitor.getClientOffset()!.y < scrollArea) {
                   window.setTimeout(() => window.scrollBy(0, -scrollArea), 0);
               } else if (monitor.getClientOffset()!.y > window.innerHeight - scrollArea) {
                   window.setTimeout(() => window.scrollBy(0, scrollArea), 0);
               }
           }
       } 
    });
    
    const [{ isDragging }, drag] = useDrag({
        type: DnDTypes.PLACE,
        item: () => ({ id: place.id, index, name: place.name }),
        collect: (monitor) => ({
            isDragging: monitor.isDragging()
        })
    });
    
    if (allowDnD) {
        drag(drop(ref));
    }
    
    /// render ///
    
    return <AccordionItem innerRef={ref} className={`${isDragging ? 'is-dragging' : ''}${place.isPrivate ? ' is-private': ''}`} data-handler-id={handlerId}>
        {/* do not use <AccordionHeader> because it does not allow us to intercept onClick */}
        <div className="accordion-header">
            <div
                tabIndex={0}
                className={`accordion-button place-header ${isOpen ? '' : 'collapsed'} ${place.alias ? 'with-alias' : ''}`}
                onClick={(e) => {
                    // click on accordion header should toggle accordion item, but not if it's on
                    // explore status or editable inline
                    const target = e.target as HTMLElement;
                    if (target && ((target.classList && target.classList.contains('accordion-button')) ||
                        (target.tagName && target.tagName === 'H5'))) {
                        onSetIsOpen(!isOpen);
                    }
                }}
            >
                <ExploreStatusIndicator
                    status={place.exploreStatus}
                    onChange={(status) => updatePlaceMutation.mutate({ ...place, exploreStatus: status })}
                />
                &nbsp;
                <PlaceCategoryIndicator
                    category={place.category}
                    onChange={(category) => updatePlaceMutation.mutate({ ...place, category })}
                />
                &nbsp;
                <div className="d-flex flex-column">
                    <EditableInline
                        value={place.name}
                        viewTag="h5"
                        viewClassName="m-0"
                        onChange={(value) => updatePlaceMutation.mutate({ ...place, name: value })}
                        validation={{ required: true }}
                    />
                    <EditableInline
                        value={place.alias}
                        viewClassName="fst-italic"
                        editableRef={aliasRef}
                        onStateChange={(state) => setAddingAlias(state)}
                        onChange={(value) => updatePlaceMutation.mutate({ ...place, alias: value })}
                    />
                </div>
                {place.isPrivate && <i className="bi bi-shield-lock" />}
                <Rating
                    className="flex-grow-1 justify-content-end me-3"    
                    value={place.rating}
                    onChange={(value) => updatePlaceMutation.mutate({ ...place, rating: value })} 
                />
            </div>
        </div>
        <AccordionBody accordionId={place.id.toString()}>
            <PlaceView
                place={place}
                isVisible={isOpen}
                area={area}
                country={country}
                isAddingAlias={isAddingAlias}
                onAddAlias={() => aliasRef.current!.startEditing()}
                onChange={(place) => updatePlaceMutation.mutate(place)}
                onDelete={async () => {
                    if (await confirmModal('Really delete this place?')) {
                        await deletePlaceMutation.mutateAsync(place);
                        onSetIsOpen(false);
                    }
                }}
            />
        </AccordionBody>
    </AccordionItem>;
};

export default PlaceComponent;