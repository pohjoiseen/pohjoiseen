import * as React from 'react';
import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import { useEffect, useRef, useState } from 'react';

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
                    Sorry you cannot do this anymore in old UI, do it in the new one!
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