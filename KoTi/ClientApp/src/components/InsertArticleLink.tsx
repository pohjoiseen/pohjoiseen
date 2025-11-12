import * as React from 'react';
import { useRef, useState } from 'react';
import { Alert } from 'reactstrap';
import { errorMessage } from '../util';
import { useArticlesQuery } from '../data/queries';
import Paginator from '../components/Paginator';
import ArticleCard from '../components/ArticleCard';

interface InsertArticleLinkProps {
    isActive: boolean;
    onSelect: (articleId: number | null, insertImmediately?: boolean) => void;
}

const ARTICLES_PER_PAGE = 24;

const InsertArticleLink = ({ isActive, onSelect }: InsertArticleLinkProps) => {
    const [page, setPage] = useState(0);
    const articles = useArticlesQuery(ARTICLES_PER_PAGE, page * ARTICLES_PER_PAGE, isActive);
    const totalPages = Math.ceil((articles.data?.total ?? 0) / ARTICLES_PER_PAGE);
    const filterInputRef = useRef<HTMLInputElement>(null);
    const [selectedId, setSelectedId] = useState(0);

    if (articles.isError) {
        return <Alert color="danger">Loading blog articles: {errorMessage(articles.error)}</Alert>;
    }

    const select = ((id: number, ctrlKey: boolean) => {
        onSelect(id, ctrlKey);
        setSelectedId(id);
    })

    return <div className={`overflow-y-auto overflow-x-hidden position-relative pt-2 ${!isActive ? 'd-none' : ''}`}>
        {articles.isSuccess && !articles.data.total && <p className="text-muted">No articles yet!</p>}
        {articles.isSuccess && <div className="d-flex flex-wrap">
            {articles.data.data.map((p) =>
                <ArticleCard key={p} id={p} selected={p === selectedId} onSelect={select} />)}
        </div>}
        <Paginator page={page} setPage={setPage} totalPages={totalPages} />
    </div>;
};

export default InsertArticleLink;
