import * as React from 'react';
import { Button, Label, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import { useForm } from 'react-hook-form';
import { useEffect, useRef } from 'react';
import { SearchingAutocomplete } from './SearchingAutocomplete';

interface CreatePlaceModalResult {
    name: string;
    area: {
        id: number;
        name: string;
    } | null;
}

interface CreatePlaceModalProps {
    defaultName: string;
    onClose: () => void;
    onSubmit: (name: string, areaId: number) => void;
}
const CreatePlaceModal = ({ defaultName, onClose, onSubmit }: CreatePlaceModalProps) => {
    const { register, handleSubmit, getValues, setValue,
        formState: { errors } } = useForm<CreatePlaceModalResult>({ defaultValues: { name: defaultName, area: null } });
    const inputElemRef = useRef<HTMLInputElement | null>();
    const { ref: inputRef, ...inputRest } = register('name', { required: true });
    register('area', { required: true });
    useEffect(() => {
        setTimeout(() => inputElemRef.current?.focus(), 0);
    }, [])

    const submit = handleSubmit((object) => {
        const result = object as unknown as CreatePlaceModalResult;
        onClose();
        onSubmit(result.name, result.area!.id);        
    });

    return (
        <Modal isOpen={true} toggle={onClose}>
            <ModalHeader toggle={onClose}>Add new place</ModalHeader>
            <ModalBody>
                <form onSubmit={submit}>
                    <Label>Name</Label>
                    <input
                        className="form-control"
                        ref={(e) => {
                            inputRef(e);
                            inputElemRef.current = e;
                        }}
                        {...inputRest}
                    />
                    {errors.name?.type === 'required' && <div className="text-danger">Name cannot be empty</div>}
                    <Label>Area</Label>
                    <SearchingAutocomplete
                        id={getValues('area.id')}
                        title={getValues('area.name')}
                        placeholder="Not set"
                        table="Areas"
                        onSelect={(id, name) => setValue('area', id && name ? { id, name } : null)}
                    />
                    {errors.area?.type === 'required' && <div className="text-danger">Area is required</div>}
                </form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={submit}>OK</Button>
                <Button color="secondary" onClick={onClose}>Cancel</Button>
            </ModalFooter>
        </Modal>
    );
};

export default CreatePlaceModal;