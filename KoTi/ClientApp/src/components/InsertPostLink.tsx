import * as React from 'react';
import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { errorMessage } from '../util';
import { usePostsQuery } from '../data/queries';
import Paginator from '../components/Paginator';
import PostCard from '../components/PostCard';

interface InsertPostLinkProps {
    isActive: boolean;
    onSelect: (postId: number | null, insertImmediately?: boolean) => void;
}

const POSTS_PER_PAGE = 24;

const InsertPostLink = ({ isActive, onSelect }: InsertPostLinkProps) => {
    const [page, setPage] = useState(0);
    const [query, setQuery] = useState('');
    const posts = usePostsQuery(POSTS_PER_PAGE, page * POSTS_PER_PAGE, query);
    const totalPages = Math.ceil((posts.data?.total ?? 0) / POSTS_PER_PAGE);
    const filterInputRef = useRef<HTMLInputElement>(null);
    const [selectedId, setSelectedId] = useState(0);
    
    if (posts.isError) {
        return <Alert color="danger">Loading blog posts: {errorMessage(posts.error)}</Alert>;
    }

    const updateQuery = (text: string) => {
        setPage(0);
        setQuery(text);
        setSelectedId(0);
        onSelect(null);
    };
    
    const select = ((id: number, ctrlKey: boolean) => {
        onSelect(id, ctrlKey);
        setSelectedId(id);
    })

    return <div className={`overflow-y-auto overflow-x-hidden position-relative pt-2 ${!isActive ? 'd-none' : ''}`}>
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
            {posts.data.data.map((p) =>
                <PostCard key={p} id={p} selected={p === selectedId} onSelect={select} />)}
        </div>}
        <Paginator page={page} setPage={setPage} totalPages={totalPages} />
    </div>;
};

export default InsertPostLink;
