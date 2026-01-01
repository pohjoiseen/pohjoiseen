// This is a small subset of KoTi 3.0 API calls that is still used in htmx frontend.
// If I ever migrate completely to htmx, these should go to the same controller on the backend.

const handleError = async (response) => {
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
    constructor(response, body, json = null) {
        super();
        this.message = `${response.status} ${response.statusText}, see console`;
        this.response = response;
        this.body = body;
        this.json = json;
    }
}

const pictureToFrontend = (picture) => {
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

const postToFrontend = (post) => {
    post.date = new Date(post.date + 'Z');
    post.updatedAt = new Date(post.updatedAt + 'Z');
    return post;
};

const articleToFrontend = (article) => {
    article.updatedAt = new Date(article.updatedAt + 'Z');
    return article;
};

async function apiGetPicture(id) {
    const response = await fetch(`/api/Pictures/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return pictureToFrontend(await response.json());
}

async function apiGetPost(id) {
    const response = await fetch(`/api/Posts/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return postToFrontend(await response.json());
}

async function apiGetArticle(id) {
    const response = await fetch(`/api/Articles/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return articleToFrontend(await response.json());
}

async function apiEnsureWebSizes (id) {
    const response = await fetch(`/api/Pictures/${id}/WebSizes`, {
        method: 'POST',
    });
    if (!response.ok || response.status !== 204) {
        await handleError(response);
    }
}
