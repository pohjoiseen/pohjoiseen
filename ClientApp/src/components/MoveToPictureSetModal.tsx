import * as React from 'react';
import { Button, Label, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import { useForm } from 'react-hook-form';
import { useEffect, useRef, useState } from 'react';
import { SearchingAutocomplete } from './SearchingAutocomplete';

interface MoveToPictureSetModalProps {
    onClose: () => void;
    onSubmit: (pictureSetId: number | null) => void;
}

const MoveToPictureSetModal = ({ onClose, onSubmit }: MoveToPictureSetModalProps) => {
    const inputElemRef = useRef<HTMLInputElement>(null);
    useEffect(() => {
        setTimeout(() => inputElemRef.current?.focus(), 0);
    }, [])
    const [folderId, setFolderId] = useState<number | null>(null);
    const [folderName, setFolderName] = useState<string | null>(null);

    const submit = () => {
        onSubmit(folderId);
        onClose();
    };

    return (
        <Modal isOpen={true} toggle={onClose}>
            <ModalHeader toggle={onClose}>Move selected picture(s) into folder</ModalHeader>
            <ModalBody>
                <form onSubmit={submit}>
                    <SearchingAutocomplete
                        id={folderId}
                        title={folderName}
                        placeholder="Not set"
                        table="PictureSets"
                        onSelect={(id, name) => {
                            setFolderId(id);
                            setFolderName(name);
                        }}
                    />
                </form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={submit}>OK</Button>
                <Button color="secondary" onClick={onClose}>Cancel</Button>
            </ModalFooter>
        </Modal>
    );
};

export default MoveToPictureSetModal;