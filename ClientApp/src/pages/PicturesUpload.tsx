import * as React from 'react';
import { useState, useEffect, useCallback, useRef, FormEvent } from 'react';
import { Alert, Button, Container } from 'reactstrap';
import exifr from 'exifr';
import { errorMessage } from '../util';
import NavBar from '../components/NavBar';
import Picture from '../model/Picture';
import PicturesList, { PicturesViewMode } from '../components/PicturesList';
import FileDropBox from '../components/FileDropBox';

const PicturesUpload = () => {
    const [viewMode, setViewMode] = useState(PicturesViewMode.THUMBNAILS);
    const [picturesForUpload, setPicturesForUpload] = useState<Picture[]>([]);
    const uploadInputRef = useRef<HTMLInputElement>(null);
    
    /// upload ///
    
    const addFile = useCallback(async (file: File) => {
        // only accept JPEGs for the time being
        if (file.type !== 'image/jpeg') {
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

        const picture: Picture = {
            id: null,
            filename: file.name,
            url,
            uploadedAt: null,
            placeId: null,
            width,
            height,
            size: file.size,
            title: '',
            description: '',
            photographedAt: meta.DateTimeOriginal || meta.CreateDate,
            camera: meta.Model,
            lens: meta.LensModel ? meta.LensModel.replace(meta.Model + ' ', '') : null,
            lat: meta.latitude,
            lng: meta.longitude
        };
        
        setPicturesForUpload(pics => [...pics, picture]);
    }, [setPicturesForUpload]);
    
    /// uploading pictures with a button ///
    
    const onInputUpload = useCallback(async (e: FormEvent<HTMLInputElement>) => {
        for (const file of Array.from(e.currentTarget.files!)) {
            await addFile(file);
        }
    }, [addFile]);
    
    /// drag-and-droppigng pictures ///
    
    const onDropFiles = useCallback(async (files: FileList) => {
        for (const file of Array.from(files)) {
            await addFile(file);
        }
    }, [addFile]);
    
    /// pasting pictures ///
    
    useEffect(() => {
        const onDocumentPaste = async (e: ClipboardEvent) => {
            const items = (e.clipboardData || (e as any).originalEvent.clipboardData).items;
            for (const item of items) {
                if (item.kind === 'file') {
                    await addFile(item.getAsFile());
                }
            }
        };

        document.addEventListener('paste', onDocumentPaste);
        return () => document.removeEventListener('paste', onDocumentPaste);
    }, [addFile]);
    
    /// render ///
    
    return <div>
        <NavBar>
            <h3>
                <i className="bi bi-image"></i>
                &nbsp;&rsaquo;&nbsp;
                Uploading
            </h3>
            <Button color="primary" className="ms-auto" onClick={() => uploadInputRef.current?.click()}>
                <i className="bi bi-upload"></i> Choose files...
            </Button>
            <input ref={uploadInputRef} type="file" hidden multiple onInput={onInputUpload} />
        </NavBar>
        <Container>
            <PicturesList pictures={picturesForUpload} viewMode={viewMode} />
            {!picturesForUpload.length && <h4 className="text-center">
                Choose files to upload, drag or paste them here.
            </h4>}
            <FileDropBox onDrop={onDropFiles} />
        </Container>
    </div>
};

export default PicturesUpload;