import { useEffect } from 'react';

const useTitle = (title?: string | false | undefined | (() => string | false | undefined), dependencies?: any[]) => {
    useEffect(() => {
        const realTitle = typeof title === 'function' ? title() : title; 
        if (realTitle === false) {
            document.title = 'Loading... — KoTi';
        } else if (realTitle === undefined) {
            document.title = 'KoTi';
        } else {
            document.title = realTitle + ' — KoTi';
        }
    }, dependencies || [title]);  // eslint-disable-line
};

export default useTitle;