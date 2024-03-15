import qs from 'qs';
import Picture from '../model/Picture';
import { handleError } from './common';
import ListWithTotal from '../model/ListWithTotal';
import { pictureToFrontend } from './mappings';

export interface GetPicturesOptions {
    setId?: number;
    placeId?: number;
    areaId?: number;
    limit?: number;
    offset?: number;
}

export const getPictures = async (options: GetPicturesOptions): Promise<ListWithTotal<Picture>> => {
    const params: { [key: string]: any } = {};
    if (typeof options.setId !== 'undefined') {
        params['setId'] = options.setId;
    }
    if (options.placeId) {
        params['placeId'] = options.placeId;
    }
    if (options.areaId) {
        params['areaId'] = options.areaId;
    }
    if (options.limit) {
        params['limit'] = options.limit;
    }
    if (options.offset) {
        params['offset'] = options.offset;
    }
    const response = await fetch('api/Pictures?' + qs.stringify(params));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    result.data = result.data.map(pictureToFrontend);
    return result;
};

export const getPicture = async (id: number): Promise<Picture> => {
    const response = await fetch(`api/Pictures/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return pictureToFrontend(await response.json());
}

export const getPicturesForPlace = async (placeId: number, limit?: number): Promise<Picture[]> => {
    const response = await fetch('api/Pictures/ForPlace/' + placeId + '?limit=' + (limit || 0));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    return result.map(pictureToFrontend);
};

export const getPicturesForArea = async (areaId: number, limit?: number): Promise<Picture[]> => {
    const response = await fetch('api/Pictures/ForArea/' + areaId + '?limit=' + (limit || 0));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    return result.map(pictureToFrontend);
};

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
    return pictureToFrontend(await response.json());
}

export const deletePicture = async (id: number): Promise<void> => {
    const response = await fetch(`api/Pictures/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}
