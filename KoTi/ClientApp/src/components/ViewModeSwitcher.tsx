import * as React from 'react';
import { PicturesViewMode } from './pictureViewCommon';
import { Button, ButtonGroup } from 'reactstrap';

interface ViewModeSwitcherProps {
    value: PicturesViewMode;
    setValue: (value: PicturesViewMode) => void;
    className?: string;
}

const ViewModeSwitcher = ({ value, setValue, className }: ViewModeSwitcherProps) => {
    return <ButtonGroup className={className}>
        <Button 
            color="secondary"
            outline
            onClick={() => setValue(PicturesViewMode.THUMBNAILS)}
            active={value === PicturesViewMode.THUMBNAILS}
        >
            View
        </Button>
        <Button
            color="secondary"
            outline
            onClick={() => setValue(PicturesViewMode.DETAILS)}
            active={value === PicturesViewMode.DETAILS}
        >
            Edit
        </Button>
    </ButtonGroup>;
};

export default ViewModeSwitcher;