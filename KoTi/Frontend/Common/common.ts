import htmx from 'htmx.org';

export const $id = (id: string) => document.getElementById(id);
(window as any).$id = $id;

export const generateId = () =>  Math.random().toString(36).substring(2, 15);

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
