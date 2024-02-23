import { useQuery, useQueryClient } from '@tanstack/react-query';
import { getCountries, getRegionsForCountry } from '../api/countries';
import { getAreasForRegion } from '../api/regions';
import { getPlacesForArea } from '../api/areas';
import { getPicture, getPictures } from '../api/pictures';

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

export const usePictureQuery = (id: number) => useQuery({
    queryKey: [QueryKeys.PICTURE, id],
    queryFn: () => getPicture(id)
});

export const usePicturesAllQuery = () => {
    const queryClient = useQueryClient();
    useQuery({
        queryKey: [QueryKeys.PICTURES],
        queryFn: async () => {
            const pictures = await getPictures();
            // store each picture as individual query
            for (const p of pictures) {
                queryClient.setQueryData([QueryKeys.PICTURE, p.id], p);
            }
            return pictures.map(p => p.id);
        }
    });
}