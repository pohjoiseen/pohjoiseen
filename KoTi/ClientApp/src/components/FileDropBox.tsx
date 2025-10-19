import * as React from 'react';
import { useDrop } from 'react-dnd';
import { NativeTypes } from 'react-dnd-html5-backend';

interface FileDropBoxProps {
    onDrop: (files: FileList) => void
}

const FileDropBox = ({ onDrop }: FileDropBoxProps) => {
    const [{ isOver }, drop] = useDrop({
        accept: NativeTypes.FILE,
        collect: (monitor) => ({ isOver: monitor.canDrop() }),
        drop: (item: { files: FileList }) => { onDrop(item.files) },
    });
    
    return <div className={'filedropbox ' + (isOver ? 'active' : '')} ref={drop}>
        <h2>Drop file(s) here to upload</h2>
    </div>;
};

export default FileDropBox;