import qs from 'qs';
import { handleError } from './common';
import ListWithTotal from '../model/ListWithTotal';
import Article from '../model/Article';
import { articleToFrontend } from './mappings';

export const getArticles = async (limit: number, offset: number): Promise<ListWithTotal<Article>> => {
    const params: { [key: string]: any } = {};
    if (limit) {
        params['limit'] = limit;
    }
    if (offset) {
        params['offset'] = offset;
    }
    const response = await fetch('api/Articles?' + qs.stringify(params));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    const result = await response.json();
    result.data = result.data.map(articleToFrontend);
    return result;
};

export const getArticle = async (id: number): Promise<Article> => {
    const response = await fetch(`api/Articles/${id}`);
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return articleToFrontend(await response.json());
}
