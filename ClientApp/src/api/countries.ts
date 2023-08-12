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

export const getRegionsForCountry: (id: number) => Promise<Region[]> = async (id) => {
    const response = await fetch(`api/Countries/${id}/Regions`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};
