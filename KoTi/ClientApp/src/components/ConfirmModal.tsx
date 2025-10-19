import * as React from 'react';
import { Button, Modal, ModalBody, ModalFooter } from 'reactstrap';

export interface ConfirmModalProps {
    message: string;
    isOpen: boolean;
    onYes: () => void;
    onNo: () => void;
}
const ConfirmModal = ({ message, isOpen, onYes, onNo }: ConfirmModalProps) => {
    return (
        <Modal isOpen={isOpen} toggle={onNo}>
            <ModalBody>
                <p>{message}</p>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={onYes}>Yes</Button>
                <Button color="secondary" onClick={onNo}>No</Button>
            </ModalFooter>
        </Modal>
    );
};

export default ConfirmModal;