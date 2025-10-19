import Country from '../model/Country';
import Region from '../model/Region';
import { handleError } from './common';

export const getCountries: () => Promise<Country[]> = async () => {
    const response = await fetch('api/Countries');
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const getCountry = async (id: number): Promise<Country> => {
    const response = await fetch(`api/Countries/${id}`, {
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

export const getRegionsForCountry: (id: number) => Promise<Region[]> = async (id) => {
    const response = await fetch(`api/Countries/${id}/Regions`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const reorderRegionsInCountry: (id: number, ids: number[]) => Promise<void> = async (id, ids) => {
    const response = await fetch(`api/Countries/${id}/Regions/Order`, {
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