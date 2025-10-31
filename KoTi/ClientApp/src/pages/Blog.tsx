import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { errorMessage } from '../util';
import CreateModal from '../components/CreateModal';
import NavBar from '../components/NavBar';
import { usePostsQuery } from '../data/queries';
import useTitle from '../hooks/useTitle';
import Post from '../model/Post';
import Paginator from '../components/Paginator';
import PostCard from '../components/PostCard';

const POSTS_PER_PAGE = 24;

const Blog = () => {
    const [page, setPage] = useState(0);
    const [query, setQuery] = useState('');
    const posts = usePostsQuery(POSTS_PER_PAGE, page * POSTS_PER_PAGE, query);
    const totalPages = Math.ceil((posts.data?.total ?? 0) / POSTS_PER_PAGE);

    //const [isAddPostOpen, setAddPostOpen] = useState(false);
    //const createPostMutation = useCreatePostMutation();
    
    useTitle('Blog');

    if (posts.isError) {
        return <Alert color="danger">Loading blog posts: {errorMessage(posts.error)}</Alert>;
    }

    return <div>
        <NavBar>
            <h3>{posts.isLoading ? <><Spinner type="grow" size="sm"/> Loading...</> : 'Blog'}</h3>
            {/*<Button color="primary" className="ms-auto" onClick={() => setAddPostOpen(true)}>New post...</Button>*/}
        </NavBar>
        <Container>
            {/*createPostMutation.isError && <Alert color="danger">Creating post: {errorMessage(createPostMutation.error)}</Alert>*/}
            {posts.data !== undefined && <>
                {!posts.data.total && <p className="text-muted">No posts yet!</p>}
                <div className="d-flex flex-wrap">
                    {posts.data.data.map((p) => <PostCard key={p} id={p} />)}
                </div>
                <Paginator page={page} setPage={setPage} totalPages={totalPages} />
                {/*isAddPostOpen && <CreateModal
                    object={{
                        id: 0,
                        name: '',
                        title: '',
                        language: 'ru'
                    } as Post}
                    title="Add post"
                    onClose={() => setAddPostOpen(false)}
                    onSubmit={(post) => createPostMutation.mutate(post)}
                />*/}
            </>}
        </Container>
    </div>;
};

export default Blog;
