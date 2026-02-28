// may be somewhat out of date now

export interface Picture {
    id: number | null;
    filename: string;
    hash?: string;
    url: string;
    thumbnailUrl: string;
    detailsUrl: string;
    uploadedAt: Date | null;
    
    setId: number | null;
    setName?: number | null;
    
    width: number;
    height: number;
    size: number;

    title: string;
    description: string;
    
    isPrivate: boolean;
    rating: number;
    
    // EXIF
    photographedAt: Date;  // CreateDate from EXIF
    camera: string | null;  // Model from EXIF
    lens: string | null;  // Lens from EXIF
    lat: number | null;  // latitude from EXIF, if available
    lng: number | null;  // longitude from EXIF, if available
    
    updatedAt: Date;
}

export interface PictureSet {
    id: number;
    name: string;
    isPrivate: boolean;
    parentId: number | null;
    children?: PictureSet[];
    thumbnailUrls: string[];
}

export interface GeoPoint {
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

export interface Link {
    label: string;
    path: string;
}

export interface CoatOfArms {
    url: string;
    size?: number;
}

export interface Post {
    id: number;
    draft: boolean;
    name: string;
    date: Date;
    contentMD: string;
    language: string;
    title: string;
    description: string;
    mini: boolean;
    titlePictureId?: number;
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

export interface Article {
    id: number;
    draft: boolean;
    name: string;
    title: string;
    contentMD: string;
    language: string;
    updatedAt: Date;
}


