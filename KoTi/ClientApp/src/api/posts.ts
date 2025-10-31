import qs from 'qs';
import ListWithTotal from '../model/ListWithTotal';
import { handleError } from './common';
import { postToFrontend } from './mappings';
import Post from '../model/Post';

export const getPosts = async (limit: number, offset: number, searchQuery?: string): Promise<ListWithTotal<Post>> => {
    const params: { [key: string]: any } = {};
    if (searchQuery) {
        params['search'] = searchQuery;
    }
    if (limit) {
        params['limit'] = limit;
    }
    if (offset) {
        params['offset'] = offset;
    }
    const response = await fetch('api/Posts?' + qs.stringify(params));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    result.data = result.data.map(postToFrontend);
    return result;
};

export const getPost = async (id: number): Promise<Post> => {
    const response = await fetch(`api/Posts/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return postToFrontend(await response.json());
}
