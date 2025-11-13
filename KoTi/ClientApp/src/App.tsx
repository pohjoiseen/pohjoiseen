import * as React from 'react';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import './custom.css';
import { ModalContainer } from './components/ModalContainer';
import Home from './pages/Home';
import Country from './pages/Country';
import Region from './pages/Region';
import Area from './pages/Area';
import PicturesUpload from './pages/PicturesUpload';
import Pictures from './pages/Pictures';
import Search from './pages/Search';
import PlaceRedirect from './pages/PlaceRedirect';
import Blog from './pages/Blog';
import Post from './pages/Post';
import Articles from './pages/Articles';
import Article from './pages/Article';
import Redirects from './pages/Redirects';

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            // This is basically a single-user application and we don't really have to worry
            // about things getting stale from under our nose.  On the other hand we want to avoid
            // especially all kinds of N+1 queries for pictures and such
            staleTime: Infinity
        }
    }
});

const router = createBrowserRouter([
    { index: true, element: <Home /> },
    
    { path: '/country/:countryId', element: <Country /> },
    { path: '/country/:countryId/region/:regionId', element: <Region /> },
    { path: '/country/:countryId/region/:regionId/area/new', element: <Area /> },
    { path: '/country/:countryId/region/:regionId/area/:areaId', element: <Area /> },

    { path: '/place/:placeId', element: <PlaceRedirect /> },

    { path: '/pictures/all', element: <Pictures sets={false} /> },
    { path: '/pictures/folders', element: <Pictures sets={true} /> },
    { path: '/pictures/upload', element: <PicturesUpload /> },

    { path: '/blog', element: <Blog /> },
    { path: '/post/:postId', element: <Post /> },

    { path: '/articles', element: <Articles /> },
    { path: '/article/:articleId', element: <Article /> },

    { path: '/redirects', element: <Redirects /> },
    
    { path: '/search', element: <Search /> },
]);

const App = () => {
    return (
        <DndProvider backend={HTML5Backend}>
            <QueryClientProvider client={queryClient}>
                <ModalContainer>
                    <RouterProvider router={router} />
                </ModalContainer>
            </QueryClientProvider>
        </DndProvider>
    );
};

export default App;


