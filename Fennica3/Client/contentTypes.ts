/**
 * JSONs passed from server side to client JS.
 */

export interface Geo {
    title?: string;
    subtitle?: string;
    description?: string;
    titleImage?: string;
    anchor?: string;
    lat: number;
    lng: number;
    zoom: number;
    icon?: string;
    maps?: string[];
    links?: {
        label?: string;
        path?: string;
    }[];
}

export interface PostDefinition {
    title: string;
    titleImage?: string;
    description?: string;
    mini?: boolean;
    geo?: Geo[];
}
