import * as React from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { Edit, Editable, EditableContext, EditableHandle } from './Editable';
import { Ref, useContext } from 'react';
import { Button } from 'reactstrap';

interface EditableLinksListProps {
    value: string;
    onChange: (value: string) => void;
    onStateChange?: (state: boolean) => void;
    editableRef?: Ref<EditableHandle>;
}

const EditableLinksList = ({ value, onChange, onStateChange, editableRef }: EditableLinksListProps) => {
    return <Editable
        viewUI={value
            ? <>
                <p className="mb-0"><b>Links</b>: <Edit /></p>
                <ul>
                    {value.split("\n").map((link, i) => <li key={i}><a href={link}>{link}</a></li>)}
                </ul>
            </>
            : <p className="text-muted">No links. <Edit /></p>}
        editUI={<EditableLinksListForm
            value={value}
            onSubmit={onChange}
        />}
        onStateChange={onStateChange}
        ref={editableRef}
    />;
};

interface EditableLinksListFormProps {
    value: string;
    onSubmit: (value: string) => void;
}

interface EditableLinksListFormValues {
    links: { value: string }[]
}

const EditableLinksListForm = ({ value, onSubmit }: EditableLinksListFormProps) => {
    const defaultValues = { links: value.split("\n").map(l => ({ value: l })) }; 
    if (!defaultValues.links.length) {
        defaultValues.links.push({ value: '' });
    }
    const { register, handleSubmit, control} =
        useForm<EditableLinksListFormValues>({ defaultValues });
    const { fields, append, remove } =
        useFieldArray({ name: 'links', control });
    
    const editableContext = useContext(EditableContext);

    const onValid = (values: EditableLinksListFormValues) => {
        editableContext.onEndEdit();
        onSubmit(values.links.map(l => l.value).filter(l => l.trim().length > 0).join("\n"));
    }

    return <form onSubmit={handleSubmit(onValid)} className="w-100">
        <h6>Links:</h6>
        {fields.map((f, i) => <div className="mt-2 row" key={f.id}>
            <div className="col-8">
                <input
                    className="form-control"
                    autoComplete="off"
                    onKeyUp={(e) => { if (e.key === 'Escape') editableContext.onEndEdit() }}
                    {...register(`links.${i}.value` as const)}
                />
            </div>
            <div className="col-4">
                <Button color="secondary" onClick={() => remove(i)}>Remove</Button> 
            </div>
        </div>)}
        <div className="mt-2">
            <Button color="secondary" onClick={() => append({ value: '' })}>Add...</Button>
        </div>
        <div className="mt-2">
            <Button color="outline-secondary" size="sm" onClick={handleSubmit(onValid)}><i className="bi-check-lg" /></Button>
            <Button color="outline-secondary" size="sm" className="ms-2" onClick={editableContext.onEndEdit}><i className="bi-x-lg" /></Button>
        </div>
    </form>;
};

export default EditableLinksList;
