/**
 * Post card for post lists.  Can be dragged but cannot be a drop target.
 */
import * as React from 'react';
import { useRef, MouseEvent, useEffect } from 'react';
import { Alert, Card, CardBody, CardTitle, Spinner } from 'reactstrap';
import { usePostQuery } from '../data/queries';

interface PostCardProps {
    id: number;
    selected?: boolean;
    onSelect?: (id: number, ctrlKey: boolean) => void;
}

const PostCard = ({ id, selected, onSelect }: PostCardProps) => {
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const onDragStart = (e: DragEvent) => {
            e.dataTransfer?.clearData();
            e.dataTransfer?.setData('text/plain', `post:${id}`);
            e.stopPropagation();
        };
        const el = ref.current;
        el?.addEventListener('dragstart', onDragStart);
        return () => el?.removeEventListener('dragstart', onDragStart);
    }, [id]);

    const postQuery = usePostQuery(id);
    const post = postQuery.data;
    
    const onClick = (e: MouseEvent) => {
        if (onSelect) {
            e.preventDefault();
            onSelect(id, e.ctrlKey);
        }
    };

    /// render ///

    return <div key={id} className="w-25 pb-1 pe-1">
        <Card className={(selected ? 'shadow-inset ' : '') + (post?.draft ? 'bg-warning-subtle' : '')} draggable={true} innerRef={ref}>
            <CardBody>
                {post && <>
                    {post.titlePicture && <a href={`/app/Posts/${id}`} onClick={onClick} draggable={false}><img draggable={false} alt="" className="w-100 mb-1" src={post.titlePicture.thumbnailUrl} /></a>}
                    <CardTitle tag="h5"><a href={`/app/Posts/${id}`} onClick={onClick} draggable={false}>{post.title}</a></CardTitle>
                    <p className="small m-0">{post.date.toISOString().substring(0, 10)}-{post.name}</p>
                </>}
                {postQuery.isLoading && <Spinner size="sm" />}
                {postQuery.isError && <Alert color="danger">Could not load post</Alert>}
            </CardBody>
        </Card>
    </div>;
}

export default PostCard;