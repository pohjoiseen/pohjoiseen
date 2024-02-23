import Picture from '../model/Picture';
import { handleError } from './common';

export const getPictures = async (): Promise<Picture[]> => {
    const response = await fetch('api/Pictures');
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const getPicture = async (id: number): Promise<Picture> => {
    const response = await fetch(`api/Pictures/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
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
    return await response.json();
}

export const deletePicture = async (id: number): Promise<void> => {
    const response = await fetch(`api/Pictures/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}
