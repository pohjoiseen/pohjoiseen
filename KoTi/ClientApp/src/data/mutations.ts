import { useMutation, useQueryClient } from '@tanstack/react-query';
import { QueryKeys } from './queries';
import Picture from '../model/Picture';
import { deletePicture, deletePictures, postPicture, putPicture } from '../api/pictures';
import PictureSet from '../model/PictureSet';
import { deletePictureSet, movePicturesToPictureSet, postPictureSet, putPictureSet } from '../api/pictureSets';
import Tag from '../model/Tag';
import { postTag } from '../api/tags';
import Redirect from '../model/Redirect';
import { deleteRedirect, postRedirect } from '../api/redirects';

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
