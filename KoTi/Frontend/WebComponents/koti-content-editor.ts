//
// <koti-content-editor>: content edit page, main page of the app.  This hooks up the Monaco editor, the properties form
// and various other things together.  The element <koti-content-editor> 
//
import * as monaco from 'monaco-editor';
import { $id } from '../Common/common.ts';
import { apiEnsureWebSizes, apiGetArticle, apiGetPicture, apiGetPost } from '../Common/api.ts';

window.customElements.define('koti-content-editor', class extends HTMLElement {
    #editor: monaco.editor.IStandaloneCodeEditor = null!;
    #formEl: HTMLFormElement = null!;
    #insertButtonEl: HTMLButtonElement = null!;
    
    #savingFormData: string | null = null;
    #lastSavedFormData: string | null = null;
    
    connectedCallback() {
        const initialContentMdEl = $id(this.getAttribute('initial-content-md-id')!) as HTMLTextAreaElement;
        this.#formEl = $id(this.getAttribute('content-form-id')!) as HTMLFormElement;
        this.#insertButtonEl = $id(this.getAttribute('insert-button-id')!) as HTMLButtonElement;
        
        this.#editor = this.setupMonacoEditor(initialContentMdEl.value);
        
        //// Custom event listeners

        // just save everything
        this.addEventListener('koti-content-editor:save', () => {
            this.#formEl.requestSubmit();
        });

        // content:insert (bubbling to document), insert pseudo-link
        const onInsert = (e: CustomEvent<{text: string}>) => {
            let text = e.detail.text;
            const selection = this.#editor.getSelection();
            text = this.handleTextInsertion(text, this.#editor.getModel()!, selection!);
            this.#editor.executeEdits(null, [{range: selection!, text, forceMoveMarkers: true}]);
        };
        document.addEventListener('content:insert', onInsert as EventListener);

        // content:select-insertable (bubbling to document), remember what to insert and show in insert button
        let selectedItem: string | null = null;
        const onSelectInsertable = (e: CustomEvent<{text: string}>) => {
            selectedItem = e.detail?.text ?? null;
            if (selectedItem) {
                this.#insertButtonEl.disabled = false;
                this.#insertButtonEl.innerText = 'Insert: ' + selectedItem;
            } else {
                this.#insertButtonEl.disabled = true;
                this.#insertButtonEl.innerText = 'Select an item';
            }
        };
        document.addEventListener('content:select-insertable', onSelectInsertable as EventListener);
        
        // click on insert button inserts selected item
        this.#insertButtonEl.addEventListener('click', () => {
            if (selectedItem) {
                this.dispatchEvent(new CustomEvent('content:insert', {bubbles: true, detail: {text: selectedItem}}));
            }
        });

        //// Other event listeners

        // for some reason explicit resize handling seems to work more reliably
        window.addEventListener('resize', () => this.#editor.layout());

        document.addEventListener('keydown', (e) => {
            // handle Ctrl-S also if the editor itself is not focused
            if (e.ctrlKey && (e.key === 's' || e.key === 'S')) {
                this.dispatchEvent(new Event('koti-content-editor:save'));
                e.preventDefault();
                e.stopPropagation();
            }

            // prevent accidental Ctrl-Left/Right navigation (unless an input or textarea is focused)
            if (e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
                /** @type {HTMLElement} */
                const el = e.target as HTMLElement;
                if (el.tagName !== 'INPUT' && el.tagName !== 'TEXTAREA') {
                    e.preventDefault();
                }
            }
        });

        this.setupExitConfirmation();
    }

    setupMonacoEditor(initialValue: string) {
        // create editor
        const editor = monaco.editor.create(this, {
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
            wordBasedSuggestions: 'off',
            // assume we have JetBrains Mono (which I certainly do), if not then whatever
            fontFamily: 'JetBrains Mono, monospace'
        });

        //// configure actions

        // add Ctrl-S action
        editor.addAction({
            id: 'save',
            label: 'Save',
            keybindings: [
                monaco.KeyCode.KeyS | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "navigation",
            run: () => this.#formEl.requestSubmit()
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
                const postId = this.matchContentLink(model!, position!, 'post');
                if (postId) {
                    window.open(`../../../Posts/${postId}/`, '_blank');
                }
                const articleId = this.matchContentLink(model!, position!, 'article');
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
            run: (editor) => this.wrapText(editor as monaco.editor.IStandaloneCodeEditor, '**', '**')
        });
        editor.addAction({
            id: 'italic',
            label: 'Italic',
            keybindings: [
                monaco.KeyCode.KeyI | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "1_modification",
            precondition: "editorHasSelection",
            run: (editor) => this.wrapText(editor as monaco.editor.IStandaloneCodeEditor, '_', '_')
        });
        editor.addAction({
            id: 'gallery',
            label: 'Gallery',
            keybindings: [
                monaco.KeyCode.KeyG | monaco.KeyMod.CtrlCmd
            ],
            contextMenuGroupId: "1_modification",
            precondition: "editorHasSelection",
            run: (editor) => this.wrapText(editor as monaco.editor.IStandaloneCodeEditor, '<!--gallery-->\n', '\n<!--/gallery-->')
        });

        //// preview popups, those call JSON API methods, it's okay if those fail
        
        // picture preview popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position) => {
                const pictureId = this.matchContentLink(model, position, 'picture');
                if (pictureId) {
                    const picture = await apiGetPicture(pictureId);
                    if (picture) {
                        return {
                            contents: [
                                {value: picture.filename},
                                {
                                    value: `<img src="${picture.detailsUrl}" height="200" alt="">`,
                                    supportHtml: true,
                                    isTrusted: true
                                }
                            ]
                        };
                    }
                }
            }
        });

        // post preview popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position) => {
                const postId = this.matchContentLink(model, position, 'post');
                if (postId) {
                    const post = await apiGetPost(postId);
                    if (post) {
                        return {
                            contents: [
                                {value: `**${post.title}**  \n${post.date.toISOString().substring(0, 10)}-${post.name}`},
                                {
                                    value: post.titlePicture ? `<img src="${post.titlePicture.detailsUrl}" height="190" alt="">` : '',
                                    supportHtml: true,
                                    isTrusted: true
                                }
                            ]
                        };
                    }
                }
            }
        });

        // article preview popups
        monaco.languages.registerHoverProvider('markdown', {
            provideHover: async (model, position) => {
                const articleId = this.matchContentLink(model, position, 'article');
                if (articleId) {
                    const article = await apiGetArticle(articleId);
                    if (article) {
                        return {
                            contents: [
                                {value: `**${article.title}**  \n${article.name}`},
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
                    const text = this.handleTextInsertion(data, editor.getModel()!, position.range);
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
        
        return editor;
    }
    
    // wraps or unwraps text in the editor with specified strings
    wrapText(editor: monaco.editor.IStandaloneCodeEditor, before: string, after: string){
        const range = editor.getSelection(), model = editor.getModel()!;
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
                    {range: rangeBefore, text: null},
                    {range: rangeAfter, text: null}
                ]);
                return;
            }
        }

        editor.executeEdits(null, [
            {range: monaco.Range.fromPositions(range.getStartPosition()), text: before},
            {range: monaco.Range.fromPositions(range.getEndPosition()), text: after}
        ]);
    }

    // match content (picture:XXX, post:XXX, etc.) link under position
    // return content id if found or 0 if not
    // probably can be written better
    matchContentLink(model: monaco.editor.ITextModel, position: monaco.Position, keyword: string) {
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
    handleTextInsertion(text: string, model: monaco.editor.ITextModel, range: monaco.Range) {
        if (text.startsWith('picture:')) {
            const match = text.match(/picture:([0-9]+)/);
            // ensure web size images are created for all inserted picture links
            // XXX not very clean way to do it from here, but works
            if (match) {
                const id = parseInt(match[1]!);
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
    }

    getFormData() {
        const formData = new FormData(this.#formEl);
        for (const key of formData.keys()) {
            if (key.startsWith('ViewState')) {
                formData.delete(key);
            }
        }

        const obj = Object.fromEntries(
            Array.from(formData.keys()).toSorted().map(key => [
                key, formData.getAll(key).length > 1 ?
                    formData.getAll(key) : formData.get(key)
            ]));
        obj['ContentMD'] = this.#editor.getValue();

        return JSON.stringify(obj);
    }

    // Ask for confirmation before leaving page if anything has been modified
    setupExitConfirmation() {
        // serialize form, without viewState*** properties, on page load
        // must however wait until all components are initialized, some are creating hidden
        // inputs dynamically.  This is hacky but 0.1s seems enough
        setTimeout(() => this.#lastSavedFormData = this.getFormData(), 100);

        // put editor value back into form on submit, remember serialized form state
        this.#formEl.addEventListener('htmx:configRequest', (e: Event) => {
            const htmxEvent = e as CustomEvent<{target: HTMLElement, parameters: URLSearchParams}>;
            if (htmxEvent.detail.target === this.#formEl) {
                htmxEvent.detail.parameters.set('ContentMD', this.#editor.getValue());
                this.#savingFormData = this.getFormData();
            }
        });

        // on successful save, store what was being saved in lastSavedFormData 
        this.#formEl.addEventListener('htmx:afterRequest', e => {
            const htmxEvent = e as CustomEvent<{successful: boolean}>;
            if (htmxEvent.detail.successful) {
                this.#lastSavedFormData = this.#savingFormData;
                this.#savingFormData = null;
            }
        });

        // now actually handle beforeunload and compare serialized state
        window.addEventListener('beforeunload', e => {
            const formData = this.getFormData();
            if (formData !== this.#lastSavedFormData) {
                //console.log("last saved data", this.#lastSavedFormData);
                //console.log("current data", formData);
                e.preventDefault();
                return 'prevent';
            }
        });
    }
});