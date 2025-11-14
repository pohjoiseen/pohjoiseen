/**
 * <ContentEditor>: wrapper for Monaco editor core, customized for editing Markdown content for KoTi/Fennica3,
 * with side panes for inserting stuff, previewing and editing metadata.
 */
import * as React from 'react';
import * as monaco from 'monaco-editor';
import { forwardRef, ReactNode, useCallback, useEffect, useImperativeHandle, useRef, useState } from 'react';
import { Col, Container, Nav, NavItem, NavLink, Row } from 'reactstrap';
import { getPicture } from '../api/pictures';
import { getPost } from '../api/posts';
import { getArticle } from '../api/articles';
import { useEnsurePictureWebSizesMutation } from '../data/mutations';
import InsertPane from './InsertPane';
import PreviewPane from './PreviewPane';

interface ContentEditorProps {
    initialValue: string;
    metaTabName: string;
    metaTab: ReactNode;
    previewUrl: string;
    onSave: () => void;
}

export interface ContentEditorRef {
    getValue: () => string;
} 

// wraps or unwraps text in the editor with specified strings
const wrapText = (editor: monaco.editor.IStandaloneCodeEditor, before: string, after: string)=> {
    const range = editor.getSelection(), model = editor.getModel() as monaco.editor.ITextModel;
    if (!range) {
        return;
    }
    
    // if already wrapped in same strings, remove them instead
    const offsetBefore = model.getOffsetAt(range.getStartPosition());
    const offsetAfter = model.getOffsetAt(range.getEndPosition());
    const rangeBefore = monaco.Range.fromPositions(range.getStartPosition(), model.getPositionAt(offsetBefore + before.length));
    if (model.getValueInRange(rangeBefore) === before) {
        const rangeAfter = monaco.Range.fromPositions(model.getPositionAt(offsetAfter - after.length), range.getEndPosition());
        if (model.getValueInRange(rangeAfter) === after) {
            editor.executeEdits(null, [
                { range: rangeBefore, text: null },
                { range: rangeAfter, text: null }
            ]);
            return;
        }
    }

    editor.executeEdits(null, [
        { range: monaco.Range.fromPositions(range.getStartPosition()), text: before },
        { range: monaco.Range.fromPositions(range.getEndPosition()), text: after }
    ]);
};

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
    Meta,
    Insert,
    Preview
}

const ContentEditor = forwardRef<ContentEditorRef, ContentEditorProps>(({ initialValue, metaTabName, metaTab, previewUrl, onSave }, ref) => {
    const editorElRef = useRef<HTMLDivElement>(null);
    const monacoRef = useRef<monaco.editor.IStandaloneCodeEditor>();
    const callbacksRef = useRef<Partial<ContentEditorProps>>({});
    const ensurePictureWebSizesMutation = useEnsurePictureWebSizesMutation();
    
    // store onSave into a ref, so that editor as initialized only once in useEffect() below can still always use the current onSave
    useEffect(() => {
        callbacksRef.current = { onSave };
    }, [onSave])
    
    // performs all possible postprocessing on text that is programmatically inserted (normally internal links to some content)
    const handleTextInsertion = useCallback((text: string, model: monaco.editor.ITextModel, range: monaco.Range) => {
        // ensure web size images are created for all inserted picture links
        if (text.startsWith('picture:')) {
            const match = text.match(/picture:([0-9]+)/);
            if (match) {
                const id = parseInt(match[1]);
                ensurePictureWebSizesMutation.mutateAsync(id);  // fire and forget
            }

            // insert full image Markdown markup, unless we are already between brackets
            if (model.getValueInRange(monaco.Range.fromPositions(range.getStartPosition(), range.getStartPosition().delta(undefined, -1))) !== '(' ||
                model.getValueInRange(monaco.Range.fromPositions(range.getEndPosition(), range.getEndPosition().delta(undefined, 1))) !== ')') {
                return `![](${text})\n`;
            }
        }

        // insert full link Markdown markup, unless we are already between brackets
        if (text.startsWith('post:') || text.startsWith('article')) {
            if (model.getValueInRange(monaco.Range.fromPositions(range.getStartPosition(), range.getStartPosition().delta(undefined, -1))) !== '(' ||
                model.getValueInRange(monaco.Range.fromPositions(range.getEndPosition(), range.getEndPosition().delta(undefined, 1))) !== ')') {
                return `[](${text})`;
            }
        }
        
        return text;
    }, [ensurePictureWebSizesMutation]);
    
    // actual Monaco initialization
    // empty dependency array, run this only once, monaco is effectively "uncontrolled input" as far as React parts are concerned
    useEffect(() => {
        const editor = monaco.editor.create(editorElRef.current!, {
            value: initialValue,
            language: 'markdown',
            wordWrap: 'on',
            // this removes annoying highlighting on short Russian words
            unicodeHighlight: {
                ambiguousCharacters: false
            },
            lineNumbers: 'off',
            dropIntoEditor: {
                enabled: true
            },
            // this is more annoying than useful in my experience
            wordBasedSuggestions: 'off'
        });
        
        // add Ctrl-S action
        editor.addAction({
            id: 'save',
            label: 'Save',
            keybindings: [
                monaco.KeyCode.KeyS | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "navigation",
            run: () => (callbacksRef.current!.onSave!)()
        });

        // add actions for wrapping stuff into markup
        editor.addAction({
            id: 'bold',
            label: 'Bold',
            keybindings: [
                monaco.KeyCode.KeyB | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "1_modification",
            precondition: "editorHasSelection",
            run: (editor) => wrapText(editor as monaco.editor.IStandaloneCodeEditor, '**', '**')
        });
        editor.addAction({
            id: 'italic',
            label: 'Italic',
            keybindings: [
                monaco.KeyCode.KeyI | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "1_modification",
            precondition: "editorHasSelection",
            run: (editor) => wrapText(editor as monaco.editor.IStandaloneCodeEditor, '_', '_')
        });
        editor.addAction({
            id: 'gallery',
            label: 'Gallery',
            keybindings: [
                monaco.KeyCode.KeyG | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "1_modification",
            precondition: "editorHasSelection",
            run: (editor) => wrapText(editor as monaco.editor.IStandaloneCodeEditor, '<!--gallery-->\n', '\n<!--/gallery-->')
        });

        // picture preview popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position) => {
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

        // post preview popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position) => {
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

        // article preview popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position) => {
                const articleId = matchContentLink(model, position, 'article');
                if (articleId) {
                    const article = await getArticle(articleId);
                    if (article) {
                        return {
                            contents: [
                                { value: `**${article.title}**  \n${article.name}`},
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
            if (data?.startsWith('picture:') || data?.startsWith('post:') || data?.startsWith('article:')) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                if (data?.endsWith(':null')) {
                    return;
                }
                // Handle the drop data yourself
                const position = editor.getTargetAtClientPoint(e.clientX, e.clientY);
                if (position?.range && data) {
                    const text = handleTextInsertion(data, editor.getModel()!, position.range);
                    editor.executeEdits('drop', [
                        {
                            range: position.range,
                            text,
                            forceMoveMarkers: true,
                        },
                    ]);
                }
            }
        });

        // for some reason explicit resize handling seems to work more reliably
        const onWindowResize = () => monacoRef.current!.layout();
        window.addEventListener('resize', onWindowResize);
        
        const onKeyDown = (e: KeyboardEvent) => {
            // handle Ctrl-S also if the editor itself is not focused
            if (e.ctrlKey && (e.key === 's' || e.key === 'S')) {
                callbacksRef.current.onSave?.();
                e.preventDefault();
            }

            // prevent accidental Ctrl-Left/Right navigation 
            if (e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
                e.preventDefault();
            }
        };
        window.addEventListener('keydown', onKeyDown);

        monacoRef.current = editor;
        
        return () => {
            window.removeEventListener('resize', onWindowResize);
            window.removeEventListener('keydown', onKeyDown);
            monacoRef.current!.dispose();
        }
    }, []);  // eslint-disable-line
    
    // programmatically insert text into editor
    const insertText = useCallback((text: string) => {
        const selection = monacoRef.current!.getSelection();
        text = handleTextInsertion(text, monacoRef.current!.getModel()!, selection!);
        monacoRef.current!.executeEdits(null, [{ range: selection!, text, forceMoveMarkers: true }]);
    }, [handleTextInsertion]);
    
    useImperativeHandle(ref, () => ({
        getValue() {
            return monacoRef.current!.getValue();
        }
    }), []);
    
    const [currentTab, setCurrentTab] = useState(ContentEditorTab.Meta);
    
    return <Container fluid className="flex-grow-1 overflow-y-auto">
        <Row className="h-100 overflow-y-auto"> 
            <Col xs="6"><div className="h-100" ref={editorElRef} /></Col>
            <Col xs="6" className="h-100 overflow-y-auto">
                <div className="d-flex flex-column h-100">
                    <Nav tabs className="mb-2 cursor-pointer">
                        <NavItem><NavLink
                            active={currentTab === ContentEditorTab.Meta}
                            onClick={() => setCurrentTab(ContentEditorTab.Meta)}>
                            {metaTabName}
                        </NavLink></NavItem>
                        <NavItem><NavLink 
                            active={currentTab === ContentEditorTab.Insert}
                            onClick={() => setCurrentTab(ContentEditorTab.Insert)}>
                            Insert
                        </NavLink></NavItem>
                        <NavItem><NavLink
                            active={currentTab === ContentEditorTab.Preview}
                            onClick={() => setCurrentTab(ContentEditorTab.Preview)}>
                            Preview
                        </NavLink></NavItem>
                    </Nav>
                    <div className="flex-grow-1 overflow-y-auto">
                        <div className={currentTab === ContentEditorTab.Meta ? 'content-editor-pane p-2' : 'd-none'}>{metaTab}</div>
                        <InsertPane onInsertText={insertText} isActive={currentTab === ContentEditorTab.Insert} />
                        {/* Preview tab can be just destroyed and recreated when switched to and from, this is even desirable with the big iframe in it */}
                        {currentTab === ContentEditorTab.Preview && <PreviewPane previewUrl={previewUrl} />}
                    </div>
                </div>
            </Col>
        </Row>
    </Container>;
});

export default ContentEditor;
