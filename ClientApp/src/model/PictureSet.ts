export default interface PictureSet {
    id: number;
    name: string;
    isPrivate: boolean;
    parentId: number | null;
    children?: PictureSet[];
}