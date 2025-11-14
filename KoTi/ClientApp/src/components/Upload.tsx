/**
 * <Upload>: picture upload component.  Used in <PictureUpload> and <InsertUpload>.
 */
import * as React from 'react';
import { FormEvent, useCallback, useEffect, useRef, useState } from 'react';
import { Alert, } from 'reactstrap';
import exifr from 'exifr';
import Picture, { PictureUploadResult } from '../model/Picture';
import PicturesList from '../components/PicturesList';
import FileDropBox from '../components/FileDropBox';
import { uploadPicture } from '../api/picturesUpload';
import { getPicture } from '../api/pictures';
import { PicturesViewMode } from './pictureViewCommon';
import PictureFullscreen from '../components/PictureFullscreen';
import { useCreatePictureMutation } from '../data/mutations';

const UPLOAD_IDLE = -1;
const UPLOAD_ERROR = -2;

interface UploadProps {
    viewMode: PicturesViewMode;
    setId?: number;
    disableKeyboardNav?: boolean;
    onSelect?: (pictureId: number | null, insertImmediately?: boolean) => void;
}

// TODO: UI-wise functionality is broadly similar to Pictures component, but implemented largely differently
// (only in-memory state for everything, no queries and no view options via query parameters).
// Could still be possible to deduplicate it partially... 
const Upload = ({ viewMode, setId, disableKeyboardNav, onSelect }: UploadProps) => {
    const [currentFullscreen, setCurrentFullscreen] = useState(-1);
    // pictures list
    const [picturesForUpload, setPicturesForUpload] = useState<Picture[]>([]);
    const setPictureForUpload = useCallback((index: number, picture: Picture) =>
        setPicturesForUpload((pics) => {
            const newPicturesForUpload = [...pics];
            newPicturesForUpload[index] = picture;
            return newPicturesForUpload;
        }), []);
    // picture that is currently being uploaded, or UPLOAD_IDLE/ERROR 
    const [currentPictureIndex, setCurrentPictureIndex] = useState(UPLOAD_IDLE);
    // ref snapshot of pictures list
    const picturesForUploadRef = useRef<Picture[]>([]);
    picturesForUploadRef.current = picturesForUpload;
    // <input> for uploading
    const uploadInputRef = useRef<HTMLInputElement>(null);
    // error message for uploading
    const [uploadError, setUploadError] = useState('');
    const createPictureMutation = useCreatePictureMutation();

    /// enqueue for upload ///

    const addFile = useCallback(async (file: File) => {
        // only accept JPEGs and PNGs for the time being
        if (file.type !== 'image/jpeg' && file.type !== 'image/png') {
            return;
        }

        const url = URL.createObjectURL(file);

        // determine dimenstions
        const img = new Image();
        img.src = url;
        const [width, height]: [number, number] = await new Promise((resolve) => {
            img.onload = () => {
                resolve([img.naturalWidth, img.naturalHeight]);
            };
        });

        // retrieve EXIF and other metadata
        const meta = await exifr.parse(file);

        // non-uploaded picture object
        const picture: Picture = {
            id: null,
            blob: file,
            filename: file.name,
            url,
            thumbnailUrl: url,
            detailsUrl: url,
            uploadedAt: null,
            placeId: null,
            setId: setId || null,
            width,
            height,
            size: file.size,
            title: '',
            description: '',
            photographedAt: meta?.DateTimeOriginal || meta?.CreateDate || new Date(),
            camera: meta?.Model,
            lens: meta?.LensModel ? meta?.LensModel.replace(meta?.Model + ' ', '') : null,
            lat: meta?.latitude,
            lng: meta?.longitude,
            isPrivate: false,
            rating: 0,
            updatedAt: new Date(),
            tags: []
        };

        // add to picture list
        setPicturesForUpload(pics => {
            // start upload for this picture, but only if no other uploads active (nor there was an error)
            setCurrentPictureIndex(idx => idx === UPLOAD_IDLE ? pics.length : idx);
            return [...pics, picture]
        });
        // TODO: is currentPictureIndex necessary?  Formally no but might break upload
    }, [setPicturesForUpload, currentPictureIndex, setId]);  // eslint-disable-line

    /// drag-and-droppigng pictures ///

    const onDropFiles = useCallback(async (files: FileList) => {
        for (const file of Array.from(files)) {
            await addFile(file);
        }
    }, [addFile]);

    /// pasting pictures ///

    useEffect(() => {
        const onDocumentPaste = async (e: ClipboardEvent) => {
            // need to convert all items to files first, otherwise items after first one seem to get lost
            const files: (File | null)[] = [...(e.clipboardData || (e as any).originalEvent.clipboardData).items].map(i => i.getAsFile());
            for (const file of files) {
                if (file) {
                    await addFile(file);
                }
            }
        };

        document.addEventListener('paste', onDocumentPaste);
        return () => document.removeEventListener('paste', onDocumentPaste);
    }, [addFile]);
    
    /// hidden input ///

    const onInputUpload = useCallback(async (e: FormEvent<HTMLInputElement>) => {
        for (const file of Array.from(e.currentTarget.files!)) {
            await addFile(file);
        }
    }, [addFile]);

    /// actual upload, triggering on currentPictureIndex update ///

    useEffect(() => {
        if (currentPictureIndex < 0) {
            return;
        }

        (async () => {
            // references to pictures array, current picture, updating the current picture
            // via functions as underlying ref can be changed during
            const pictures = () => picturesForUploadRef.current;
            const picture = () => picturesForUploadRef.current[currentPictureIndex];
            const setPicture = (p: Picture) => setPictureForUpload(currentPictureIndex, p);

            // blob should always be non-null at this point but still
            const blob = picture().blob;
            if (blob) {
                setUploadError('');
                //console.log(`Uploading ${currentPictureIndex} (${picture().filename})`);
                try {
                    const result = await uploadPicture(blob, (percentage) => {
                        // update visual percentage only for increments of 5%
                        const roundedProgressOld = Math.round(picture().upload! / 5);
                        const roundedProgressNew = Math.round(percentage / 5);
                        if (roundedProgressOld !== roundedProgressNew) {
                            setPicture({ ...picture(), upload: percentage });
                        }
                    });

                    // intermediate phase: file uploaded to S3 (digital ocean) but not linked to database yet 
                    URL.revokeObjectURL(picture().url);
                    setPicture({
                        ...picture(),
                        hash: result.hash,
                        url: result.pictureUrl,
                        detailsUrl: result.detailsUrl,
                        thumbnailUrl: result.thumbnailUrl,
                        blob: undefined,
                        upload: PictureUploadResult.UPLOADED
                    });

                    if (result.existingId) {
                        // dup, just load existing picture instead
                        // but only first time this is encountered 
                        // TODO: should not call API directly from component
                        if (pictures().find(p => p.id === result.existingId)) {
                            setPicture({ ...picture(), upload: PictureUploadResult.DUPLICATE });
                        } else {
                            setPicture({
                                ...await getPicture(result.existingId),
                                blob: undefined,
                                upload: PictureUploadResult.DUPLICATE
                            });
                        }
                    } else {
                        // store new picture in database
                        const uploadedPicture = await createPictureMutation.mutateAsync({
                            ...picture(),
                            hash: result.hash,
                            url: result.pictureUrl,
                            detailsUrl: result.detailsUrl,
                            thumbnailUrl: result.thumbnailUrl,
                            uploadedAt: new Date()
                        });
                        setPicture({
                            ...uploadedPicture,
                            blob: undefined,
                            upload: PictureUploadResult.UPLOADED
                        });
                    }
                    //console.log(`Successfully uploaded ${currentPictureIndex} (${picture().filename})`);
                } catch (e) {
                    console.error(e);
                    setPicture({ ...picture(), upload: PictureUploadResult.FAILED });
                    setUploadError('Uploading ' + picture().filename + ': ' + ((e as any).message || 'Unknown error'));
                    //console.log(`Failed to upload ${currentPictureIndex} (${picture().filename})`);
                    setCurrentPictureIndex(UPLOAD_ERROR);  // do not upload any more pictures (until explicitly requested)
                    return;
                }
            }

            // after upload is complete, look for any other picture that is not uploaded yet or failed,
            // starting from the current one and looping from beginning; enqueue it as a next currentPictureIndex
            for (let i = currentPictureIndex + 1; i !== currentPictureIndex; i++) {
                if (i >= pictures().length) {
                    i = 0;
                    if (i === currentPictureIndex) {
                        break;
                    }
                }

                if (typeof pictures()[i].upload === 'undefined' || pictures()[i].upload === PictureUploadResult.FAILED) {
                    //console.log(`Will next upload ${i} (${pictures()[i].filename})`);
                    setCurrentPictureIndex(i);
                    return;
                }
            }
            setCurrentPictureIndex(UPLOAD_IDLE);  // uploaded everything possible
            //console.log(`Nothing to upload`);
        })();
        // TODO: is createPictureMutation necessary?  Formally yes but might break upload
    }, [currentPictureIndex, setPictureForUpload, setUploadError]);  // eslint-disable-line
    
    /// keyboard ///

    useEffect(() => {
        if (!disableKeyboardNav) {
            const handler = (e: KeyboardEvent) => {
                if (e.key === 'Escape') {
                    e.preventDefault();
                    setCurrentFullscreen(-1)
                }
                if (e.key === 'ArrowLeft' && (e.target as any).tagName !== 'INPUT' && currentFullscreen > 0) {
                    e.preventDefault();
                    setCurrentFullscreen(currentFullscreen - 1);
                }
                if (e.key === 'ArrowRight' && (e.target as any).tagName !== 'INPUT' && currentFullscreen !== -1 &&
                    currentFullscreen < picturesForUpload.length - 1) {
                    e.preventDefault();
                    setCurrentFullscreen(currentFullscreen + 1);
                }
            };
            document.addEventListener('keydown', handler);
            return () => document.removeEventListener('keydown', handler);
        }
    }, [currentFullscreen, picturesForUpload.length, disableKeyboardNav]);

    const [selectedIndex, setSelectedIndex] = useState(-1);

    const select = useCallback((index: number) => {
        setSelectedIndex(index);
        if (onSelect) {
            if (index === -1) {
                onSelect(null);
            } else {
                onSelect(picturesForUpload[index].id);
            }
        }
    }, [setSelectedIndex, onSelect, picturesForUpload]);

    const open = useCallback((index: number, ctrlKey: boolean) => {
        if (ctrlKey && onSelect) {
            onSelect(picturesForUpload[index].id, true);
        } else {
            setCurrentFullscreen(index);
        }
    }, [picturesForUpload, setCurrentFullscreen, onSelect]);

    /// render ///

    return <>
        {uploadError.length > 0 && <Alert color="danger">{uploadError}<br/>Click the failed picture to retry</Alert>}
        <PicturesList
            pictures={picturesForUpload.map(p => p.id && p.upload !== PictureUploadResult.DUPLICATE ? p.id : p)}
            selection={selectedIndex}
            currentIndex={currentFullscreen}
            viewMode={viewMode}
            onOpen={open}
            onSetSelection={(selection) => select(selection.findIndex(s => s))}
            onRetryUpload={(k) => setCurrentPictureIndex(k)}
        />
        {!picturesForUpload.length && <h4 className="text-center cursor-pointer" onClick={() => uploadInputRef.current?.click()}>
            Choose files to upload, drag or paste them here.
        </h4>}
        {currentFullscreen >= 0 && <PictureFullscreen
            picture={picturesForUpload[currentFullscreen]}
            onRetryUpload={() => setCurrentPictureIndex(currentFullscreen)}
        />}
        <FileDropBox onDrop={onDropFiles} />
        <input ref={uploadInputRef} type="file" hidden multiple onInput={onInputUpload} />
    </>;
};

export default Upload;