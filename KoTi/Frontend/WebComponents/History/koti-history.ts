//
// <koti-history>: earlier versions of the content on the edit page (History tab).  Rows of the server-rendered
// table have a revision id; Compare loads that text and shows it against the current text of the editor
// in a side-by-side diff (old on the left), Restore puts the old text into the editor as a regular edit.
//
import * as monaco from 'monaco-editor';
import type ContentEditorElement from '../ContentEditor/koti-content-editor.ts';

export default class HistoryElement extends HTMLElement {
    #initialized = false;
    #dialogEl: HTMLDialogElement = null!;
    #diffEditor: monaco.editor.IStandaloneDiffEditor | null = null;
    #revisionText = '';

    get #editor() {
        return (document.querySelector('koti-content-editor') as ContentEditorElement).editor;
    }

    connectedCallback() {
        // fires again when <koti-tabs> wraps the tab
        if (this.#initialized) return;
        this.#initialized = true;

        this.#dialogEl = this.querySelector('dialog')!;
        for (const rowEl of this.querySelectorAll<HTMLElement>('tr[data-id]')) {
            rowEl.querySelector('.history-compare')!.addEventListener('click',
                () => this.compare(rowEl.dataset.id!, rowEl.dataset.label!));
        }
        this.querySelector('.history-close')!.addEventListener('click', () => this.#dialogEl.close());
        this.querySelector('.history-restore')!.addEventListener('click', () => this.restore());
        this.#dialogEl.addEventListener('close', () => this.disposeDiff());
    }

    async compare(id: string, label: string) {
        let text: string;
        try {
            const response = await fetch(`/api/Revisions/${id}`);
            if (!response.ok) throw new Error(`${response.status}`);
            text = (await response.json()).contentMD;
        } catch (e) {
            alert(`Could not load this version: ${e}`);
            return;
        }

        this.#revisionText = text.replaceAll('\r\n', '\n');
        this.querySelector('.history-title')!.textContent = `${label} (left) compared to the text in the editor (right)`;
        this.#dialogEl.showModal();

        this.disposeDiff();
        this.#diffEditor = monaco.editor.createDiffEditor(this.querySelector('.history-diff')!, {
            readOnly: true,
            originalEditable: false,
            wordWrap: 'on',
            diffWordWrap: 'on',
            lineNumbers: 'off',
            renderSideBySide: true,
            useInlineViewWhenSpaceIsLimited: false,
            automaticLayout: true,
            unicodeHighlight: { ambiguousCharacters: false },
            fontFamily: 'JetBrains Mono, monospace'
        });
        this.#diffEditor.setModel({
            original: monaco.editor.createModel(this.#revisionText, 'markdown'),
            modified: monaco.editor.createModel(this.#editor.getValue().replaceAll('\r\n', '\n'), 'markdown')
        });
    }

    restore() {
        const editor = this.#editor;
        editor.pushUndoStop();
        editor.executeEdits('koti-history', [{range: editor.getModel()!.getFullModelRange(), text: this.#revisionText}]);
        editor.pushUndoStop();
        this.#dialogEl.close();
    }

    disposeDiff() {
        if (!this.#diffEditor) return;
        const model = this.#diffEditor.getModel();
        this.#diffEditor.dispose();
        model?.original.dispose();
        model?.modified.dispose();
        this.#diffEditor = null;
    }
};
