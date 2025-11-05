import * as React from 'react';
import { RegisterOptions, useForm } from 'react-hook-form';
import { Edit, Editable, EditableContext, EditableHandle } from './Editable';
import { Ref, useContext, useEffect, useRef } from 'react';

interface EditableInlineProps {
    value: string;
    placeholder?: string;
    onChange: (value: string) => void;
    onStateChange?: (state: boolean) => void;
    viewTag?: keyof JSX.IntrinsicElements;
    viewClassName?: string;
    inputClassName?: string;
    validation?: RegisterOptions;
    editableRef?: Ref<EditableHandle>;
}

const EditableInline = ({ value, placeholder, onChange, onStateChange, viewTag, viewClassName, inputClassName,
                            validation, editableRef }: EditableInlineProps) => {
    return <Editable
        className="d-flex align-items-start"
        viewUI={<EditableInlineView
            value={value}
            viewTag={viewTag}
            placeholder={placeholder}
            viewClassName={viewClassName}
        />}
        editUI={<EditableInlineForm
            value={value}
            onSubmit={onChange}
            inputClassName={inputClassName}
            validation={validation}
        />}
        onStateChange={onStateChange}
        ref={editableRef}
    />;
};

interface EditableInlineViewProps {
    value: string;
    viewTag?: keyof JSX.IntrinsicElements;
    placeholder?: string;
    viewClassName?: string;
}

const EditableInlineView = ({ value, viewTag, placeholder, viewClassName }: EditableInlineViewProps) => {
    const ViewTag = viewTag || 'span';
    const editableContext = useContext(EditableContext);
    return <>
        <ViewTag
            className={`me-2 ${viewClassName || ''}`}
            tabIndex={0}
            onFocus={() => editableContext.onStartEdit()}
        >
            {value ? value : <i className="text-muted">{placeholder}</i>}
        </ViewTag>
        <Edit className="align-self-center bigger" />
    </>;
};

interface EditableInlineFormProps {
    value: string;
    onSubmit: (value: string) => void;
    inputClassName?: string;
    validation?: RegisterOptions;
}

const EditableInlineForm = ({ value, onSubmit, inputClassName, validation }: EditableInlineFormProps) => {
    const { register, handleSubmit,
        formState: { errors } } = useForm<{ value: string }>({ defaultValues: { value }, mode: 'onChange' });
    const editableContext = useContext(EditableContext);
    const inputRef = useRef<HTMLInputElement | null>();
    const { ref, ...rest } = register('value', {
        ...validation,
        onBlur: () => {
            handleSubmit(onValid)();
        }
    });
    useEffect(() => {
        inputRef.current?.focus();
    }, [])
    
    const onValid = (values: { value: string }) => {
        editableContext.onEndEdit();
        if (values.value !== value) {
            onSubmit(values.value);
        }
    }
    
    return <form onSubmit={handleSubmit(onValid)}>
        <input
            size={value.length + 3}
            onInput={(e) => (e.target as HTMLInputElement).size = (e.target as HTMLInputElement).value.length + 3}
            className={`form-control ${errors.value && 'is-invalid'} ${inputClassName || ''}`}
            autoComplete="off"
            ref={(e) => {
              ref(e);
              inputRef.current = e;
            }}
            onKeyUp={(e) => { if (e.key === 'Escape') editableContext.onEndEdit() }}
            {...rest}
        />
    </form>;
};

export default EditableInline;
