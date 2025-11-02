/**
 * <ContentEditor>: wrapper for Monaco editor core, customized for editing Markdown content for KoTi/Fennica3,
 * with side panes for inserting stuff, previewing and editing metadata.
 */
import * as React from 'react';
import * as monaco from 'monaco-editor';
import { forwardRef, useCallback, useEffect, useImperativeHandle, useRef, useState } from 'react';
import { Col, Container, Nav, NavItem, NavLink, Row } from 'reactstrap';
import { getPicture } from '../api/pictures';
import { getPost } from '../api/posts';
import InsertPane from './InsertPane';

interface ContentEditorProps {
    initialValue: string;
    metaTabName: string;
    onSave: () => void;
}

export interface ContentEditorRef {
    getValue: () => string;
} 

// match content (picture:XXX, post:XXX, etc.) link under position
// return content id if found or 0 if not
// probably can be written better
const matchContentLink = (model: monaco.editor.ITextModel, position: monaco.Position, keyword: string) => {
    const word = model.getWordAtPosition(position);
    let id = 0;
    // case 1: position is over the keyword, look for ':' and then id
    if (word?.word === keyword) {
        position = position.with(undefined, word.endColumn);
        if (model.getValueInRange(monaco.Range.fromPositions(position, position.delta(undefined, 1))) === ':') {
            position = position.delta(undefined, 1);
            const idWord = model.getWordAtPosition(position);
            if (idWord) {
                id = parseInt(idWord.word, 10);
                if (!isNaN(id)) {
                    return id;
                }
            }
        }
    // case 2: position is over numeric id, look for ':' and then keyword before
    } else if (word) {
        id = parseInt(word.word, 10);
        if (!isNaN(id)) {
            position = position.with(undefined, word.startColumn - 1);
            if (model.getValueInRange(monaco.Range.fromPositions(position, position.delta(undefined, 1))) === ':') {
                position = position.delta(undefined, -1);
                const keyWord = model.getWordAtPosition(position);
                if (keyWord?.word === keyword) {
                    return id;
                }
            }
        }
    // case 3: position is over ':', look for id after and keyword before
    } else if (model.getValueInRange(monaco.Range.fromPositions(position, position.delta(undefined, 1))) === ':') {
        const keywordPosition = position.delta(undefined, -1);
        const keyWord = model.getWordAtPosition(keywordPosition);
        if (keyWord?.word === keyword) {
            const idPosition = position.delta(undefined, 1);
            const idWord = model.getWordAtPosition(idPosition);
            if (idWord) {
                id = parseInt(idWord.word, 10);
                if (!isNaN(id)) {
                    return id;
                }
            }
        }
    }

    return 0;
}

enum ContentEditorTab {
    Insert
}

const ContentEditor = forwardRef<ContentEditorRef, ContentEditorProps>(({ initialValue, metaTabName, onSave }, ref) => {
    const editorElRef = useRef<HTMLDivElement>(null);
    const monacoRef = useRef<monaco.editor.IStandaloneCodeEditor>();
    const callbacksRef = useRef<Partial<ContentEditorProps>>({});
    
    useEffect(() => {
        callbacksRef.current = { onSave };
    }, [onSave])
    
    useEffect(() => {
        const editor = monaco.editor.create(editorElRef.current!, {
            value: initialValue,
            language: 'markdown',
            wordWrap: 'on',
            unicodeHighlight: {
                ambiguousCharacters: false
            },
            lineNumbers: 'off',
            dropIntoEditor: {
                enabled: true
            }
        });
        
        editor.addAction({
            id: 'save',
            label: 'Save',
            keybindings: [
                monaco.KeyCode.KeyS | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "navigation",
            run: () => (callbacksRef.current!.onSave!)()
        });

        // picture popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position, token) => {
                const pictureId = matchContentLink(model, position, 'picture');
                if (pictureId) {
                    const picture = await getPicture(pictureId);
                    if (picture) {
                        return {
                            contents: [
                                { value: picture.filename },
                                { value: `<img src="${picture.detailsUrl}" height="200" alt="">`, supportHtml: true, isTrusted: true} 
                            ]
                        };
                    }
                }
            }
        });

        // post popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position, token) => {
                const postId = matchContentLink(model, position, 'post');
                if (postId) {
                    const post = await getPost(postId);
                    if (post) {
                        return {
                            contents: [
                                { value: `**${post.title}**  \n${post.date.toISOString().substring(0, 10)}-${post.name}`},
                                { value: post.titlePicture ? `<img src="${post.titlePicture.detailsUrl}" height="200" alt="">` : '', supportHtml: true, isTrusted: true}
                            ]
                        };
                    }
                }
            }
        });

        // workaround for "$0" appended to plain text paste data erroneously,
        // see https://github.com/microsoft/monaco-editor/issues/4386,
        // workaround from https://github.com/microsoft/monaco-editor/issues/4386#issuecomment-3436268354 there
        // still results in "Error: Trying to add a disposable to a DisposableStore that has already been disposed of. The added object will be leaked!" though...
        editor.getContainerDomNode().addEventListener('drop', e => {
            const data = e.dataTransfer?.getData('text/plain');
            if (data?.startsWith('picture:')) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                if (data?.endsWith(':null')) {
                    return;
                }
                // Handle the drop data yourself
                const position = editor.getTargetAtClientPoint(e.clientX, e.clientY);
                if (position?.range && data) {
                    editor.executeEdits('drop', [
                        {
                            range: position.range,
                            text: data,
                            forceMoveMarkers: true,
                        },
                    ]);
                }
            }
        });

        const onWindowResize = () => monacoRef.current!.layout();
        window.addEventListener('resize', onWindowResize);

        monacoRef.current = editor;
        
        return () => {
            window.removeEventListener('resize', onWindowResize);
            monacoRef.current!.dispose();
        }
    }, []);
    
    const insertText = useCallback((text: string) => {
        const selection = monacoRef.current!.getSelection();
        monacoRef.current!.executeEdits(null, [{ range: selection!, text, forceMoveMarkers: true }]);
    }, [monacoRef.current]);
    
    useImperativeHandle(ref, () => ({
        getValue() {
            return monacoRef.current!.getValue();
        }
    }), []);
    
    const [currentTab, setCurrentTab] = useState(ContentEditorTab.Insert);
    
    return <Container fluid className="flex-grow-1 overflow-y-auto">
        <Row className="h-100 overflow-y-auto"> 
            <Col xs="6"><div className="h-100" ref={editorElRef} /></Col>
            <Col xs="6" className="h-100 overflow-y-auto">
                <div className="d-flex flex-column h-100">
                    <Nav tabs className="mb-2">
                        <NavItem><NavLink 
                            active={currentTab === ContentEditorTab.Insert}
                            onClick={() => setCurrentTab(ContentEditorTab.Insert)}>
                            Insert
                        </NavLink></NavItem>
                    </Nav>
                    <div className="flex-grow-1 overflow-y-auto">
                        {currentTab === ContentEditorTab.Insert && <InsertPane onInsertText={insertText} />}
                    </div>
                </div>
            </Col>
        </Row>
    </Container>;
});

export default ContentEditor;
