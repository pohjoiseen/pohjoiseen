import { QueryKey, useMutation, useQueryClient } from '@tanstack/react-query';
import debounce from 'lodash/debounce';
import { QueryKeys } from './queries';
import Region from '../model/Region';
import Area from '../model/Area';
import Place from '../model/Place';
import { reorderRegionsInCountry } from '../api/countries';
import { deleteRegion, postRegion, putRegion, reorderAreasInRegion } from '../api/regions';
import { deleteArea, postArea, putArea, reorderPlacesInArea } from '../api/areas';
import { deletePlace, postPlace, putPlace } from '../api/places';
import Picture from '../model/Picture';
import { deletePicture, deletePictures, ensureWebSizes, postPicture, putPicture } from '../api/pictures';
import PictureSet from '../model/PictureSet';
import { deletePictureSet, movePicturesToPictureSet, postPictureSet, putPictureSet } from '../api/pictureSets';
import Tag from '../model/Tag';
import { postTag } from '../api/tags';
import Post from '../model/Post';
import { deletePost, postPost, putPost } from '../api/posts';
import Article from '../model/Article';
import { deleteArticle, postArticle, putArticle } from '../api/articles';
import Redirect from '../model/Redirect';
import { deleteRedirect, postRedirect } from '../api/redirects';

export const useReorderRegionsMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (regions: Region[]) => {
            if (!regions.length) {
                return;
            }
            await reorderRegionsInCountry(regions[0].countryId, regions.map(r => r.id));
            const regionsForCountryData: Region[] | undefined = queryClient.getQueryData([QueryKeys.REGIONS_FOR_COUNTRY, regions[0].countryId]);
            if (regionsForCountryData) {
                queryClient.setQueryData([QueryKeys.REGIONS_FOR_COUNTRY, regions[0].countryId], regions);
            } else {
                await queryClient.invalidateQueries([QueryKeys.REGIONS_FOR_COUNTRY, regions[0].countryId]);
            }
        }
    });
};

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
            area.updatedAt = new Date();
            const areasForRegionData: Area[] | undefined = queryClient.getQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            if (areasForRegionData) {
                queryClient.setQueryData([QueryKeys.AREAS_FOR_REGION, area.regionId], areasForRegionData.map(a => a.id === area.id ? area : a));
            } else {
                await queryClient.invalidateQueries([QueryKeys.AREAS_FOR_REGION, area.regionId]);
            }
            await putArea(area.id, area);
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

export const useReorderAreasMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (areas: Area[]) => {
            if (!areas.length) {
                return;
            }
            await reorderAreasInRegion(areas[0].regionId, areas.map(a => a.id));
            const areasForRegionData: Area[] | undefined = queryClient.getQueryData([QueryKeys.AREAS_FOR_REGION, areas[0].regionId]);
            if (areasForRegionData) {
                queryClient.setQueryData([QueryKeys.AREAS_FOR_REGION, areas[0].regionId], areas);
            } else {
                await queryClient.invalidateQueries([QueryKeys.AREAS_FOR_REGION, areas[0].regionId]);
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
            // mostly to make sure place autocomplete in picture details does not get stuck with old results
            // when a place is created/edited inline
            await queryClient.invalidateQueries([QueryKeys.SEARCH]);
            return place;
        },
    });
};

export const useUpdatePlaceMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (place: Place) => {
            place.updatedAt = new Date();
            const placesForAreaData: Place[] | undefined = queryClient.getQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            if (placesForAreaData) {
                queryClient.setQueryData([QueryKeys.PLACES_FOR_AREA, place.areaId], placesForAreaData.map(p => p.id === place.id ? place : p));
            } else {
                await queryClient.invalidateQueries([QueryKeys.PLACES_FOR_AREA, place.areaId]);
            }
            // mostly to make sure place autocomplete in picture details does not get stuck with old results
            // when a place is created/edited inline
            await queryClient.invalidateQueries([QueryKeys.SEARCH]);
            await putPlace(place.id, place);
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

const reoderPlacesInAreaDebounced = debounce(reorderPlacesInArea, 500);

export const useReorderPlacesMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (places: Place[]) => {
            if (!places.length) {
                return;
            }
            const placesForAreaData: Place[] | undefined = queryClient.getQueryData([QueryKeys.PLACES_FOR_AREA, places[0].areaId]);
            if (placesForAreaData) {
                queryClient.setQueryData([QueryKeys.PLACES_FOR_AREA, places[0].areaId], places);
            }
            await reoderPlacesInAreaDebounced(places[0].areaId, places.map(p => p.id));
        }
    });
};

export const useCreatePictureMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (picture: Picture) => {
            picture = await postPicture(picture);
            // uploading a new picture invalidates all existing picture lists
            queryClient.removeQueries({ queryKey: [QueryKeys.PICTURES] });
            return picture;
        },
    });
};

export const useUpdatePictureMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (picture: Picture) => {
            picture.updatedAt = new Date();
            queryClient.setQueryData([QueryKeys.PICTURE, picture.id], picture);
            await putPicture(picture.id!, picture);
            // changing a picture might invalidate some pictures lists (reordered, picture added or removed from some)
            // but we don't attempt to track that, in fact IMHO it would only lead to worse UX
        }
    });
};

export const useDeletePictureMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (picture: Picture) => {
            // deleting a picture invalidates all existing picture lists
            await deletePicture(picture.id!);
            await queryClient.invalidateQueries({ queryKey: [QueryKeys.PICTURES] });
        }
    });
};

export const useDeletePicturesMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (ids: number[]) => {
            // deleting a picture invalidates all existing picture lists
            await deletePictures(ids);
            await queryClient.invalidateQueries({ queryKey: [QueryKeys.PICTURES] });
        }
    });
};

export const useEnsurePictureWebSizesMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (picture: Picture | number) => await ensureWebSizes(typeof picture === 'number' ? picture : picture.id!)
    });
};

export const useCreatePictureSetMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (pictureSet: PictureSet) => {
            pictureSet = await postPictureSet(pictureSet);
            queryClient.setQueryData([QueryKeys.SETS, pictureSet.id], pictureSet);
            await queryClient.invalidateQueries([QueryKeys.SETS, pictureSet.parentId || 0]);
            return pictureSet;
        },
    })
};


export const useUpdatePictureSetMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (pictureSet: PictureSet) => {
            await putPictureSet(pictureSet.id, pictureSet);
            queryClient.setQueryData([QueryKeys.SETS, pictureSet.id], pictureSet);
            await queryClient.invalidateQueries([QueryKeys.SETS, pictureSet.parentId || 0]);
        }
    });
};

export const useDeletePictureSetMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (pictureSet: PictureSet) => {
            await deletePictureSet(pictureSet.id);
            await queryClient.invalidateQueries([QueryKeys.SETS, pictureSet.parentId || 0]);
        }
    });
};

export const useMovePicturesToPictureSetMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ id, pictureIds }: { id: number | null, pictureIds: number[] }) => {
            if (!pictureIds.length) {
                return;
            }
            await movePicturesToPictureSet(id, pictureIds);
            // rare operation, just invalidate everything
            await queryClient.invalidateQueries([QueryKeys.SETS]);
            await queryClient.invalidateQueries([QueryKeys.PICTURES]);
        }
    });
};

export const useCreateTagMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (tag: Tag) => {
            tag = await postTag(tag);
            await queryClient.invalidateQueries([QueryKeys.TAGS]);
            return tag;
        },
    })
};

export const useCreatePostMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (post: Post) => {
            post = await postPost(post);
            await queryClient.invalidateQueries([QueryKeys.POSTS]);
            return post;
        },
    })
};

export const useUpdatePostMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (post: Post) => {
            queryClient.setQueryData([QueryKeys.POST, post.id], post);
            await putPost(post.id, post);
            await queryClient.invalidateQueries([QueryKeys.POSTS]);
            return post;
        }
    });
};

export const useDeletePostMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (post: Post) => {
            await deletePost(post.id);
            await queryClient.invalidateQueries([QueryKeys.POSTS]);
            await queryClient.invalidateQueries([QueryKeys.POST, post.id]);
        }
    });
};

export const useCreateArticleMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (article: Article) => {
            article = await postArticle(article);
            await queryClient.invalidateQueries([QueryKeys.ARTICLES]);
            return article;
        },
    })
};

export const useUpdateArticleMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (article: Article) => {
            queryClient.setQueryData([QueryKeys.ARTICLE, article.id], article);
            await putArticle(article.id, article);
            await queryClient.invalidateQueries([QueryKeys.ARTICLES]);
            return article;
        }
    });
};

export const useDeleteArticleMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (article: Article) => {
            await deleteArticle(article.id);
            await queryClient.invalidateQueries([QueryKeys.ARTICLES]);
            await queryClient.invalidateQueries([QueryKeys.ARTICLE, article.id]);
        }
    });
};

export const useCreateRedirectMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (redirect: Redirect) => {
            await postRedirect(redirect);
            await queryClient.invalidateQueries([QueryKeys.REDIRECTS]);
        }
    })
};

export const useDeleteRedirectMutation = () => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (id: number) => {
            await deleteRedirect(id);
            await queryClient.invalidateQueries([QueryKeys.REDIRECTS]);
        }
    })
};
