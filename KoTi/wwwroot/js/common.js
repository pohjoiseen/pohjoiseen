function $id(id) { return document.getElementById(id); }

// remember ephemeral state of some UI elements and restore it on page load or htmx update

function saveAndRestoreTabState(tabsEl) {
    const key = window.location.pathname + '#tabs#' + tabsEl.dataset.rememberState;
    const savedValue = window.localStorage.getItem(key);
    if (savedValue) {
        try {
            tabsEl.querySelector(`& > div > input[type=radio][id=${savedValue}]`).checked = true;
        } catch (e) {
            window.localStorage.removeItem(key);
        }
    }
    tabsEl.querySelectorAll('& > div > input[type=radio]').forEach(radioEl =>
        radioEl.addEventListener('change', () => {
                if (radioEl.checked) window.localStorage.setItem(key, radioEl.id);
            }
        ));
}

function saveAndRestoreListState(pictureListEl) {
    const key = window.location.pathname + '#list#' + pictureListEl.dataset.rememberState + '#scroll';
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

function handleElements(rootEl, selector, callback) {
    rootEl.querySelectorAll(selector).forEach(callback);
    if (rootEl.matches(selector)) callback(rootEl);
}

htmx.onLoad((el) => {
    handleElements(el, '.koti-tabs[data-remember-state]', saveAndRestoreTabState);
    handleElements(el, '.picture-list[data-remember-state]', saveAndRestoreListState);
    handleElements(el, '.content-list[data-remember-state]', saveAndRestoreListState);
});
