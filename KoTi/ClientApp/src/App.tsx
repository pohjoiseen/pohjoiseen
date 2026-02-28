import * as React from 'react';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import './custom.css';
import { ModalContainer } from './components/ModalContainer';
import PicturesUpload from './pages/PicturesUpload';
import Pictures from './pages/Pictures';
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
    { path: '/pictures/all', element: <Pictures sets={false} /> },
    { path: '/pictures/folders', element: <Pictures sets={true} /> },
    { path: '/pictures/upload', element: <PicturesUpload /> },

    { path: '/redirects', element: <Redirects /> },
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


