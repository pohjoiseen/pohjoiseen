interface Picture {
    id: number | null;
    filename: string;
    url: string;
    uploadedAt: Date | null;
    
    placeId: number | null;
    
    width: number;
    height: number;
    size: number;

    title: string;
    description: string;
    
    // EXIF
    photographedAt: Date;  // CreateDate from EXIF
    camera: string | null;  // Model from EXIF
    lens: string | null;  // Lens from EXIF
    lat: number | null;  // latitude from EXIF, if available
    lng: number | null;  // longitude from EXIF, if available
    
    blob?: Blob | undefined;  // used before the file is uploaded
    upload?: number | undefined;  // used during file upload, >0 = in progress, bytes uploaded; <0 = status
}

export enum PictureUploadResult {
    UPLOADED = -1,
    FAILED = -2,
    DUPLICATE = -3
}

export const PICTURE_SIZE_THUMBNAIL = 125;
export const PICTURE_SIZE_DETAILS = 400;

export default Picture;