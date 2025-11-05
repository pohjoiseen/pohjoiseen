import { QueryClient, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCountries, getRegionsForCountry } from '../api/countries';
import { getAreasForRegion } from '../api/regions';
import { getPlacesForArea } from '../api/areas';
import { getPicture, getPictures, getPicturesForArea, getPicturesForPlace, GetPicturesOptions } from '../api/pictures';
import { search, SearchOptions } from '../api/search';
import ListWithTotal from '../model/ListWithTotal';
import Picture from '../model/Picture';
import PictureSet from '../model/PictureSet';
import { getPictureSet, getPictureSetByName, getPictureSets } from '../api/pictureSets';
import { getStats } from '../api/home';
import { getTags } from '../api/tags';
import { getPost, getPosts } from '../api/posts';

export enum QueryKeys {
    STATS = 'stats',
    COUNTRIES = 'countries',
    REGIONS_FOR_COUNTRY = 'regionsForCountry',
    AREAS_FOR_REGION = 'areasForRegion',
    PLACES_FOR_AREA = 'placesForArea',
    PICTURE = 'picture', // actual pictures by id
    PICTURES = 'pictures', // various lists of ids
    PICTURES_FOR_PLACE = 'picturesForPlace',
    PICTURES_FOR_AREA = 'picturesForArea',
    SETS = 'sets',
    SEARCH = 'search',
    TAGS = 'tags',
    POST = 'post',
    POSTS = 'posts',
}

export const PICTURE_PREVIEW_NUMBER = 10;

export const useStatsQuery = () => useQuery({
    queryKey: [QueryKeys.STATS],
    queryFn: getStats
});

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
    queryFn: async () => {
        if (!areaId) {
            return [];
        }
        return await getPlacesForArea(areaId);
    }
});

export const usePictureQuery = (id: number | null | undefined, disabled?: boolean) => useQuery({
    queryKey: [QueryKeys.PICTURE, id],
    queryFn: () => id ? getPicture(id) : null,
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

export const usePicturesByPlaceQuery = (placeId: number, isEnabled: boolean) => {
    const queryClient = useQueryClient();
    return useQuery<number[]>({
        queryKey: [QueryKeys.PICTURES_FOR_PLACE, placeId],
        queryFn: async () => {
            const result = await getPicturesForPlace(placeId, PICTURE_PREVIEW_NUMBER + 1);
            // store each picture as individual query
            for (const p of result) {
                queryClient.setQueryData([QueryKeys.PICTURE, p.id], p);
            }
            return result.map(p => p.id!);
        },
        enabled: isEnabled
    });
};

export const usePicturesByAreaQuery = (areaId: number) => {
    const queryClient = useQueryClient();
    return useQuery<number[]>({
        queryKey: [QueryKeys.PICTURES_FOR_AREA, areaId],
        queryFn: async () => {
            const result = await getPicturesForArea(areaId, PICTURE_PREVIEW_NUMBER + 1);
            // store each picture as individual query
            for (const p of result) {
                queryClient.setQueryData([QueryKeys.PICTURE, p.id], p);
            }
            return result.map(p => p.id!);
        }
    });
};

export const usePictureSetQuery = (id: number | null) => useQuery({
    queryKey: [QueryKeys.SETS, id],
    queryFn: async (): Promise<PictureSet> => {
        // handle three cases: dummy set (for all pictures mode), fake "top-level" set and regular set
        if (typeof id !== 'number') {
            return { 
                id: 0,
                name: '',
                isPrivate: false,
                parentId: null,
                children: [],
                thumbnailUrls: []
            };
        } 
        if (!id) {
            return {
                id: 0,
                name: '<Root>',
                isPrivate: false,
                parentId: null,
                children: await getPictureSets(),
                thumbnailUrls: []
            };
        }
        return await getPictureSet(id);
    }
});

export const usePictureSetByNameQuery = (name: string) => useQuery({
    queryKey: [QueryKeys.SETS, 'name', name],
    queryFn: async (): Promise<PictureSet | null> => await getPictureSetByName(name)
});

export const useSearchQuery = (options: SearchOptions) => useQuery({
    queryKey: [QueryKeys.SEARCH, options],
    queryFn: async () => {
        if (!options.q) {
            return { total: 0, data: [] };
        }
        return await search(options)
    }
});

export const useTagsQuery = (q: string) => useQuery({
    queryKey: [QueryKeys.TAGS, q],
    queryFn: () => q ? getTags(q) : []
});

export const usePostsQuery = (limit: number, offset: number, searchQuery?: string, enabled?: boolean) => {
    const queryClient = useQueryClient();
    return useQuery<ListWithTotal<number>>({
        queryKey: [QueryKeys.POSTS, { limit, offset, searchQuery }],
        queryFn: async () => {
            const result = await getPosts(limit, offset, searchQuery);
            // store each picture as individual query
            for (const p of result.data) {
                queryClient.setQueryData([QueryKeys.POST, p.id], p);
            }
            return {
                total: result.total,
                data: result.data.map(p => p.id!)
            };
        },
        enabled
    });
};

export const usePostQuery = (id: number) => useQuery({
    queryKey: [QueryKeys.POST, id],
    queryFn: () => getPost(id),
});
