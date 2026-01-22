import htmx from 'htmx.org';

export const $id = (id: string) => document.getElementById(id);
(window as any).$id = $id;

export const generateId = () =>  Math.random().toString(36).substring(2, 15);

// reindex name, id, for atributes on <input>, <textarea>, <label> and also <koti-location-picker>.
// This is a bit hacky, especially since we might be changing these attributes from under a few other web components,
// but should work fine.
export const reindexElements = (parentEl: HTMLElement, prefix: string, index: number) => {
    parentEl.querySelectorAll('input, textarea, label, koti-location-picker').forEach((el) => {
        const name = el.getAttribute('name');
        if (name) el.setAttribute('name', name.replace(new RegExp(`^${prefix}\\[\\d+]`), `${prefix}[${index}]`));
        const id = el.getAttribute('id');
        if (id) el.setAttribute('id', id.replace(/-\d+$/, '-' + index));
        const labelFor = el.getAttribute('for');
        if (labelFor) el.setAttribute('for', labelFor.replace(/-\d+$/, '-' + index));
    });
};

// remember ephemeral state of some UI elements and restore it on page load or htmx update

function saveAndRestoreListState(pictureListEl: Element) {
    const key = window.location.pathname + '#list#' + (pictureListEl as HTMLElement).dataset.rememberState + '#scroll';
    const savedValue = window.localStorage.getItem(key);
    if (savedValue) {
        try {
            pictureListEl.scrollTop = parseInt(savedValue);
        } catch (e) {
            window.localStorage.removeItem(key);
        }
    }
    pictureListEl.addEventListener('scroll', () => {
        window.localStorage.setItem(key, pictureListEl.scrollTop.toString());
    });
}

function handleElements(rootEl: HTMLElement, selector: string, callback: (el: Element) => void) {
    rootEl.querySelectorAll(selector).forEach(callback);
    if (rootEl.matches(selector)) callback(rootEl);
}

htmx.onLoad((el) => {
    handleElements(el as HTMLElement, '.list[data-remember-state]', saveAndRestoreListState);
});

// custom event to open any dialog with showModal(), useful for HX-Trigger
document.addEventListener('dialogopenmodal', (e) => {
    if (e.target instanceof HTMLDialogElement) e.target.showModal(); 
});