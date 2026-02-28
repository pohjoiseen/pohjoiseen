import './Common/common.ts';
import BookPickerElement from './WebComponents/Books/koti-book-picker.ts';
import DndOrderElement from './WebComponents/Common/koti-dnd-order.ts';
import DropdownElement from './WebComponents/Common/koti-dropdown.ts';
import ExploreStatusPickerElement from './WebComponents/Common/koti-explore-status-picker.ts';
import GeoIconPickerElement from './WebComponents/Common/koti-geo-icon-picker.ts';
import LocationPickerElement from './WebComponents/Common/koti-location-picker.ts';
import PopoverElement from './WebComponents/Common/koti-popover.ts';
import RatingElement from './WebComponents/Common/koti-rating.ts';
import TabsElement from './WebComponents/Common/koti-tabs.ts';
import ContentEditorElement from './WebComponents/ContentEditor/koti-content-editor.ts';
import ContentItemElement from './WebComponents/ContentEditor/koti-content-item.ts';
import FullscreenPictureElement from './WebComponents/Pictures/koti-fullscreen-picture.ts';
import PictureItemElement from './WebComponents/Pictures/koti-picture.ts';
import PicturePickerElement from './WebComponents/Pictures/koti-picture-picker.ts';
import PictureUploadModalElement from './WebComponents/Pictures/koti-picture-upload-modal.ts';
import PictureUploaderElement from './WebComponents/Pictures/koti-picture-uploader.ts';
import GeoExternalLinksElement from './WebComponents/Posts/koti-geo-external-links.ts';
import PostCoatsOfArmsElement from './WebComponents/Posts/koti-post-coats-of-arms.ts';
import PostGeoElement from './WebComponents/Posts/koti-post-geo.ts';
import PostTitlePictureElement from './WebComponents/Posts/koti-post-title-picture.ts';

// register web components

// common
customElements.define('koti-dnd-order', DndOrderElement);
customElements.define('koti-dropdown', DropdownElement);
customElements.define('koti-explore-status-picker', ExploreStatusPickerElement);
customElements.define('koti-geo-icon-picker', GeoIconPickerElement);
customElements.define('koti-location-picker', LocationPickerElement);
customElements.define('koti-popover', PopoverElement);
customElements.define('koti-rating', RatingElement);
customElements.define('koti-tabs', TabsElement);

// content editor
customElements.define('koti-content-editor', ContentEditorElement);
customElements.define('koti-content-item', ContentItemElement);

// books
customElements.define('koti-book-picker', BookPickerElement);

// pictures
customElements.define('koti-fullscreen-picture', FullscreenPictureElement);
customElements.define('koti-picture', PictureItemElement);
customElements.define('koti-picture-picker', PicturePickerElement);
customElements.define('koti-picture-upload-modal', PictureUploadModalElement);
customElements.define('koti-picture-uploader', PictureUploaderElement);

// posts
customElements.define('koti-geo-external-links', GeoExternalLinksElement);
customElements.define('koti-post-coats-of-arms', PostCoatsOfArmsElement);
customElements.define('koti-post-geo', PostGeoElement);
customElements.define('koti-post-title-picture', PostTitlePictureElement);

