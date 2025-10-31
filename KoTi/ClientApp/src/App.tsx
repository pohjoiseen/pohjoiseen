import * as React from 'react';
import { Route, Routes } from 'react-router-dom';
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

const App = () => {
    return (
        <DndProvider backend={HTML5Backend}>
            <QueryClientProvider client={queryClient}>
                <ModalContainer>
                    <Routes>
                        <Route index={true} element={<Home />} />
                        
                        <Route path="/country/:countryId" element={<Country />} />
                        <Route path="/country/:countryId/region/:regionId" element={<Region />} />
                        <Route path="/country/:countryId/region/:regionId/area/new" element={<Area />} />
                        <Route path="/country/:countryId/region/:regionId/area/:areaId" element={<Area />} />
                        
                        <Route path="/place/:placeId" element={<PlaceRedirect />} />

                        <Route path="/pictures/all" element={<Pictures sets={false} />} />
                        <Route path="/pictures/folders" element={<Pictures sets={true} />} />
                        <Route path="/pictures/upload" element={<PicturesUpload />} />

                        <Route path="/blog" element={<Blog />} />
                        
                        <Route path="/search" element={<Search />} />
                    </Routes>
                </ModalContainer>
            </QueryClientProvider>
        </DndProvider>
    );
};

export default App;


