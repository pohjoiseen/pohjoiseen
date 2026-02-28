import { QueryClient, useQuery, useQueryClient } from '@tanstack/react-query';
import { getPicture, getPictures, GetPicturesOptions } from '../api/pictures';
import ListWithTotal from '../model/ListWithTotal';
import Picture from '../model/Picture';
import PictureSet from '../model/PictureSet';
import { getPictureSet, getPictureSets } from '../api/pictureSets';
import { getTags } from '../api/tags';
import { getPost, getPosts } from '../api/posts';
import { getArticle, getArticles } from '../api/articles';
import { getRedirects } from '../api/redirects';

export enum QueryKeys {
    PICTURE = 'picture', // actual pictures by id
    PICTURES = 'pictures', // various lists of ids
    SETS = 'sets',
    TAGS = 'tags',
    POST = 'post',
    POSTS = 'posts',
    ARTICLES = 'articles',
    ARTICLE = 'article',
    REDIRECTS = 'redirects',
}

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
            // store each post as individual query
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

export const useArticlesQuery = (limit: number, offset: number, enabled?: boolean) => {
    const queryClient = useQueryClient();
    return useQuery<ListWithTotal<number>>({
        queryKey: [QueryKeys.ARTICLES, { limit, offset }],
        queryFn: async () => {
            const result = await getArticles(limit, offset);
            for (const a of result.data) {
                queryClient.setQueryData([QueryKeys.ARTICLES, a.id], a);
            }
            return {
                total: result.total,
                data: result.data.map(a => a.id!)
            };
        },
        enabled
    });
};

export const useArticleQuery = (id: number) => useQuery({
    queryKey: [QueryKeys.ARTICLE, id],
    queryFn: () => getArticle(id),
});

export const useRedirectsQuery = (limit: number, offset: number) => useQuery({
    queryKey: [QueryKeys.REDIRECTS, { limit, offset }],
    queryFn: async () => await getRedirects(limit, offset)
});
