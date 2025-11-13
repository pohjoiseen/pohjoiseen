import qs from 'qs';
import { handleError } from './common';
import ListWithTotal from '../model/ListWithTotal';
import Redirect from '../model/Redirect';

export const getRedirects = async (limit: number, offset: number): Promise<ListWithTotal<Redirect>> => {
    const params: { [key: string]: any } = {};
    if (limit) {
        params['limit'] = limit;
    }
    if (offset) {
        params['offset'] = offset;
    }
    const response = await fetch('api/Redirects?' + qs.stringify(params));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const postRedirect = async (redirect: Redirect): Promise<void> => {
    const response = await fetch(`api/Redirects`, {
        method: 'POST',
        body: JSON.stringify(redirect),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}

export const deleteRedirect = async (id: number): Promise<void> => {
    const response = await fetch(`api/Redirects/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}