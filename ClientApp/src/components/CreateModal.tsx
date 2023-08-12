import * as React from 'react';
import { Button, Label, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import { useForm } from 'react-hook-form';
import { useEffect, useRef } from 'react';

interface Creatable {
    name: string;
}

interface CreateModalProps<T extends Creatable> {
    object: T;
    title: string;
    onClose: () => void;
    onSubmit: (object: T) => void;
}
const CreateModal = <T extends Creatable>({ object, title, onClose, onSubmit }: CreateModalProps<T>) => {
    const { register, handleSubmit,
        formState: { errors } } = useForm<Creatable>({ defaultValues: object });
    const inputRef = useRef<HTMLInputElement | null>();
    const { ref, ...rest } = register('name', { required: true });
    useEffect(() => {
        setTimeout(() => inputRef.current?.focus(), 0);
    }, [])

    const submit = handleSubmit((object) => {
        onClose();
        onSubmit(object as T);
    });
    
    return (
        <Modal isOpen={true} toggle={onClose}>
            <ModalHeader toggle={onClose}>{title}</ModalHeader>
            <ModalBody>
                <form onSubmit={submit}>
                    <Label>Name</Label>
                    <input
                        className="form-control"
                        ref={(e) => {
                            ref(e);
                            inputRef.current = e;
                        }}
                        {...rest}
                    />
                    {errors.name?.type === 'required' && <div className="text-danger">Name cannot be empty</div>}
                </form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={submit}>OK</Button>
                <Button color="secondary" onClick={onClose}>Cancel</Button>
            </ModalFooter>
        </Modal>
    );
};

export default CreateModal;