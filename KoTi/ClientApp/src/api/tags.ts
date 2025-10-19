import { handleError } from './common';
import Tag from '../model/Tag';
import qs from 'qs';

export const getTags: (q: string) => Promise<Tag[]> = async (q) => {
    const response = await fetch('api/Tags?' + qs.stringify({ q }));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const postTag = async (tag: Tag): Promise<Tag> => {
    const response = await fetch(`api/Tags`, {
        method: 'POST',
        body: JSON.stringify(tag),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return await response.json();
};
