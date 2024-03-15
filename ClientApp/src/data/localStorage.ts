import { PicturesViewMode } from '../components/pictureViewCommon';

const KEY_VIEWMODE = 'koti.viewmode';

export const getDefaultViewMode = () => {
    let viewMode = window.localStorage.getItem(KEY_VIEWMODE);
    if (!viewMode) {
        viewMode = PicturesViewMode.THUMBNAILS;
    }
    return viewMode as PicturesViewMode;
};

export const setDefaultViewMode = (viewMode: PicturesViewMode) => {
    window.localStorage.setItem(KEY_VIEWMODE, viewMode);
};
