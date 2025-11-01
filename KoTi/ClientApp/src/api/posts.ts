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

export const putPost = async (id: number, post: Post) => {
    const response = await fetch(`api/Posts/${id}`, {
        method: 'PUT',
        body: JSON.stringify(post),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
};

export const postPost = async (post: Post): Promise<Post> => {
    const response = await fetch(`api/Posts`, {
        method: 'POST',
        body: JSON.stringify(post),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (!response.ok || response.status !== 201) {
        await handleError(response);
    }
    return await response.json();
}

export const deletePost = async (id: number): Promise<void> => {
    const response = await fetch(`api/Posts/${id}`, {
        method: 'DELETE',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}