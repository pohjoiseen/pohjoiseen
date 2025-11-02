/**
 * <InsertPane>: tab for <ContentEditor> that handles inserting references to various kinds of content
 * to post etc. Markdown text.
 */
import * as React from 'react';
import { useCallback, useState } from 'react';
import { Button, Nav, NavItem, NavLink } from 'reactstrap';
import InsertPicture from './InsertPicture';

interface InsertPaneProps {
    onInsertText: (text: string) => void;
}

enum InsertPaneMode {
    Picture
}

const InsertPane = ({ onInsertText }: InsertPaneProps) => {
    const [mode, setMode] = useState(InsertPaneMode.Picture);
    const [insertText, setInsertText] = React.useState('');
    
    const selectPicture = useCallback((pictureId: number | null, insertImmediately?: boolean) => {
       const text = pictureId ? "picture:" + pictureId : '';
       setInsertText(text);
       if (insertImmediately) {
           onInsertText(text);
       }
    }, [setInsertText, onInsertText]);
    
    return <div className="insert-pane">
        <Nav pills className="mb-2">
            <NavItem><NavLink active={mode === InsertPaneMode.Picture} onClick={() => setMode(InsertPaneMode.Picture)}>
                Photo
            </NavLink></NavItem>
            <div className="flex-grow-1" />
            <Button disabled={!insertText} onClick={() => onInsertText(insertText)}>
                {insertText ? `Insert: ${insertText}` : 'Select an item'}
            </Button>
        </Nav>
        {mode === InsertPaneMode.Picture && <InsertPicture onSelect={selectPicture} />}
    </div>;
};

export default InsertPane;