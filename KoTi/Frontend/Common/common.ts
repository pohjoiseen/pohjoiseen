import htmx from 'htmx.org';

export const $id = (id: string) => document.getElementById(id);
(window as any).$id = $id;

// remember ephemeral state of some UI elements and restore it on page load or htmx update

function saveAndRestoreTabState(tabsEl: Element) {
    const key = window.location.pathname + '#tabs#' + (tabsEl as HTMLElement).dataset.rememberState;
    const savedValue = window.localStorage.getItem(key);
    if (savedValue) {
        try {
            (tabsEl.querySelector(`& > div > input[type=radio][id=${savedValue}]`) as HTMLInputElement).checked = true;
        } catch (e) {
            window.localStorage.removeItem(key);
        }
    }
    tabsEl.querySelectorAll('& > div > input[type=radio]').forEach((radioEl: Element) =>
        radioEl.addEventListener('change', () => {
            if ((radioEl as HTMLInputElement).checked) window.localStorage.setItem(key, radioEl.id);
        }
    ));
}

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
    handleElements(el as HTMLElement, '.koti-tabs[data-remember-state]', saveAndRestoreTabState);
    handleElements(el as HTMLElement, '.list[data-remember-state]', saveAndRestoreListState);
});
