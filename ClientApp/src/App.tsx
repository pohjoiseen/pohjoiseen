import * as React from 'react';
import { Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import './custom.css';
import { ModalContainer } from './components/ModalContainer';
import Home from './pages/Home';
import Country from './pages/Country';
import Region from './pages/Region';
import Area from './pages/Area';

const queryClient = new QueryClient();

const App = () => {
    return (
        <QueryClientProvider client={queryClient}>
            <ModalContainer>
                <Routes>
                    <Route index={true} element={<Home />} />
                    <Route path="/country/:countryId" element={<Country />} />
                    <Route path="/country/:countryId/region/:regionId" element={<Region />} />
                    <Route path="/country/:countryId/region/:regionId/area/new" element={<Area />} />
                    <Route path="/country/:countryId/region/:regionId/area/:areaId" element={<Area />} />
                </Routes>
            </ModalContainer>
        </QueryClientProvider>
    );
};

export default App;


