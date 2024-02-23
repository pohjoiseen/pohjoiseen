export interface UploadResult {
    hash: string;
    pictureUrl: string;
    thumbnailUrl: string;
    detailsUrl: string;
    existingId: number | null;
}

export const uploadPicture = async (picture: File, onProgress: (percentage: number) => void) => {
    const originalName = picture.name;
  
    // hash image data on client side
    const pictureAsArrayBuffer = await picture.arrayBuffer();
    const rawHash = await crypto.subtle.digest("SHA-1", pictureAsArrayBuffer);
    const hash = Array.from(new Uint8Array(rawHash))
        .map((b) => b.toString(16).padStart(2, '0'))
        .join('');
    
    // use XMLHttpRequest instead of fetch here to get progress
    const request = new XMLHttpRequest();
    request.responseType = 'json';
    request.upload.addEventListener('progress', (e) => {
        onProgress(e.loaded / e.total * 100);
    });
    
    request.open('POST', `/api/Pictures/Upload/${hash}/${originalName}`);

    await new Promise<void>((resolve) => {
        request.addEventListener('readystatechange', () => {
            if (request.readyState === XMLHttpRequest.DONE) {
                resolve();
            }
        });
        request.send(picture);
    });

    const response = request.response;

    if (request.status !== 200) {
        console.error(`${request.status} ${request.statusText}`, response);
        if (response && response.title) {
            throw new Error(response.title);
        } else if (request.status) {
            throw new Error(`${request.status} ${request.statusText}`);
        } else {
            throw new Error('Network error');
        }
    }
    
    return response as UploadResult;
};
    
