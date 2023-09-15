import * as React from 'react';
import { createContext, ReactNode, useContext, useState } from 'react';
import { Button } from 'reactstrap';

interface EditableProps {
    className?: string;
    viewUI: ReactNode;
    editUI: ReactNode;
    initialState?: boolean;
    onStateChange?: (state: boolean) => void;
}

interface EditableContext {
    onStartEdit: () => void;
    onEndEdit: () => void;
}

export const EditableContext = createContext<EditableContext>({
    onStartEdit: () => { throw new Error('editableContext provider not set') },
    onEndEdit: () => { throw new Error('editableContext provider not set') }
});

export const Editable = ({ className, viewUI, editUI, initialState, onStateChange }: EditableProps) => {
    const [isEditing, setEditing] = useState(initialState || false);
    
    const doSetEditing = (state: boolean) => {
        setEditing(state);
        if (onStateChange) {
            onStateChange(state);
        }
    }
    
    return <div className={`editable ${className || ''}`}>
        <EditableContext.Provider value={{
            onStartEdit: () => doSetEditing(true),
            onEndEdit: () => doSetEditing(false)
        }}>
            {isEditing ? editUI : viewUI}
        </EditableContext.Provider>
    </div>;
};

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
