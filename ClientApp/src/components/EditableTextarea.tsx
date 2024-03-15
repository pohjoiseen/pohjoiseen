import * as React from 'react';
import { RegisterOptions, useForm } from 'react-hook-form';
import { Edit, Editable, EditableContext, EditableHandle } from './Editable';
import { Ref, useContext, useEffect, useRef } from 'react';
import { Button } from 'reactstrap';

interface EditableTextareaProps {
    value: string;
    onChange: (value: string) => void;
    onStateChange?: (state: boolean) => void;
    viewTag?: keyof JSX.IntrinsicElements;
    titleString?: string;
    emptyValueString?: string;
    inputClassName?: string;
    validation?: RegisterOptions;
    editableRef?: Ref<EditableHandle>;
}

const nl2br = (value: string): string => {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;')
        .replaceAll("\n", "<br>");
}

const EditableTextarea = ({ value, onChange, onStateChange, viewTag,
                            titleString, emptyValueString, inputClassName, validation, editableRef }: EditableTextareaProps) => {
    return <Editable
        className="d-flex align-items-start"
        viewUI={<EditableTextareaView
            value={value}
            viewTag={viewTag}
            emptyValueString={emptyValueString}
            titleString={titleString}
        />}
        editUI={<EditableTextareaForm
            value={value}
            onSubmit={onChange}
            inputClassName={inputClassName}
            titleString={titleString}
            validation={validation}
        />}
        onStateChange={onStateChange}
        ref={editableRef}
    />;
};

interface EditableTextareaViewProps {
    value: string;
    viewTag?: keyof JSX.IntrinsicElements;
    emptyValueString?: string;
    titleString?: string;
}

const EditableTextareaView = ({ value, viewTag, emptyValueString, titleString }: EditableTextareaViewProps) => {
    const ViewTag = viewTag || 'p';
    const editableContext = useContext(EditableContext);
    return <>
        {(value || emptyValueString) &&
            <ViewTag
                tabIndex={0}
                 onFocus={() => editableContext.onStartEdit()}
            >
                {titleString && <><b>{titleString}</b>:&nbsp;</>}
                {value ? <span dangerouslySetInnerHTML={{ __html: nl2br(value) }} /> : <span className="text-muted">{emptyValueString}</span>}
                {' '}
                <Edit className="align-self-center" />
            </ViewTag>}
    </>
}

interface EditableTextareaFormProps {
    value: string;
    onSubmit: (value: string) => void;
    inputClassName?: string;
    titleString?: string;
    validation?: RegisterOptions;
}

const EditableTextareaForm = ({ value, onSubmit, inputClassName, titleString, validation }: EditableTextareaFormProps) => {
    const { register, handleSubmit,
        formState: { errors } } = useForm<{ value: string }>({ defaultValues: { value }, mode: 'onChange' });
    const editableContext = useContext(EditableContext);
    const inputRef = useRef<HTMLTextAreaElement | null>();
    const { ref, ...rest } = register('value', {
        ...validation,
        onBlur: () => {
            handleSubmit(onValid)();
        }               
    });
    useEffect(() => {
        inputRef.current?.focus();
        autoSize();
    }, [])
    
    const autoSize = () => {
        inputRef.current!.style.height = '5px';
        inputRef.current!.style.height = inputRef.current!.scrollHeight + 15 + 'px';
    }

    const onValid = (values: { value: string }) => {
        editableContext.onEndEdit();
        onSubmit(values.value);
    }

    return <form onSubmit={handleSubmit(onValid)} className="w-100">
        {titleString && <><b>{titleString}</b>:</>}
        <textarea
            className={`editable-textarea form-control w-100 ${errors.value && 'is-invalid'} ${inputClassName || ''}`}
            ref={(e) => {
                ref(e);
                inputRef.current = e;
            }}
            onKeyUp={(e) => { if (e.key === 'Escape') editableContext.onEndEdit() }}
            onInput={autoSize}
            {...rest}
        />
    </form>;
};

export default EditableTextarea;
