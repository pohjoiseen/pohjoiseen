import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { useNavigate } from 'react-router-dom';
import { errorMessage } from '../util';
import NavBar from '../components/NavBar';
import { useArticlesQuery } from '../data/queries';
import useTitle from '../hooks/useTitle';
import Article from '../model/Article';
import Paginator from '../components/Paginator';
import ArticleCard from '../components/ArticleCard';
import { useCreateArticleMutation } from '../data/mutations';
import CreatePostModal from '../components/CreatePostModal';

const ARTICLES_PER_PAGE = 24;

const Articles = () => {
    const [page, setPage] = useState(0);
    const articles = useArticlesQuery(ARTICLES_PER_PAGE, page * ARTICLES_PER_PAGE);
    const totalPages = Math.ceil((articles.data?.total ?? 0) / ARTICLES_PER_PAGE);

    const [isAddArticleOpen, setAddArticleOpen] = useState(false);
    const createArticleMutation = useCreateArticleMutation();
    const navigate = useNavigate();

    useTitle('Blog articles');

    if (articles.isError) {
        return <Alert color="danger">Loading blog articles: {errorMessage(articles.error)}</Alert>;
    }
    
    const create = async (article: Article) => {
        article = await createArticleMutation.mutateAsync(article);
        navigate(`/article/${article.id}`);
    };

    return <div>
        <NavBar>
            <h3>{articles.isInitialLoading ? <><Spinner type="grow" size="sm"/> Loading...</> : 'Blog Articles'}</h3>
            <Button color="primary" className="ms-auto" onClick={() => setAddArticleOpen(true)}><i className="bi bi-plus-lg" /> New article...</Button>
        </NavBar>
        <Container>
            {createArticleMutation.isError && <Alert color="danger">Creating article: {errorMessage(createArticleMutation.error)}</Alert>}
            {articles.isSuccess && !articles.data.total && <p className="text-muted">No articles yet!</p>}
            {articles.isSuccess && <div className="d-flex flex-wrap">
                {articles.data.data.map((p) => <ArticleCard key={p} id={p} />)}
            </div>}
            <Paginator page={page} setPage={setPage} totalPages={totalPages} />
            {isAddArticleOpen && <CreatePostModal
                onClose={() => setAddArticleOpen(false)}
                onSubmit={(article) => create({ ...article, draft: true, contentMD: '', updatedAt: new Date() }) }
            />}
        </Container>
    </div>;
};

export default Articles;
