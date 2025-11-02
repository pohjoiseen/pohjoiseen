/**
 * <PreviewPane>: tab for <ContentEditor> that simply displays content preview in an iframe.
 */
import * as React from 'react';
import { useRef } from 'react';
import { Button, Nav, NavItem, NavLink } from 'reactstrap';

interface PreviewPaneProps {
    previewUrl: string;
}


const PreviewPane = ({ previewUrl }: PreviewPaneProps) => {
    const iframeRef = useRef<HTMLIFrameElement>(null);
    
    const reload = () => {
        // this bypasses cross-origin issues, with cooperation from main.ts in Fennica3
        iframeRef.current!.contentWindow!.postMessage('reload', process.env.REACT_APP_PREVIEW_HOST!);
    };
    
    return <div className="content-editor-pane">
        <Nav className="mb-2">
            <Button onClick={reload} color="primary" className="me-2">
                <i className="bi bi-arrow-clockwise" /> Refresh
            </Button>
            <Button onClick={() => iframeRef.current!.src += ''} className="me-2">
                <i className="bi bi-arrow-clockwise" /> Reset
            </Button>
            <Button onClick={() => window.open(process.env.REACT_APP_PREVIEW_HOST + previewUrl, '_blank')}>
                <i className="bi bi-box-arrow-up-right" /> Open in new tab
            </Button>
            <NavItem><NavLink disabled>
                Preview shows the last saved version.
            </NavLink></NavItem>
        </Nav>
        <iframe className="w-100 h-100 overflow-y-auto overflow-x-hidden" ref={iframeRef} src={process.env.REACT_APP_PREVIEW_HOST + previewUrl} />
    </div>;
};

export default PreviewPane;