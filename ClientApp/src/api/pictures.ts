import Picture from '../model/Picture';
import { handleError } from './common';

// normalize a picture received from server for frontend in particular by converting
// date fields to actual Date objects
// TODO: adding a 'Z' here gives us correct time zones but is hacky, we don't
// have to explicitly do that when posting pictures, but they end up stored without 'Z'
const toFrontend = (picture: any): Picture => {
    if (picture.uploadedAt) {
        picture.uploadedAt = new Date(picture.uploadedAt + 'Z');
    }
    if (picture.photographedAt) {
        picture.photographedAt = new Date(picture.photographedAt + 'Z');
    }
    return picture as Picture;
}

export const getPictures = async (): Promise<Picture[]> => {
    const response = await fetch('api/Pictures');
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return (await response.json()).map(toFrontend);
};

export const getPicture = async (id: number): Promise<Picture> => {
    const response = await fetch(`api/Pictures/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return toFrontend(await response.json());
}

export const putPicture = async (id: number, picture: Picture) => {
    const response = await fetch(`api/Pictures/${id}`, {
        method: 'PUT',
        body: JSON.stringify(picture),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const postPicture = async (picture: Picture): Promise<Picture> => {
    const response = await fetch(`api/Pictures`, {
        method: 'POST',
        body: JSON.stringify(picture),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return toFrontend(await response.json());
}

export const deletePicture = async (id: number): Promise<void> => {
    const response = await fetch(`api/Pictures/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}
