///
/// Content edit page, main page of the app.  This hooks up the Monaco editor, the properties form
/// and various other things together.  This is not a web component as there is no need for it to be
/// reusable elsewhere.
///

// AMD-style standalone loader for Monaco editor, seems to be the only or at least
// the easiest way to use it in a no-build app
// TODO: still very painful to use something like monaco-editor without any typechecking
// This code was debugged and checked to work in 3.0, no major changes should be necessary, but if any major
// rework comes maaaybe going back to TS and a frontend build step would be necessary after all
require(["vs/editor/editor.main"], () => {
    const editorEl = $id('editor-container'), inputEl = $id('content-md-initial'),
        formEl = $id('content-form'), insertButtonEl = $id('insert-button');
    
    //// Monaco editor and handling stuff pasted into it
    
    // initialize editor
    const editor = monaco.editor.create(editorEl, {
        value: inputEl.value,
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
        wordBasedSuggestions: 'off',
        fontFamily: 'JetBrains Mono, monospace'
    });
    
    //// Text manupulation functions
    
    // wraps or unwraps text in the editor with specified strings
    const wrapText = (editor, before, after)=> {
        const range = editor.getSelection(), model = editor.getModel();
        if (!range) {
            return;
        }
        const offsetBefore = model.getOffsetAt(range.getStartPosition());
        const offsetAfter = model.getOffsetAt(range.getEndPosition());

        // if wrapping ends with a newline, look at the selection; if it ends with a newline itself, do the intuitive thing
        // and put the newline in 'after' to the end instead of beginning
        if (after.startsWith('\n') && model.getValueInRange(monaco.Range.fromPositions(
            model.getPositionAt(offsetAfter - '\n'.length), range.getEndPosition())) === '\n') {
            after = after.replace(/^\n(.*)/, '$1\n');
        }

        // if already wrapped in same strings, remove them instead
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
    const matchContentLink = (model, position, keyword) => {
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
    
    // performs all possible postprocessing on text that is programmatically inserted (normally internal links to some content)
    const handleTextInsertion = (text, model, range) => {
        // ensure web size images are created for all inserted picture links
        if (text.startsWith('picture:')) {
            const match = text.match(/picture:([0-9]+)/);
            if (match) {
                const id = parseInt(match[1]);
                // fire and forget
                apiEnsureWebSizes(id);
            }

            // insert full image Markdown markup, unless we are already between brackets
            if (model.getValueInRange(monaco.Range.fromPositions(range.getStartPosition(), range.getStartPosition().delta(undefined, -1))) !== '(' ||
                model.getValueInRange(monaco.Range.fromPositions(range.getEndPosition(), range.getEndPosition().delta(undefined, 1))) !== ')') {
                return `![](${text})\n`;
            }
        }

        // insert full link Markdown markup, unless we are already between brackets
        // wrap any existing selection as link text
        if (text.startsWith('post:') || text.startsWith('article')) {
            if (model.getValueInRange(monaco.Range.fromPositions(range.getStartPosition(), range.getStartPosition().delta(undefined, -1))) !== '(' ||
                model.getValueInRange(monaco.Range.fromPositions(range.getEndPosition(), range.getEndPosition().delta(undefined, 1))) !== ')') {

                return `[${model.getValueInRange(range)}](${text})`;
            }
        }

        return text;
    };

    //// Configure editor
    
    // add Ctrl-S action
    editor.addAction({
        id: 'save',
        label: 'Save',
        keybindings: [
            monaco.KeyCode.KeyS | monaco.KeyMod.CtrlCmd
        ],
        contextMenuGroupId: "navigation",
        run: () => formEl.requestSubmit()
    });
    
    // open post etc. links in new tab on Ctrl-B
    editor.addAction({
        id: 'open-link',
        label: 'Open linked content in new tab',
        keybindings: [
            monaco.KeyCode.KeyB | monaco.KeyMod.CtrlCmd
        ],
        contextMenuGroupId: 'navigation',
        run: () => {
            const model = editor.getModel(), position = editor.getPosition();
            const postId = matchContentLink(model, position, 'post');
            if (postId) {
                window.open(`../../../Posts/${postId}/`, '_blank');
            }
            const articleId = matchContentLink(model, position, 'article');
            if (articleId) {
                window.open(`../../../Articles/${articleId}/`, '_blank');
            }
        }
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
        run: (editor) => wrapText(editor, '**', '**')
    });
    editor.addAction({
        id: 'italic',
        label: 'Italic',
        keybindings: [
            monaco.KeyCode.KeyI | monaco.KeyMod.CtrlCmd
        ],
        contextMenuGroupId: "1_modification",
        precondition: "editorHasSelection",
        run: (editor) => wrapText(editor, '_', '_')
    });
    editor.addAction({
        id: 'gallery',
        label: 'Gallery',
        keybindings: [
            monaco.KeyCode.KeyG | monaco.KeyMod.CtrlCmd
        ],
        contextMenuGroupId: "1_modification",
        precondition: "editorHasSelection",
        run: (editor) => wrapText(editor, '<!--gallery-->\n', '\n<!--/gallery-->')
    });

    // picture preview popups
    monaco.languages.registerHoverProvider('markdown', {
        provideHover: async (model, position) => {
            const pictureId = matchContentLink(model, position, 'picture');
            if (pictureId) {
                const picture = await apiGetPicture(pictureId);
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
                const post = await apiGetPost(postId);
                if (post) {
                    return {
                        contents: [
                            { value: `**${post.title}**  \n${post.date.toISOString().substring(0, 10)}-${post.name}`},
                            { value: post.titlePicture ? `<img src="${post.titlePicture.detailsUrl}" height="190" alt="">` : '', supportHtml: true, isTrusted: true}
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
                const article = await apiGetArticle(articleId);
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
                const text = handleTextInsertion(data, editor.getModel(), position.range);
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
    
    //// Custom event listeners
    
    const insertSources = document.querySelectorAll('#editor-container, ' +
        '.insert-picture .list, .insert-uploaded-picture .list, .insert-post-link .list, .insert-article-link .list');

    document.addEventListener('content:save', () => {
        formEl.requestSubmit();
    });

    /**
     * @param {CustomEvent<{text: string}>} e 
     */
    const onInsert =(e) => {
        let text = e.detail.text;
        const selection = editor.getSelection();
        text = handleTextInsertion(text, editor.getModel(), selection);
        editor.executeEdits(null, [{ range: selection, text, forceMoveMarkers: true }]);
    };
    insertSources.forEach(el => el.addEventListener('content:insert', onInsert));
    
    let selectedItem = null;
    /**
     * @param {CustomEvent<{text: string}>} e
     */
    const onSelectInsertable = (e) => {
        selectedItem = e.detail?.text ?? null;
        if (selectedItem) {
            insertButtonEl.disabled = false;
            insertButtonEl.innerText = 'Insert: ' + selectedItem;
        } else {
            insertButtonEl.disabled = true;
            insertButtonEl.innerText = 'Select an item';
        }
    };
    insertSources.forEach(el => el.addEventListener('content:select-insertable', onSelectInsertable));
    insertButtonEl.addEventListener('click', () => {
       if (selectedItem) {
           editorEl.dispatchEvent(new CustomEvent('content:insert', { detail: { text: selectedItem } }));
       } 
    });
    
    //// Other event listeners

    // for some reason explicit resize handling seems to work more reliably
    window.addEventListener('resize', () => editor.layout());

    document.addEventListener('keydown', (e) => {
        // handle Ctrl-S also if the editor itself is not focused
        if (e.ctrlKey && (e.key === 's' || e.key === 'S')) {
            document.dispatchEvent(new Event('content:save'));
            e.preventDefault();
        }

        // prevent accidental Ctrl-Left/Right navigation (unless an input or textarea is focused)
        if (e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
            /** @type {HTMLElement} */
            const el = e.target;
            if (el.tagName !== 'INPUT' && el.tagName !== 'TEXTAREA') {
                e.preventDefault();
            }
        }
    });
    
    //// Ask for confirmation before leaving page if anything has been modified

    // serialize form, without viewState*** properties, on page load
    const getFormData = () => {
        const formData = new FormData(formEl);
        for (const key of formData.keys()) {
            if (key.startsWith('ViewState')) {
                formData.delete(key);
            }
        }
        formData.set('ContentMD', editor.getValue());
        return new URLSearchParams(formData).toString()
    };
    let lastSavedFormData = getFormData();

    // put editor value back into form on submit, remember serialized form state
    let savingFormData = null;
    formEl.addEventListener('htmx:configRequest', e => {
        if (e.detail.target === formEl) {
            e.detail.parameters.set('ContentMD', editor.getValue());
            savingFormData = getFormData();
        }
    });
    
    // on successful save, store what was being saved in lastSavedFormData 
    formEl.addEventListener('htmx:afterRequest', e => {
        if (e.detail.successful) {
            lastSavedFormData = savingFormData;
            savingFormData = null;
        }
    });

    // now actually handle beforeunload and compare serialized state
    document.addEventListener('beforeunload', e => {
        if (getFormData() !== lastSavedFormData) {
            e.preventDefault();
            return 'prevent';
        }
    });
});