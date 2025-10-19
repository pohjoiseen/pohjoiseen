import * as React from 'react';
import ConfirmModal, { ConfirmModalProps } from './ConfirmModal';
import { ReactNode, useEffect, useState } from 'react';

let setGlobalConfirmState: ((props: ConfirmModalProps) => void) | undefined;

const defaultGlobalConfirmState: ConfirmModalProps = {
    message: '',
    isOpen: false,
    onYes: () => {},
    onNo: () => {}
};

export const ModalContainer = ({ children }: { children: ReactNode }) => {
    const [confirmState, setConfirmState] =
        useState<ConfirmModalProps>(defaultGlobalConfirmState);
    
    useEffect(() => {
        if (setGlobalConfirmState) {
            throw new Error('Only one instance of ModalContainer can be used');
        }
        setGlobalConfirmState = setConfirmState;
    }, []);
    
    return (
        <>
            {children}
            <ConfirmModal {...confirmState} />
        </>
    );
};

export const confirmModal = (message: string): Promise<boolean> => {
    return new Promise((resolve) => {
        if (!setGlobalConfirmState) {
            throw new Error('No ModalContainer used');
        }
        setGlobalConfirmState({
            message,
            isOpen: true,
            onYes: () => {
                setGlobalConfirmState!(defaultGlobalConfirmState);
                resolve(true);
            },
            onNo: () => {
                setGlobalConfirmState!(defaultGlobalConfirmState);
                resolve(false);
            }
        })
    });
};
