//
// <koti-post-title-picture>: title picture tab for post form.  Handles only showing/hiding extra fields
//
export default class PostTitlePictureElement extends HTMLElement {
    #titleImageInTextContainer: HTMLElement = null!;
    #titleImageOffsetYContainer: HTMLElement = null!;
    #titleImageCaptionContainer: HTMLElement = null!;
    #titleImageInTextInput: HTMLInputElement = null!;
    
    constructor() {
        super();
    }
    
    connectedCallback() {
        // do not re-initialize if already initialized
        if (this.#titleImageInTextContainer) return;
        
        this.#titleImageOffsetYContainer = this.querySelector('.title-image-offset-y-container') as HTMLElement;
        this.#titleImageInTextContainer = this.querySelector('.title-image-in-text-container') as HTMLElement;
        this.#titleImageCaptionContainer = this.querySelector('.title-image-caption-container') as HTMLElement;
        this.#titleImageInTextInput = this.querySelector('input[name=TitleImageInText]') as HTMLInputElement;

        // show/hide all fields depnding on whether a picture is selected or not
        document.body.addEventListener('picture-picker:loaded', (e) => {
            const customEvent = e as CustomEvent<{ componentId: string, pictureId: number | null }>;
            if (customEvent.detail.componentId === this.getAttribute('picker-component-id')) {
                this.onPictureIdChange(customEvent.detail.pictureId);
            }
        });
        
        // show either offset Y or caption field depending on image in text switch
        this.#titleImageInTextInput.addEventListener('change', () => this.onTitleImageInTextChange(this.#titleImageInTextInput.checked));
    }
    
    onPictureIdChange(pictureId: number | null) {
        if (pictureId) {
            this.#titleImageInTextContainer.removeAttribute('hidden');
            this.onTitleImageInTextChange(this.#titleImageInTextInput.checked);
        } else {
            this.#titleImageInTextContainer.setAttribute('hidden', 'hidden');
            this.#titleImageOffsetYContainer.setAttribute('hidden', 'hidden');
            this.#titleImageCaptionContainer.setAttribute('hidden', 'hidden');
        }
    }
    
    onTitleImageInTextChange(checked: boolean) {
        if (checked) {
            this.#titleImageCaptionContainer.removeAttribute('hidden');
            this.#titleImageOffsetYContainer.setAttribute('hidden', 'hidden');
        } else {
            this.#titleImageOffsetYContainer.removeAttribute('hidden');
            this.#titleImageCaptionContainer.setAttribute('hidden', 'hidden');
        }
    }
};
