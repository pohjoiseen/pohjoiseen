import qs from 'qs';
import Picture from '../model/Picture';
import { handleError } from './common';
import ListWithTotal from '../model/ListWithTotal';
import { pictureToFrontend } from './mappings';

export interface GetPicturesOptions {
    setId?: number;
    placeId?: number;
    areaId?: number;
    regionId?: number;
    countryId?: number;
    tagIds?: number[];
    minRating?: number;
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
    if (options.regionId) {
        params['regionId'] = options.regionId;
    }
    if (options.countryId) {
        params['countryId'] = options.countryId;
    }
    if (options.tagIds) {
        params['tagIds'] = options.tagIds;
    }
    if (options.minRating) {
        params['minRating'] = options.minRating;
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
};

export const deletePicture = async (id: number): Promise<void> => {
    const response = await fetch(`api/Pictures/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const deletePictures = async (ids: number[]): Promise<void> => {
    const response = await fetch('api/Pictures', {
        method: 'DELETE',
        body: JSON.stringify({ ids }),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};
