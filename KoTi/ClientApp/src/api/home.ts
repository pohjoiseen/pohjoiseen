import { handleError } from './common';
import Stats from '../model/Stats';

export const getStats: () => Promise<Stats> = async () => {
    const response = await fetch('api/Home/Stats');
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};
