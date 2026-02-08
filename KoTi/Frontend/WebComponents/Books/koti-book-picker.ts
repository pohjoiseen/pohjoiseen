//
// <koti-book-picker>: book picker component interactivity (dialog-related).
// Very similar to <koti-picture-picker>, but for books instead of pictures.  Can be unified?
//
import htmx from 'htmx.org';

export default class BookPickerElement extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        const pickerEl = this.firstElementChild!;
        const dialogEl = pickerEl.querySelector('dialog')!;
        const selectButtonEl = dialogEl.querySelector('.select-btn')! as HTMLButtonElement;
        const inputEl = pickerEl.querySelector('input[type=hidden]')! as HTMLInputElement;

        // on click etc. on a book, enable/disable "Select" button and set text in it, set hidden input value
        pickerEl.addEventListener('content:select-insertable', (e) => {
            const customEvent = e as CustomEvent<{text: string}>;
            e.stopPropagation();
            if (customEvent.detail?.text) {
                const id = customEvent.detail.text.replace('book:', '');
                inputEl.value = id;
                selectButtonEl.textContent = 'Select: ' + id;
                selectButtonEl.disabled = false;
            } else {
                selectButtonEl.textContent = 'Select';
                selectButtonEl.disabled = true;
            }
        });

        // on double click etc. on a book, same and emulate click on Select button
        pickerEl.addEventListener('content:select', (e) => {
            const customEvent = e as CustomEvent<{text: string}>;
            e.stopPropagation();
            const id = customEvent.detail.text.replace('book:', '');
            inputEl.value = id;
            selectButtonEl.textContent = 'Select: ' + id;
            selectButtonEl.disabled = false;
            selectButtonEl.click();
        });

        // on click on "Select" button simply close dialog and submit picker, value should be already saved in hidden input
        selectButtonEl.addEventListener('click', () => {
            dialogEl.close();
            htmx.trigger(pickerEl, 'book-picker:reload');
        });
        // on click on "Unset" button set value to empty, close dialog and submit picker
        dialogEl.querySelector('.unset-btn')!.addEventListener('click', () => {
            inputEl.value = '';
            dialogEl.close();
            htmx.trigger(pickerEl, 'book-picker:reload');
        });
        // on click on "Cancel" button close dialog and don't do anything else
        dialogEl.querySelector('.cancel-btn')!.addEventListener('click', () => dialogEl.close());
    }
};