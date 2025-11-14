import * as React from 'react';
import { useCallback, KeyboardEvent, MouseEvent } from 'react';

interface RatingProps {
    className?: string;
    value: number;
    onChange: (value: number) => void;
}

const Rating = ({ className, value, onChange }: RatingProps) => {
    // premature optimization probably with the dataset stuff but whatever
    const click = useCallback((e: MouseEvent<HTMLSpanElement>) => {
        const target = e.currentTarget as HTMLElement;
        const newValue = parseInt(target.dataset['rating']!); 
        onChange(value === newValue ? 0 : newValue);
    }, [value, onChange]);
    
    const keyDown = useCallback((e: KeyboardEvent<HTMLDivElement>) => {
        if (e.key === 'ArrowLeft' && value > 0) {
            onChange(value - 1);
            e.preventDefault();
        } else if (e.key === 'ArrowRight' && value < 3) {
            onChange(value + 1);
            e.preventDefault();
        }
    }, [value, onChange]);
    
    return <div className={`d-flex ${className || ''}`} tabIndex={0} onKeyDown={keyDown}>
        {[1, 2, 3].map(k => <span onClick={click} data-rating={k} key={k}>
            <i className={`cursor-pointer bi bi-star${k <= value ? '-fill' : ''}`} />
        </span>)}
    </div>;
};

export default Rating;