import { useQuery } from '@tanstack/react-query';
import { getCountries, getRegionsForCountry } from '../api/countries';
import { getAreasForRegion } from '../api/regions';
import { getPlacesForArea } from '../api/areas';

export enum QueryKeys {
    COUNTRIES = 'countries',
    REGIONS_FOR_COUNTRY = 'regionsForCountry',
    AREAS_FOR_REGION = 'areasForRegion',
    PLACES_FOR_AREA = 'placesForArea'
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
