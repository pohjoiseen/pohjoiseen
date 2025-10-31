import Picture from "./Picture";

interface GeoPoint {
    title?: string;
    subtitle?: string;
    description?: string;
    titleImage?: string;
    anchor?: string;
    lat: number;
    lng: number;
    zoom?: number;
    icon?: string;
    maps?: string[];
    links?: Link[];
}

interface Link {
    label: string;
    path: string;
}

interface CoatOfArms {
    url: string;
    size?: number;
}

interface Post {
    id: number;
    draft: boolean;
    name: string;
    date: Date;
    contentMD: string;
    language: string;
    title: string;
    description: string;
    mini: boolean;
    titlePictureId?: string;
    titlePicture?: Picture;
    titleImageOffsetY?: number;
    titleImageInText?: boolean;
    titleImageCaption?: string;
    dateDescription?: string;
    locationDescription?: string;
    address?: string;
    publicTransport?: string;
    coatsOfArms?: CoatOfArms[];
    geo?: GeoPoint[];
    updatedAt: Date;
}

export default Post;