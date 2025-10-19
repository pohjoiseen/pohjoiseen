import Region from '../model/Region';
import Area from '../model/Area';
import { handleError } from './common';
import { areaToFrontend } from './mappings';

export const getRegion = async (id: number): Promise<Region> => {
    const response = await fetch(`api/Regions/${id}`, {
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

export const putRegion = async (id: number, region: Region) => {
    const response = await fetch(`api/Regions/${id}`, {
        method: 'PUT',
        body: JSON.stringify(region),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const postRegion = async (region: Region): Promise<Region> => {
    const response = await fetch(`api/Regions`, {
        method: 'POST',
        body: JSON.stringify(region),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return await response.json();
}

export const deleteRegion = async (id: number): Promise<void> => {
    const response = await fetch(`api/Regions/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}

export const getAreasForRegion: (id: number) => Promise<Area[]> = async (id) => {
    const response = await fetch(`api/Regions/${id}/Areas`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    return result.map(areaToFrontend);
};

export const reorderAreasInRegion: (id: number, ids: number[]) => Promise<void> = async (id, ids) => {
    const response = await fetch(`api/Regions/${id}/Areas/Order`, {
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
