import * as React from 'react';
import { RegisterOptions, useForm } from 'react-hook-form';
import { Edit, Editable, EditableContext } from './Editable';
import { useContext, useEffect, useRef } from 'react';
import { Button } from 'reactstrap';

interface EditableTextareaProps {
    value: string;
    onChange: (value: string) => void;
    onStateChange?: (state: boolean) => void;
    initialState?: boolean;
    viewTag?: keyof JSX.IntrinsicElements;
    titleString?: string;
    emptyValueString?: string;
    inputClassName?: string;
    validation?: RegisterOptions;
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

const EditableTextarea = ({ value, onChange, initialState, onStateChange, viewTag,
                            titleString, emptyValueString, inputClassName, validation }: EditableTextareaProps) => {
    const ViewTag = viewTag || 'p';
    return <Editable
        className="d-flex align-items-start"
        viewUI={<>
            {(value || emptyValueString) && 
                <ViewTag>
                    {titleString && <><b>{titleString}</b>:&nbsp;</>}
                    {value ? <span dangerouslySetInnerHTML={{ __html: nl2br(value) }} /> : <span className="text-muted">{emptyValueString}</span>}
                    {' '}
                    <Edit className="align-self-center" />
                </ViewTag>}
        </>}
        editUI={<EditableTextareaForm
            value={value}
            onSubmit={onChange}
            inputClassName={inputClassName}
            titleString={titleString}
            validation={validation}
        />}
        initialState={initialState}
        onStateChange={onStateChange}
    />;
};

interface EditableTextareaFormProps {
    value: string;
    onSubmit: (value: string) => void;
    inputClassName?: string;
    titleString?: string;
    validation?: RegisterOptions;
}

const EditableTextareaForm = ({ value, onSubmit, inputClassName, titleString, validation }: EditableTextareaFormProps) => {
    const { register, handleSubmit, watch,
        formState: { errors } } = useForm<{ value: string }>({ defaultValues: { value } });
    const editableContext = useContext(EditableContext);
    const inputRef = useRef<HTMLTextAreaElement | null>();
    const { ref, ...rest } = register('value', validation);
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
            className={`editable-textarea form-control overflow-hidden w-100 ${errors.value && 'is-invalid'} ${inputClassName || ''}`}
            ref={(e) => {
                ref(e);
                inputRef.current = e;
            }}
            onKeyUp={(e) => { if (e.key === 'Escape') editableContext.onEndEdit() }}
            onInput={autoSize}
            {...rest}
        />
        <div className="mt-2">
            <Button color="outline-secondary" size="sm" onClick={handleSubmit(onValid)}><i className="bi-check-lg" /></Button>
            <Button color="outline-secondary" size="sm" className="ms-2" onClick={editableContext.onEndEdit}><i className="bi-x-lg" /></Button>
        </div>
    </form>;
};

export default EditableTextarea;
