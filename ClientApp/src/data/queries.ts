import { QueryClient, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCountries, getRegionsForCountry } from '../api/countries';
import { getAreasForRegion } from '../api/regions';
import { getPlacesForArea } from '../api/areas';
import { getPicture, getPictures, GetPicturesOptions } from '../api/pictures';
import ListWithTotal from '../model/ListWithTotal';
import Picture from '../model/Picture';

export enum QueryKeys {
    COUNTRIES = 'countries',
    REGIONS_FOR_COUNTRY = 'regionsForCountry',
    AREAS_FOR_REGION = 'areasForRegion',
    PLACES_FOR_AREA = 'placesForArea',
    PICTURE = 'picture', // actual pictures by id
    PICTURES = 'pictures', // various lists of ids
}

export const useCountriesQuery = () => useQuery({
    queryKey: [QueryKeys.COUNTRIES],
    queryFn: getCountries
});

export const useRegionsQuery = (countryId: number) => useQuery({
    queryKey: [QueryKeys.REGIONS_FOR_COUNTRY, countryId],
    queryFn: () => getRegionsForCountry(countryId)
});

export const useAreasQuery = (regionId: number) => useQuery({
    queryKey: [QueryKeys.AREAS_FOR_REGION, regionId],
    queryFn: () => getAreasForRegion(regionId)
});

export const usePlacesQuery = (areaId: number) => useQuery({
    queryKey: [QueryKeys.PLACES_FOR_AREA, areaId],
    queryFn: () => getPlacesForArea(areaId),
});

export const usePictureQuery = (id: number, disabled?: boolean) => useQuery({
    queryKey: [QueryKeys.PICTURE, id],
    queryFn: () => getPicture(id),
    enabled: !disabled
});

export const getPictureFromCache = (queryClient: QueryClient, id: number) => {
    const picture = queryClient.getQueryData([QueryKeys.PICTURE, id]);
    return picture ? picture as Picture : null;
};

export const usePicturesQuery = (options: GetPicturesOptions) => {
    const queryClient = useQueryClient();
    return useQuery<ListWithTotal<number>>({
        queryKey: [QueryKeys.PICTURES, options],
        queryFn: async () => {
            const result = await getPictures(options);
            // store each picture as individual query
            for (const p of result.data) {
                queryClient.setQueryData([QueryKeys.PICTURE, p.id], p);
            }
            return {
                total: result.total,
                data: result.data.map(p => p.id!)
            };
        }
    });
};