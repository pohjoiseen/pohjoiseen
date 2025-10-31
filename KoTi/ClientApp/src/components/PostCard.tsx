/**
 * Post card for post lists.  Can be dragged but cannot be a drop target.
 */
import * as React from 'react';
import { useRef } from 'react';
import { Link } from 'react-router-dom';
import { useDrag } from 'react-dnd';
import { Alert, Card, CardBody, CardTitle, Spinner } from 'reactstrap';
import DnDTypes from '../model/DnDTypes';
import { usePostQuery } from '../data/queries';

interface PostCardProps {
    id: number;
}

const PostCard = ({ id }: PostCardProps) => {
    const ref = useRef<HTMLDivElement>(null);
    
    const [{ isDragging }, drag] = useDrag({
        type: DnDTypes.POST,
        item: () => ({ id }),
        collect: (monitor) => ({
            isDragging: monitor.isDragging()
        })
    });

    drag(ref);
    
    const postQuery = usePostQuery(id);
    const post = postQuery.data;

    /// render ///

    return <div key={id} ref={ref} className={`w-25 pb-1 pe-1 ${isDragging ? 'is-dragging' : ''}`}>
        <Card>
            <CardBody>
                {post && <>
                    {post.titlePicture && <Link to={`/post/${id}`}><img alt="" className="w-100 mb-1" src={post.titlePicture.thumbnailUrl} /></Link>}
                    <CardTitle tag="h5"><Link to={`/post/${id}`}>{post.title}</Link></CardTitle>
                    <p className="small m-0">{post.date.toISOString().substring(0, 10)}-{post.name}</p>
                </>}
                {postQuery.isLoading && <Spinner size="sm" />}
                {postQuery.isError && <Alert color="danger">Could not load post</Alert>}
            </CardBody>
        </Card>
    </div>;
}

export default PostCard;