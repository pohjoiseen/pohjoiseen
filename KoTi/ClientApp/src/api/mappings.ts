import Area from '../model/Area';
import Picture from '../model/Picture';
import Place from '../model/Place';
import Post from '../model/Post';
import Article from '../model/Article';

// TODO: do we need 'Z' or not?
export const pictureToFrontend = (picture: any): Picture => {
    if (picture.uploadedAt) {
        picture.uploadedAt = new Date(picture.uploadedAt.endsWith('Z')
            ? picture.uploadedAt : picture.uploadedAt + 'Z');
    }
    if (picture.photographedAt) {
        picture.photographedAt = new Date(picture.photographedAt.endsWith('Z')
            ? picture.photographedAt : picture.photographedAt + 'Z');
    }
    picture.updatedAt = new Date(picture.updatedAt + 'Z');
    return picture as Picture;
}

export const placeToFrontend = (place: any): Place => {
    place.updatedAt = new Date(place.updatedAt + 'Z');
    return place as Place;
};

export const areaToFrontend = (area: any): Area => {
    area.updatedAt = new Date(area.updatedAt + 'Z');
    return area as Area;
};

export const postToFrontend = (post: any): Post => {
    post.date = new Date(post.date + 'Z');
    post.updatedAt = new Date(post.updatedAt + 'Z');
    return post as Post;
};

export const articleToFrontend = (article: any): Article => {
    article.updatedAt = new Date(article.updatedAt + 'Z');
    return article as Article;
};