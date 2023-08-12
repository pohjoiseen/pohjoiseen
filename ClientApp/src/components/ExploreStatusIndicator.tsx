import * as React from 'react';
import { useRef, useState } from 'react';
import ExploreStatus from '../model/ExploreStatus';
import { Popover, PopoverBody } from 'reactstrap';

interface ExploreStatusIndicatorProps {
    status: ExploreStatus;
    onChange: (status: ExploreStatus) => void;
}

let counter = 0;

const ExploreStatusIndicator = ({ status, onChange }: ExploreStatusIndicatorProps) => {
    const idRef = useRef('');
    if (!idRef.current) {
        idRef.current = 'explorestatus-' + (++counter);
    }
    const [isPopoverOpen, setPopoverOpen] = useState(false); 
    
    const doChange = (status: ExploreStatus) => {
        setPopoverOpen(false);
        onChange(status);
    }
    
    let color: string;
    switch (status) {
        case ExploreStatus.None:
            color = 'red';
            break;
        case ExploreStatus.Minimal:
            color = 'orange';
            break;
        case ExploreStatus.InProgress:
            color = 'yellow';
            break;
        case ExploreStatus.Sufficient:
            color = 'green';
            break;
        default:
            throw new Error('Invalid ExploreStatus value');
    }
    
    return <>
        <button type="button" className={`explorestatus explorestatus-${color}`} id={idRef.current} onClick={() => setPopoverOpen(true)} />
        <Popover
            target={idRef.current}
            placement="bottom"
            trigger="focus"
            isOpen={isPopoverOpen}
            toggle={() => setPopoverOpen(!isPopoverOpen)}
        >
            <PopoverBody>
                <button
                    className={`explorestatus explorestatus-red ${status === ExploreStatus.None ? 'explorestatus-selected' : ''}`}
                    onClick={() => doChange(ExploreStatus.None)}
                />
                &nbsp;
                Not visited
                <br />
                <button
                    className={`explorestatus explorestatus-orange ${status === ExploreStatus.Minimal ? 'explorestatus-selected' : ''}`}
                    onClick={() => doChange(ExploreStatus.Minimal)}
                />
                &nbsp;
                Visited but not properly explored
                <br />
                <button
                    className={`explorestatus explorestatus-yellow ${status === ExploreStatus.InProgress ? 'explorestatus-selected' : ''}`}
                    onClick={() => doChange(ExploreStatus.InProgress)}
                />
                &nbsp;
                Explored partially
                <br />
                <button
                    className={`explorestatus explorestatus-green ${status === ExploreStatus.Sufficient ? 'explorestatus-selected' : ''}`}
                    onClick={() => doChange(ExploreStatus.Sufficient)}
                />
                &nbsp;
                Explored satisfactorily
            </PopoverBody>
        </Popover>
    </>;
};

export default ExploreStatusIndicator;