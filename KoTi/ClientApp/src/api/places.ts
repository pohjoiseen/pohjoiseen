import Place from '../model/Place';
import { handleError } from './common';
import { placeToFrontend } from './mappings';

export const getPlace = async (id: number): Promise<Place> => {
    const response = await fetch(`api/Places/${id}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return placeToFrontend(await response.json());
};

export const putPlace = async (id: number, place: Place) => {
    const response = await fetch(`api/Places/${id}`, {
        method: 'PUT',
        body: JSON.stringify(place),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const postPlace = async (place: Place): Promise<Place> => {
    const response = await fetch(`api/Places`, {
        method: 'POST',
        body: JSON.stringify(place),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return placeToFrontend(await response.json());
}

export const deletePlace = async (id: number): Promise<void> => {
    const response = await fetch(`api/Places/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}
