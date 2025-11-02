/**
 * <InsertPane>: tab for <ContentEditor> that handles inserting references to various kinds of content
 * to post etc. Markdown text.
 */
import * as React from 'react';
import { useCallback, useState } from 'react';
import { Button, Nav, NavItem, NavLink } from 'reactstrap';
import InsertPicture from './InsertPicture';
import InsertUpload from './InsertUpload';
import InsertPostLink from './InsertPostLink';

interface InsertPaneProps {
    isActive: boolean;
    onInsertText: (text: string) => void;
}

enum InsertPaneMode {
    Picture,
    Upload,
    PostLink
}

const InsertPane = ({ isActive, onInsertText }: InsertPaneProps) => {
    const [mode, setMode] = useState(InsertPaneMode.Picture);
    const [insertText, setInsertText] = React.useState('');
    
    const selectPicture = useCallback((pictureId: number | null, insertImmediately?: boolean) => {
       const text = pictureId ? "picture:" + pictureId : '';
       setInsertText(text);
       if (insertImmediately && pictureId) {
           onInsertText(text);
       }
    }, [setInsertText, onInsertText]);

    const selectPost = useCallback((postId: number | null, insertImmediately?: boolean) => {
        const text = postId ? "post:" + postId : '';
        setInsertText(text);
        if (insertImmediately && postId) {
            onInsertText(text);
        }
    }, [setInsertText, onInsertText]);

    return <div className={`content-editor-pane ${!isActive ? 'd-none' : ''}`}>
        <Nav pills className="mb-2">
            <NavItem>
                <NavLink className="cursor-pointer" 
                         active={mode === InsertPaneMode.Picture} 
                         onClick={() => setMode(InsertPaneMode.Picture)}>
                    Photo
                </NavLink>
            </NavItem>
            <NavItem>
                <NavLink className="cursor-pointer"
                         active={mode === InsertPaneMode.Upload} 
                         onClick={() => setMode(InsertPaneMode.Upload)}>
                    Upload picture
                </NavLink>
            </NavItem>
            <NavItem>
                <NavLink className="cursor-pointer"
                         active={mode === InsertPaneMode.PostLink}
                         onClick={() => setMode(InsertPaneMode.PostLink)}>
                    Post link
                </NavLink>
            </NavItem>
            <div className="flex-grow-1" />
            <Button disabled={!insertText} onClick={() => onInsertText(insertText)}>
                {insertText ? `Insert: ${insertText}` : 'Select an item'}
            </Button>
        </Nav>
        <InsertPicture isActive={mode === InsertPaneMode.Picture} onSelect={selectPicture} />
        <InsertUpload isActive={mode === InsertPaneMode.Upload} onSelect={selectPicture} />
        <InsertPostLink isActive={mode === InsertPaneMode.PostLink} onSelect={selectPost} />
    </div>;
};

export default InsertPane;