import Place from '../model/Place';
import Area from '../model/Area';
import { handleError } from './common';
import { areaToFrontend, placeToFrontend } from './mappings';

export const getArea = async (id: number): Promise<Area> => {
    const response = await fetch(`api/Areas/${id}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return areaToFrontend(await response.json());
};

export const putArea = async (id: number, area: Area) => {
    const response = await fetch(`api/Areas/${id}`, {
        method: 'PUT',
        body: JSON.stringify(area),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const postArea = async (area: Area): Promise<Area> => {
    const response = await fetch(`api/Areas`, {
        method: 'POST',
        body: JSON.stringify(area),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return areaToFrontend(await response.json());
}

export const deleteArea = async (id: number): Promise<void> => {
    const response = await fetch(`api/Areas/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}

export const getPlacesForArea: (id: number) => Promise<Place[]> = async (id) => {
    const response = await fetch(`api/Areas/${id}/Places`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    return result.map(placeToFrontend);
};

export const reorderPlacesInArea: (id: number, ids: number[]) => Promise<void> = async (id, ids) => {
    const response = await fetch(`api/Areas/${id}/Places/Order`, {
        method: 'PUT',
        body: JSON.stringify({ ids }),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};
