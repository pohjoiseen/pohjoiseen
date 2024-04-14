import { useEffect } from 'react';

const useTitle = (title?: string | false | undefined | (() => string | false | undefined), dependencies?: any[]) => {
    useEffect(() => {
        if (typeof title === 'function') {
            title = title();
        }
        if (title === false) {
            document.title = 'Loading... — KoTi';
        } else if (title === undefined) {
            document.title = 'KoTi';
        } else {
            document.title = title + ' — KoTi';
        }
    }, dependencies || []);
};

export default useTitle;