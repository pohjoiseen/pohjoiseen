import { useMutation, useQueryClient } from '@tanstack/react-query';
import Region from '../model/Region';
import { deleteRegion, postRegion, putRegion } from '../api/regions';
import { QueryKeys } from './queries';
import Area from '../model/Area';
import { deleteArea, postArea, putArea } from '../api/areas';
import Place from '../model/Place';
import { deletePlace, postPlace, putPlace } from '../api/places';

export const useCreateRegionMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (region: Region) => {
            region = await postRegion(region);
            const regionsForCountryData: Region[] | undefined = queryClient.getQueryData([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId]);
            if (regionsForCountryData) {
                queryClient.setQueryData([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId], [...regionsForCountryData, region]);
                queryClient.setQueryData([QueryKeys.AREAS_FOR_REGION, region.id], []);
            } else {
                await queryClient.invalidateQueries([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId]);
            }
            return region;
        },
    })
};

export const useUpdateRegionMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (region: Region) => {
            await putRegion(region.id, region);
            const regionsForCountryData: Region[] | undefined = queryClient.getQueryData([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId]); 
            if (regionsForCountryData) {
                queryClient.setQueryData([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId],
                    regionsForCountryData.map(r => r.id === region.id ? region : r));
            } else {
                await queryClient.invalidateQueries([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId]);
            }
        }
    });
};

export const useDeleteRegionMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (region: Region) => {
            await deleteRegion(region.id);
            const regionsForCountryData: Region[] | undefined = queryClient.getQueryData([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId]);
            if (regionsForCountryData) {
                queryClient.setQueryData([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId],
                    regionsForCountryData.filter(r => r.id !== region.id));
            } else {
                await queryClient.invalidateQueries([QueryKeys.REGIONS_FOR_COUNTRY, region.countryId]);
            }
        }
    });
};

export const useCreateAreaMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (area: Area) => {
            area = await postArea(area);
            const areasForRegionData: Area[] | undefined = queryClient.getQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            if (areasForRegionData) {
                queryClient.setQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId], [...areasForRegionData, area]);
                queryClient.setQueryData([QueryKeys.PLACES_FOR_AREA, area.id], []);
            } else {
                await queryClient.invalidateQueries([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            }
            return area;
        },
    });
};

export const useUpdateAreaMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (area: Area) => {
            await putArea(area.id, area);
            const areasForRegionData: Area[] | undefined = queryClient.getQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            if (areasForRegionData) {
                queryClient.setQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId], areasForRegionData.map(a => a.id === area.id ? area : a));
            } else {
                await queryClient.invalidateQueries([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            }
        }
    });
};

export const useDeleteAreaMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (area: Area) => {
            await deleteArea(area.id);
            const areasForRegionData: Area[] | undefined = queryClient.getQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            if (areasForRegionData) {
                queryClient.setQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId], areasForRegionData.filter(a => a.id !== area.id));
            } else {
                await queryClient.invalidateQueries([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            }
        }
    });
};


export const useCreatePlaceMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (place: Place) => {
            place = await postPlace(place);
            const placesForAreaData: Place[] | undefined = queryClient.getQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            if (placesForAreaData) {
                queryClient.setQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId], [...placesForAreaData, place]);
            } else {
                await queryClient.invalidateQueries([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            }
            return place;
        },
    });
};

export const useUpdatePlaceMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (place: Place) => {
            await putPlace(place.id, place);
            const placesForAreaData: Place[] | undefined = queryClient.getQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            if (placesForAreaData) {
                queryClient.setQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId], placesForAreaData.map(p => p.id === place.id ? place : p));
            } else {
                await queryClient.invalidateQueries([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            }
        }
    });
};

export const useDeletePlaceMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (place: Place) => {
            await deletePlace(place.id);
            const placesForAreaData: Place[] | undefined = queryClient.getQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            if (placesForAreaData) {
                queryClient.setQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId], placesForAreaData.filter(p => p.id !== place.id));
            } else {
                await queryClient.invalidateQueries([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            }
        }
    });
};
