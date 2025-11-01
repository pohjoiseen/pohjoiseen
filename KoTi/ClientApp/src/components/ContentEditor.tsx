/**
 * <ContentEditor>: wrapper for Monaco editor core, customized for editing Markdown content for KoTi/Fennica3.
 */
import * as React from 'react';
import * as monaco from 'monaco-editor';
import { forwardRef, useEffect, useImperativeHandle, useRef } from 'react';
import { getPicture } from '../api/pictures';
import { getPost } from '../api/posts';

interface ContentEditorProps {
    initialValue: string;
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

const ContentEditor = forwardRef<ContentEditorRef, ContentEditorProps>(({ initialValue, onSave }, ref) => {
    const elRef = useRef<HTMLDivElement>(null);
    const monacoRef = useRef<monaco.editor.IStandaloneCodeEditor>();
    const callbacksRef = useRef<Partial<ContentEditorProps>>({});
    
    useEffect(() => {
        callbacksRef.current = { onSave };
    }, [onSave])
    
    useEffect(() => {
        monacoRef.current = monaco.editor.create(elRef.current!, {
            value: initialValue,
            language: 'markdown',
            wordWrap: 'on',
            unicodeHighlight: {
                ambiguousCharacters: false
            },
            lineNumbers: 'off',
        });
        
        monacoRef.current.addAction({
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

        const onWindowResize = () => monacoRef.current!.layout();
        window.addEventListener('resize', onWindowResize);

        return () => {
            window.removeEventListener('resize', onWindowResize);
            monacoRef.current!.dispose();
        }
    }, []);
    
    useImperativeHandle(ref, () => ({
        getValue() {
            return monacoRef.current!.getValue();
        }
    }), []);
    
    return <div className="w-100 h-100" ref={elRef} />;
});

export default ContentEditor;
