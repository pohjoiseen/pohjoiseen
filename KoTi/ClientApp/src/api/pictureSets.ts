import qs from 'qs';
import PictureSet from '../model/PictureSet';
import { handleError } from './common';

export const getPictureSets = async (): Promise<PictureSet[]> => {
    const response = await fetch(`api/PictureSets`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const getPictureSet = async (id: number): Promise<PictureSet> => {
    const response = await fetch(`api/PictureSets/${id}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const getPictureSetByName = async (name: string): Promise<PictureSet | null> => {
    const response = await fetch(`api/PictureSets/ByName?${qs.stringify({ name })}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const putPictureSet = async (id: number, pictureSet: PictureSet) => {
    const response = await fetch(`api/PictureSets/${id}`, {
        method: 'PUT',
        body: JSON.stringify(pictureSet),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const postPictureSet = async (pictureSet: PictureSet): Promise<PictureSet> => {
    const response = await fetch(`api/PictureSets`, {
        method: 'POST',
        body: JSON.stringify(pictureSet),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return await response.json();
};

export const deletePictureSet = async (id: number): Promise<void> => {

    const response = await fetch(`api/PictureSets/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const movePicturesToPictureSet: (id: number | null, ids: number[]) => Promise<void> = async (id, ids) => {
    const response = await fetch(id ? `api/PictureSets/${id}/MovePictures` : `api/PictureSets/RemovePicturesFromSets`, {
        method: 'POST',
        body: JSON.stringify({ ids }),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};