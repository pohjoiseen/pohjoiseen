import * as React from 'react';
import { Button, Label, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import { useForm } from 'react-hook-form';
import { useEffect, useRef } from 'react';

interface CreatePostModalProps {
    onClose: () => void;
    onSubmit: (post: { id: number, name: string, title: string, language: string }) => void;
}
const CreateModal = ({ onClose, onSubmit }: CreatePostModalProps) => {
    const { register, handleSubmit,
        formState: { errors } } = useForm<{ id: number, name: string, title: string, language: string }>({ defaultValues: { id: 0, name: '', title: '', language: 'ru' } });
    const inputRef = useRef<HTMLInputElement | null>();
    const { ref, ...rest } = register('title', { required: true });
    useEffect(() => {
        setTimeout(() => inputRef.current?.focus(), 0);
    }, [])

    const submit = handleSubmit((post) => {
        onClose();
        onSubmit(post);
    });

    return (
        <Modal isOpen={true} toggle={onClose}>
            <ModalHeader toggle={onClose}>Add new post</ModalHeader>
            <ModalBody>
                <form onSubmit={submit}>
                    <Label>Title</Label>
                    <input
                        className="form-control"
                        autoComplete="off"
                        ref={(e) => {
                            ref(e);
                            inputRef.current = e;
                        }}
                        {...rest}
                    />
                    {errors.title?.type === 'required' && <div className="text-danger">Title cannot be empty</div>}
                    <Label>Name (URL slug)</Label>
                    <input
                        className="form-control"
                        autoComplete="off"
                        {...register('name', { required: true })}
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