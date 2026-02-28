/**
 * <koti-dropdown>: basic dropdown menu using built-in popovers.  Cannot be updated once created.
 * Expected markup:
 * <koti-dropdown selected="Label for toggle button">
 *     <koti-dropdown-option value="value">This will emit an event</koti-dropdown-value>
 *     <koti-dropdown-option href="http://example.org">This will be just a link</koti-dropdown-value>
 *     <koti-dropdown-option href="http://example.org" muted>Muted attribute makes text muted and italic</koti-dropdown-value>    
 * </koti-dropdown>
 */
export default class KotiDropdownElement extends HTMLElement {
    #initialized = false;
    #button: HTMLButtonElement = null!;
    #dropdown: HTMLUListElement = null!;

    constructor() {
        super();
    }

    connectedCallback() {
        if (this.#initialized) return;
        this.#initialized = true;

        this.#button = document.createElement('button');
        this.#button.classList.add('koti-btn', 'dropdown-toggle');
        this.#button.textContent = this.getAttribute('selected') ?? '';
        this.#button.type = 'button';

        const chevron = document.createElement('i');
        chevron.classList.add('bi', 'bi-chevron-down');
        this.#button.appendChild(chevron);
        this.appendChild(this.#button);

        this.#dropdown = document.createElement('ul');
        this.#dropdown.popover = 'auto';
        this.appendChild(this.#dropdown);
        this.#button.popoverTargetElement = this.#dropdown;

        const optionEls = this.querySelectorAll('koti-dropdown-option');
        for (const optionEl of optionEls) {
            const value = optionEl.getAttribute('value') ?? optionEl.textContent;
            const href = optionEl.getAttribute('href');
            const label = optionEl.textContent;
            optionEl.remove();

            const li = document.createElement('li');
            if (optionEl.hasAttribute('muted')) {
                li.classList.add('muted');
            }
            
            if (href) {
                const a = document.createElement('a');
                a.href = href;
                a.textContent = label;
                li.appendChild(a);
            } else {
                const button = document.createElement('button');
                button.type = 'button';
                button.textContent = label;
                button.addEventListener('click', () => {
                    this.dispatchEvent(new CustomEvent('koti-dropdown:select', {detail: value}));
                });
                li.appendChild(button);
            }
            this.#dropdown.appendChild(li);
        }
    }
}