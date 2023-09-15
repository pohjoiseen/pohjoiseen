import * as React from 'react';
import { RegisterOptions, useForm } from 'react-hook-form';
import { Edit, Editable, EditableContext } from './Editable';
import { useContext, useEffect, useRef } from 'react';
import { Button } from 'reactstrap';

interface EditableInlineProps {
    value: string;
    onChange: (value: string) => void;
    onStateChange?: (state: boolean) => void;
    initialState?: boolean;
    viewTag?: keyof JSX.IntrinsicElements;
    viewClassName?: string;
    inputClassName?: string;
    validation?: RegisterOptions;
}

const EditableInline = ({ value, onChange, onStateChange, initialState,
                          viewTag, viewClassName, inputClassName, validation }: EditableInlineProps) => {
    const ViewTag = viewTag || 'span';
    return <Editable
        className="d-flex align-items-start"
        viewUI={<>
            <ViewTag className={`me-2 ${viewClassName || ''}`}>{value}</ViewTag>
            <Edit className="align-self-center bigger" />
        </>}
        editUI={<EditableInlineForm
            value={value}
            onSubmit={onChange}
            inputClassName={inputClassName}
            validation={validation}
        />}
        onStateChange={onStateChange}
        initialState={initialState}
    />;
};

interface EditableInlineFormProps {
    value: string;
    onSubmit: (value: string) => void;
    inputClassName?: string;
    validation?: RegisterOptions;
}

const EditableInlineForm = ({ value, onSubmit, inputClassName, validation }: EditableInlineFormProps) => {
    const { register, handleSubmit,
        formState: { errors } } = useForm<{ value: string }>({ defaultValues: { value } });
    const editableContext = useContext(EditableContext);
    const inputRef = useRef<HTMLInputElement | null>();
    const { ref, ...rest } = register('value', validation);
    useEffect(() => {
        inputRef.current?.focus();
    }, [])
    
    const onValid = (values: { value: string }) => {
        editableContext.onEndEdit();
        onSubmit(values.value);
    }
    
    return <form onSubmit={handleSubmit(onValid)} className="d-flex">
        <input
            className={`form-control ${errors.value && 'is-invalid'} ${inputClassName || ''}`}
            ref={(e) => {
              ref(e);
              inputRef.current = e;
            }}
            onKeyUp={(e) => { if (e.key === 'Escape') editableContext.onEndEdit() }}
            {...rest}
        />
        <Button color="outline-secondary" size="sm" className="ms-1" onClick={handleSubmit(onValid)}><i className="bi-check-lg" /></Button>
        <Button color="outline-secondary" size="sm" className="ms-1" onClick={editableContext.onEndEdit}><i className="bi-x-lg" /></Button>
    </form>;
};

export default EditableInline;
