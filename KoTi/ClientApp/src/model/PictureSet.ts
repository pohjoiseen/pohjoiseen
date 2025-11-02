export default interface PictureSet {
    id: number;
    name: string;
    isPrivate: boolean;
    parentId: number | null;
    children?: PictureSet[];
    thumbnailUrls: string[];
}

export const BLOG_PICTURES_SET = 'Blog Pictures';
export const COATS_OF_ARMS_SET = 'Coats of Arms';