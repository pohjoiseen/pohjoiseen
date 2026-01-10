// This is a small subset of KoTi 3.0 API calls that is still used in htmx frontend.
// If I ever migrate completely to htmx, these should go to the same controller on the backend.

import type Picture from '../model/Picture.ts';
import type Post from '../model/Post.ts';
import type Article from '../model/Article.ts';

const handleError = async (response: Response) => {
    const body = await response.text();
    let json = null;
    try {
        json = JSON.parse(body);
    } catch (e) {}

    if (json) {
        console.error(`${response.status} ${response.statusText}`, json);
        if (json.title) {
            throw new Error(json.title);
        } else {
            throw new ServerError(response, body, json);
        }
    } else {
        console.error(`${response.status} ${response.statusText}`, body);
        throw new ServerError(response, body);
    }
};

class ServerError extends Error {
    response: Response;
    body: string;
    json: any;
    constructor(response: Response, body: string, json: any = null) {
        super();
        this.message = `${response.status} ${response.statusText}, see console`;
        this.response = response;
        this.body = body;
        this.json = json;
    }
}

const pictureToFrontend = (picture: any): Picture => {
    if (picture.uploadedAt) {
        picture.uploadedAt = new Date(picture.uploadedAt.endsWith('Z')
            ? picture.uploadedAt : picture.uploadedAt + 'Z');
    }
    if (picture.photographedAt) {
        picture.photographedAt = new Date(picture.photographedAt.endsWith('Z')
            ? picture.photographedAt : picture.photographedAt + 'Z');
    }
    picture.updatedAt = new Date(picture.updatedAt + 'Z');
    return picture;
};

const postToFrontend = (post: any): Post => {
    post.date = new Date(post.date + 'Z');
    post.updatedAt = new Date(post.updatedAt + 'Z');
    return post;
};

const articleToFrontend = (article: any): Article => {
    article.updatedAt = new Date(article.updatedAt + 'Z');
    return article;
};

export async function apiGetPicture(id: number) {
    const response = await fetch(`/api/Pictures/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return pictureToFrontend(await response.json());
}

export async function apiGetPost(id: number) {
    const response = await fetch(`/api/Posts/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return postToFrontend(await response.json());
}

export async function apiGetArticle(id: number) {
    const response = await fetch(`/api/Articles/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return articleToFrontend(await response.json());
}

export async function apiEnsureWebSizes (id: number) {
    const response = await fetch(`/api/Pictures/${id}/WebSizes`, {
        method: 'POST',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}
