import * as React from 'react';
import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { useNavigate } from 'react-router-dom';
import { errorMessage } from '../util';
import NavBar from '../components/NavBar';
import { usePostsQuery } from '../data/queries';
import useTitle from '../hooks/useTitle';
import Post from '../model/Post';
import Paginator from '../components/Paginator';
import PostCard from '../components/PostCard';
import { useCreatePostMutation } from '../data/mutations';
import CreatePostModal from '../components/CreatePostModal';

const POSTS_PER_PAGE = 24;

const Blog = () => {
    const [page, setPage] = useState(0);
    const [query, setQuery] = useState('');
    const posts = usePostsQuery(POSTS_PER_PAGE, page * POSTS_PER_PAGE, query);
    const totalPages = Math.ceil((posts.data?.total ?? 0) / POSTS_PER_PAGE);
    const filterInputRef = useRef<HTMLInputElement>(null);

    const [isAddPostOpen, setAddPostOpen] = useState(false);
    const createPostMutation = useCreatePostMutation();
    const navigate = useNavigate();
    
    useTitle('Blog');
    useEffect(() => filterInputRef.current?.focus(), []);

    if (posts.isError) {
        return <Alert color="danger">Loading blog posts: {errorMessage(posts.error)}</Alert>;
    }
    
    const updateQuery = (text: string) => {
        setPage(0);
        setQuery(text);
    };
    
    const create = async (post: Post) => {
        post = await createPostMutation.mutateAsync(post);
        navigate(`/app/Posts/${post.id}`);
    };
    
    return <div>
        <NavBar>
            <h3>{posts.isInitialLoading ? <><Spinner type="grow" size="sm"/> Loading...</> : 'Blog'}</h3>
            <Button color="primary" className="ms-auto" onClick={() => setAddPostOpen(true)}><i className="bi bi-plus-lg" /> New post...</Button>
        </NavBar>
        <Container>
            {createPostMutation.isError && <Alert color="danger">Creating post: {errorMessage(createPostMutation.error)}</Alert>}
            <div className="d-flex align-items-center mb-2">
                <div>Find:</div>
                <input
                    className="form-control flex-grow-1 ms-1 me-1"    
                    value={query}
                    onChange={e => updateQuery(e.target.value)}
                    ref={filterInputRef}
                />
            </div>
            {posts.isSuccess && !posts.data.total && <p className="text-muted">No posts yet!</p>}
            {posts.isSuccess && <div className="d-flex flex-wrap">
                {posts.data.data.map((p) => <PostCard key={p} id={p} />)}
            </div>}
            <Paginator page={page} setPage={setPage} totalPages={totalPages} />
            {isAddPostOpen && <CreatePostModal
                onClose={() => setAddPostOpen(false)}
                onSubmit={(post) => create({ 
                    ...post,
                    date: new Date(),
                    draft: true,
                    contentMD: '',
                    description: '',
                    mini: false,
                    updatedAt: new Date()
                })}
            />}
        </Container>
    </div>;
};

export default Blog;
