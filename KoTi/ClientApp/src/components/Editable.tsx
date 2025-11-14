import * as React from 'react';
import { createContext, forwardRef, ReactNode, useCallback, useContext, useImperativeHandle, useState } from 'react';
import { Button } from 'reactstrap';

interface EditableProps {
    className?: string;
    viewUI: ReactNode;
    editUI: ReactNode;
    onStateChange?: (state: boolean) => void;
}

interface EditableContextType {
    onStartEdit: () => void;
    onEndEdit: () => void;
}

export interface EditableHandle {
    startEditing: () => void;
}

export const EditableContext = createContext<EditableContextType>({
    onStartEdit: () => { throw new Error('editableContext provider not set') },
    onEndEdit: () => { throw new Error('editableContext provider not set') }
});

export const Editable = forwardRef<EditableHandle, EditableProps>(
    ({ className, viewUI, editUI, onStateChange }: EditableProps, ref) => {
        
    const [isEditing, setEditing] = useState(false);

    const doSetEditing = useCallback((state: boolean) => {
        setEditing(state);
        if (onStateChange) {
            onStateChange(state);
        }
    }, [onStateChange]);
    
    useImperativeHandle(ref, () => ({
        startEditing() {
            doSetEditing(true);
        },
    }), [doSetEditing]);
    
    return <div className={`editable ${className || ''}`}>
        <EditableContext.Provider value={{
            onStartEdit: () => doSetEditing(true),
            onEndEdit: () => doSetEditing(false)
        }}>
            {isEditing ? editUI : viewUI}
        </EditableContext.Provider>
    </div>;
});

export const Edit = ({ className }: { className?: string }) => {
    const { onStartEdit } = useContext(EditableContext);
    return <Button
        className={`editable-edit ${className || ''}`}
        size="sm"
        color="outline-secondary"
        onClick={onStartEdit}>
        Edit...
    </Button>;
};
