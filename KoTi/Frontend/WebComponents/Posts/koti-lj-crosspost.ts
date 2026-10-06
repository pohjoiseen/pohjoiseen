//
// <koti-lj-crosspost>: LiveJournal crosspost dialog.  Shows post HTML converted for LJ in a Monaco editor, regenerates it
// on the server when settings change, and shows its size in bytes (LJ limits posts to 64 KB).
//
import * as monaco from 'monaco-editor';
import { $id } from '../../Common/common.ts';

const LJ_MAX_BYTES = 65535;

export default class LJCrosspostElement extends HTMLElement {
    #editor: monaco.editor.IStandaloneCodeEditor = null!;
    #formEl: HTMLFormElement = null!;
    #bytesEl: HTMLElement = null!;
    // last HTML from the server, if editor content differs it has been edited by hand
    #generatedHtml = '';
    // settings for which #generatedHtml was generated, to go back to if regenerating is cancelled
    #generatedSettings: Map<string, string> = new Map();
    #requestCounter = 0;

    connectedCallback() {
        const dialogEl = this.querySelector('dialog')!;
        this.#formEl = this.querySelector('form')!;
        this.#bytesEl = this.querySelector('.lj-crosspost-bytes')!;
        this.#generatedHtml = ($id(this.getAttribute('initial-html-id')!) as HTMLTextAreaElement).value;
        this.#generatedSettings = this.getSettings();

        // HTML language features (completion, validation etc.) run in a web worker, which we don't set up;
        // syntax highlighting works without it
        monaco.html.htmlDefaults.setModeConfiguration({});

        this.#editor = monaco.editor.create(this.querySelector('.lj-crosspost-editor')!, {
            value: this.#generatedHtml,
            language: 'html',
            wordWrap: 'on',
            unicodeHighlight: {
                ambiguousCharacters: false
            },
            lineNumbers: 'off',
            wordBasedSuggestions: 'off',
            // no multiple cursors, too easy to add by accident (Alt-click etc.), never used
            multiCursorLimit: 1,
            automaticLayout: true,
            fontFamily: 'JetBrains Mono, monospace'
        });
        this.#editor.onDidChangeModelContent(() => this.updateByteCount());
        this.updateByteCount();

        this.#formEl.addEventListener('change', () => this.regenerate());
        this.#formEl.addEventListener('submit', e => e.preventDefault());

        this.querySelector('.copy-btn')!.addEventListener('click', async () => {
            try {
                await navigator.clipboard.writeText(this.#editor.getValue());
            } catch {
                // clipboard API is available only on HTTPS or localhost, at least select everything for Ctrl-C
                this.#editor.setSelection(this.#editor.getModel()!.getFullModelRange());
                this.#editor.focus();
            }
        });
        this.querySelector('.cancel-btn')!.addEventListener('click', () => dialogEl.close());
        // dialog is loaded anew each time with default settings, so just clean up
        dialogEl.addEventListener('close', () => {
            this.#editor.dispose();
            this.remove();
        });
    }

    getSettings() {
        const settings = new Map<string, string>();
        for (const el of this.#formEl.elements) {
            if (el instanceof HTMLInputElement && el.type === 'checkbox') {
                settings.set(el.name, el.checked ? 'true' : 'false');
            } else if (el instanceof HTMLSelectElement) {
                settings.set(el.name, el.value);
            }
        }
        return settings;
    }

    setSettings(settings: Map<string, string>) {
        for (const el of this.#formEl.elements) {
            if (el instanceof HTMLInputElement && el.type === 'checkbox') {
                el.checked = settings.get(el.name) === 'true';
            } else if (el instanceof HTMLSelectElement) {
                el.value = settings.get(el.name) ?? el.value;
            }
        }
    }

    isEdited() {
        return this.#editor.getValue() !== this.#generatedHtml;
    }

    async regenerate() {
        if (this.isEdited() && !confirm('HTML has been edited by hand.  Overwrite the edits?')) {
            this.setSettings(this.#generatedSettings);
            return;
        }

        const settings = this.getSettings();
        const requestNumber = ++this.#requestCounter;
        this.#formEl.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(this.getAttribute('html-url') + '?' + new URLSearchParams([...settings]));
            if (!response.ok) {
                throw new Error(`${response.status} ${response.statusText}`);
            }
            const html = await response.text();
            // ignore responses to outdated requests, if settings were changed quickly
            if (requestNumber !== this.#requestCounter) {
                return;
            }
            this.#generatedHtml = html;
            this.#generatedSettings = settings;
            this.#editor.setValue(html);
        } catch (e) {
            if (requestNumber === this.#requestCounter) {
                this.setSettings(this.#generatedSettings);
            }
            alert('Failed to generate HTML: ' + e);
        } finally {
            if (requestNumber === this.#requestCounter) {
                this.#formEl.removeAttribute('aria-busy');
            }
        }
    }

    updateByteCount() {
        const bytes = new TextEncoder().encode(this.#editor.getValue()).length;
        this.#bytesEl.textContent = `${bytes.toLocaleString('en-US')} bytes`;
        this.#bytesEl.classList.toggle('too-long', bytes > LJ_MAX_BYTES);
        this.#bytesEl.title = `LJ limit is ${LJ_MAX_BYTES.toLocaleString('en-US')} bytes` +
            (bytes > LJ_MAX_BYTES ? `, ${(bytes - LJ_MAX_BYTES).toLocaleString('en-US')} too many` : '');
    }
};
