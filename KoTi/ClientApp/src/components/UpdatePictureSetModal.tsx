import * as React from 'react';
import { Button, FormGroup, Input, Label, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import { useForm } from 'react-hook-form';
import { useEffect, useRef } from 'react';
import PictureSet from '../model/PictureSet';

interface UpdatePictureSetModal {
    pictureSet: PictureSet;
    onClose: () => void;
    onSubmit: (pictureSet: PictureSet) => void;
}
const UpdatePictureSetModal = ({ pictureSet, onClose, onSubmit }: UpdatePictureSetModal) => {
    const { register, handleSubmit,
        formState: { errors } } = useForm<PictureSet>({ defaultValues: pictureSet });
    const inputElemRef = useRef<HTMLInputElement | null>();
    const { ref: inputRef, ...inputRest } = register('name', { required: true });
    const isPrivate = register('isPrivate');
    useEffect(() => {
        setTimeout(() => inputElemRef.current?.focus(), 0);
    }, [])

    const submit = handleSubmit((object) => {
        const result = object as unknown as PictureSet;
        onClose();
        onSubmit(result);        
    });

    return (
        <Modal isOpen={true} toggle={onClose}>
            <ModalHeader toggle={onClose}>Edit folder</ModalHeader>
            <ModalBody>
                <form onSubmit={submit}>
                    <Label>Name</Label>
                    <input
                        className="form-control"
                        autoComplete="off"
                        ref={(e) => {
                            inputRef(e);
                            inputElemRef.current = e;
                        }}
                        {...inputRest}
                    />
                    {errors.name?.type === 'required' && <div className="text-danger">Name cannot be empty</div>}
                    <FormGroup check inline className="mt-2">
                        <input
                            type="checkbox"
                            className="form-check-input"
                            id="picture-set-is-private"
                            {...isPrivate}
                        />
                        <Label htmlFor="picture-set-is-private" check>Private/Draft</Label>
                    </FormGroup>
                </form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={submit}>OK</Button>
                <Button color="secondary" onClick={onClose}>Cancel</Button>
            </ModalFooter>
        </Modal>
    );
};

export default UpdatePictureSetModal;