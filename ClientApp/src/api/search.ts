import qs from 'qs';
import ListWithTotal from '../model/ListWithTotal';
import { handleError } from './common';

export const SEARCHABLE_TABLES = [
    'Places', 'Areas', 'Regions', 'Countries', 'Pictures'
] as const;

export interface SearchOptions {
    q: string;
    tables?: string;
    limit?: number;
    offset?: number;
}

export interface SearchResult {
    tableName: string;
    tableId: number;
    title: string;
    text: string;
    rank: number;
}

export const search = async (options: SearchOptions): Promise<ListWithTotal<SearchResult>> => {
    const response = await fetch('api/Search?' + qs.stringify(options));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.json();
};

export const getUrl = async (tableName: string, tableId: number): Promise<string> => {
    const response = await fetch('api/Search/UrlFor?' + qs.stringify({ tableName, tableId }));
    if (!response.ok || response.status !== 200) {
        await handleError(response);
    }
    return await response.text();
};
