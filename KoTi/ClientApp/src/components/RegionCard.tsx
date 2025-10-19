/**
 * Region card for country page.  Simple enough but also handles DnD.
 */
import * as React from 'react';
import { useRef } from 'react';
import { Link } from 'react-router-dom';
import { useDrag, useDrop } from 'react-dnd';
import { Identifier, XYCoord } from 'dnd-core';
import { Card, CardBody, CardTitle } from 'reactstrap';
import DnDTypes from '../model/DnDTypes';
import Region from '../model/Region';

interface RegionCardProps {
    region: Region;
    index: number;
    onReorder: (from: number, to: number) => void;
}

interface RegionDnDItem {
    id: number;
    index: number;
}

const RegionCard = ({ region, index, onReorder }: RegionCardProps) => {
    const ref = useRef<HTMLDivElement>(null);

    /// DnD shenanigans ///
    // DnD implementation basically copied from sample
    // https://codesandbox.io/p/sandbox/github/react-dnd/react-dnd/tree/gh-pages/examples_ts/04-sortable/simple

    const [{ handlerId }, drop] = useDrop<RegionDnDItem, void, { handlerId: Identifier | null }>({
        accept: DnDTypes.REGION,
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

            // Get horizontal middle
            const hoverMiddleX =
                (hoverBoundingRect.right - hoverBoundingRect.left) / 2;

            // Determine mouse position
            const clientOffset = monitor.getClientOffset();

            // Get pixels to the left
            const hoverClientX = (clientOffset as XYCoord).x - hoverBoundingRect.left;

            // Only perform the move when the mouse has crossed half of the items width
            // When dragging right, only move when the cursor is after 50%
            // When dragging left, only move when the cursor is before 50%

            // Dragging right
            if (dragIndex < hoverIndex && hoverClientX < hoverMiddleX) {
                return;
            }

            // Dragging left
            if (dragIndex > hoverIndex && hoverClientX > hoverMiddleX) {
                return;
            }

            // Time to actually perform the action
            onReorder(dragIndex, hoverIndex);

            // Note: we're mutating the monitor item here!
            // Generally it's better to avoid mutations,
            // but it's good here for the sake of performance
            // to avoid expensive index searches.
            item.index = hoverIndex;
        }
    });

    const [{ isDragging }, drag] = useDrag({
        type: DnDTypes.REGION,
        item: () => ({ id: region.id, index }),
        collect: (monitor) => ({
            isDragging: monitor.isDragging()
        })
    });

    drag(drop(ref));

    /// render ///

    return <div key={region.id} ref={ref} className={`w-25 pb-1 pe-1 ${isDragging ? 'is-dragging' : ''}`} data-handler-id={handlerId}>
        <Card>
            <CardBody>
                <CardTitle tag="h5"><Link to={`/country/${region.countryId}/region/${region.id}`}>{region.name}</Link></CardTitle>
            </CardBody>
        </Card>
    </div>;
}

export default RegionCard;