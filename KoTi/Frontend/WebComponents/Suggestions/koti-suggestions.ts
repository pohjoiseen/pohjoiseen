//
// <koti-suggestions>: suggested edits to the content on the edit page (Suggestions tab).  Children are
// server-rendered .suggestion cards with the old and the new text in data attributes.  A suggestion applies while
// its old text occurs in the editor exactly once.  Accepting replaces the text in the editor and nothing else;
// ids of accepted suggestions are sent along when the content form is saved, and are recorded as accepted
// by the server only then.  Declining is saved immediately (and can be undone until the page is left).
//
import * as monaco from 'monaco-editor';
import { $id } from '../../Common/common.ts';
import type ContentEditorElement from '../ContentEditor/koti-content-editor.ts';
import { diffWords } from './diff.ts';

type State = 'pending' | 'accepted' | 'declined' | 'saved';

type Card = {
    el: HTMLElement,
    id: number,
    oldText: string,
    newText: string,
    state: State,
    // pending but old text is not in the editor exactly once
    stale: boolean
};

export default class SuggestionsElement extends HTMLElement {
    #cards: Card[] = [];
    #formEl: HTMLFormElement = null!;
    #savingIds: number[] | null = null;
    #initialized = false;

    get #editor() {
        return (document.querySelector('koti-content-editor') as ContentEditorElement).editor;
    }

    connectedCallback() {
        // fires again when <koti-tabs> wraps the tab
        if (this.#initialized) return;
        this.#initialized = true;

        this.#formEl = $id(this.getAttribute('form-id')!) as HTMLFormElement;

        for (const el of this.querySelectorAll<HTMLElement>('.suggestion')) {
            const card: Card = {
                el,
                id: parseInt(el.dataset.id!),
                oldText: el.dataset.old!.replaceAll('\r\n', '\n'),
                newText: el.dataset.new!.replaceAll('\r\n', '\n'),
                state: 'pending',
                stale: false
            };
            this.#cards.push(card);
            this.renderDiff(card);
            el.querySelector('.suggestion-diff')!.addEventListener('click', () => this.show(card));
            el.querySelector('.suggestion-accept')!.addEventListener('click', () => this.accept(card));
            el.querySelector('.suggestion-decline')!.addEventListener('click', () => this.setDeclined(card, true));
            el.querySelector('.suggestion-undo')!.addEventListener('click', () => this.undo(card));
        }
        if (!this.#cards.length) return;

        // the editor is created by another component, possibly after this one
        setTimeout(() => {
            let timeout: ReturnType<typeof setTimeout> | undefined;
            this.#editor.onDidChangeModelContent(() => {
                clearTimeout(timeout);
                timeout = setTimeout(() => this.refresh(), 300);
            });
            this.refresh();
        });

        // send accepted suggestions with the content
        this.#formEl.addEventListener('htmx:configRequest', (e: Event) => {
            const htmxEvent = e as CustomEvent<{target: HTMLElement, parameters: FormData}>;
            if (htmxEvent.detail.target === this.#formEl) {
                this.refresh();
                this.#savingIds = this.#cards.filter(c => c.state === 'accepted').map(c => c.id);
                for (const id of this.#savingIds) {
                    htmxEvent.detail.parameters.append('AcceptedSuggestionIds', id.toString());
                }
            }
        });

        this.#formEl.addEventListener('htmx:afterRequest', (e: Event) => {
            const htmxEvent = e as CustomEvent<{successful: boolean, elt: HTMLElement}>;
            if (htmxEvent.detail.elt !== this.#formEl || !this.#savingIds) return;
            if (htmxEvent.detail.successful) {
                for (const card of this.#cards) {
                    if (this.#savingIds.includes(card.id) && card.state === 'accepted') card.state = 'saved';
                }
            }
            this.#savingIds = null;
            this.render();
        });
    }

    // where the text is in the editor, if it is there exactly once
    find(text: string): monaco.Range | null {
        const matches = this.#editor.getModel()!.findMatches(text, false, false, true, null, false, 2);
        return matches.length === 1 ? matches[0]!.range : null;
    }

    replace(range: monaco.Range, text: string) {
        const editor = this.#editor, model = editor.getModel()!;
        editor.pushUndoStop();
        editor.executeEdits('koti-suggestions', [{range, text, forceMoveMarkers: true}]);
        editor.pushUndoStop();
        const start = range.getStartPosition();
        const end = model.getPositionAt(model.getOffsetAt(start) + text.replaceAll('\n', model.getEOL()).length);
        this.select(monaco.Range.fromPositions(start, end));
    }

    select(range: monaco.Range) {
        this.#editor.setSelection(range);
        this.#editor.revealRangeInCenterIfOutsideViewport(range);
    }

    show(card: Card) {
        const range = this.find(card.state === 'accepted' || card.state === 'saved' ? card.newText : card.oldText);
        if (range) this.select(range);
    }

    accept(card: Card) {
        const range = this.find(card.oldText);
        if (card.state !== 'pending' || !range) {
            this.refresh();
            return;
        }
        this.replace(range, card.newText);
        card.state = 'accepted';
        this.refresh();
    }

    undo(card: Card) {
        if (card.state === 'declined') {
            this.setDeclined(card, false);
        } else if (card.state === 'accepted') {
            // put the old text back, unless the new one has been edited since: then it stays as it is
            const range = this.find(card.newText);
            if (range) this.replace(range, card.oldText);
            card.state = 'pending';
            this.refresh();
        }
    }

    async setDeclined(card: Card, declined: boolean) {
        const buttons = card.el.querySelectorAll<HTMLButtonElement>('button');
        buttons.forEach(b => b.disabled = true);
        try {
            const response = await fetch(`/api/Suggestions/${card.id}/status`, {
                method: 'PUT',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({status: declined ? 'declined' : 'pending'})
            });
            if (!response.ok) throw new Error(`${response.status} ${await response.text()}`);
            card.state = declined ? 'declined' : 'pending';
        } catch (e) {
            alert(`Could not ${declined ? 'decline' : 'restore'} the suggestion: ${e}`);
        }
        buttons.forEach(b => b.disabled = false);
        this.refresh();
    }

    // sync states with the text in the editor
    refresh() {
        for (const card of this.#cards) {
            if (card.state === 'accepted' && !this.find(card.newText) && this.find(card.oldText)) {
                // undone in the editor
                card.state = 'pending';
            }
            card.stale = card.state === 'pending' && !this.find(card.oldText);
        }
        this.render();
    }

    render() {
        for (const card of this.#cards) {
            const {el, state, stale} = card;
            el.dataset.state = stale ? 'stale' : state;
            el.querySelector('.suggestion-state')!.textContent =
                stale ? 'Does not apply: the text to replace is not in the editor (or not just once)' :
                state === 'accepted' ? 'Accepted, not saved yet' :
                state === 'saved' ? 'Accepted' :
                state === 'declined' ? 'Declined' : '';
            (el.querySelector('.suggestion-accept') as HTMLButtonElement).hidden = state !== 'pending';
            (el.querySelector('.suggestion-accept') as HTMLButtonElement).disabled = stale;
            (el.querySelector('.suggestion-decline') as HTMLButtonElement).hidden = state !== 'pending';
            (el.querySelector('.suggestion-undo') as HTMLButtonElement).hidden = state !== 'accepted' && state !== 'declined';
        }

        // number of those still to be dealt with in the tab header
        const labelEl = document.querySelector(`label[for="${this.getAttribute('tab-label-for')}"]`);
        if (labelEl) {
            const open = this.#cards.filter(c => c.state === 'pending' || c.state === 'accepted').length;
            labelEl.textContent = open ? `Suggestions (${open})` : 'Suggestions';
        }
    }

    renderDiff(card: Card) {
        const diffEl = card.el.querySelector('.suggestion-diff')!;
        for (const part of diffWords(card.oldText, card.newText)) {
            const partEl = document.createElement(part.type === 'del' ? 'del' : part.type === 'ins' ? 'ins' : 'span');
            partEl.textContent = part.text;
            diffEl.appendChild(partEl);
        }
    }
};
