import * as React from 'react';
import { useCallback, useEffect, useRef } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { Link, useBlocker, useParams } from 'react-router-dom';
import { useArticleQuery } from '../data/queries';
import NavBar from '../components/NavBar';
import { errorMessage } from '../util';
import useTitle from '../hooks/useTitle';
import ContentEditor, { ContentEditorRef } from '../components/ContentEditor';
import ArticleModel from '../model/Article';
import { useUpdateArticleMutation } from '../data/mutations';
import ArticleMetaPane from '../components/ArticleMetaPane';
import ConfirmModal from '../components/ConfirmModal';

const Article = () => {
    // article id from route
    const routeParams = useParams();
    const articleId = parseInt(routeParams['articleId']!);

    const articleQuery = useArticleQuery(articleId);
    const article = articleQuery.data;
    const updateArticleMutation = useUpdateArticleMutation();

    const editorRef = useRef<ContentEditorRef>(null);

    useTitle(article ? article.title : '', [article]);

    const save = useCallback(async (updatedArticle?: ArticleModel) => {
        await updateArticleMutation.mutateAsync({ ...(updatedArticle ?? article!), contentMD: editorRef.current!.getValue() });
    }, [updateArticleMutation, article]);

    const previewUrl = article ?
        `/${article.language}/article/${article.name}/` : '';

    // detect unsaved changes both on browser and react-router level
    useEffect(() =>  {
        const onBeforeUnload = (e: BeforeUnloadEvent) => {
            if (article?.contentMD !== editorRef.current?.getValue()) {
                e.preventDefault();
                return 'prevent';
            }
        };
        window.addEventListener('beforeunload', onBeforeUnload);
        return () => window.removeEventListener('beforeunload', onBeforeUnload);
    }, [article, editorRef]);
    const blocker = useBlocker(useCallback(() => article?.contentMD !== editorRef.current?.getValue(), [article, editorRef]));

    return <div className="vh-100 d-flex flex-column">
        <NavBar>
            <h3>
                <Link to="/articles">Blog Articles</Link>
                &nbsp;&rsaquo;&nbsp;
                {articleQuery.isLoading && <><Spinner type="grow" size="sm"/> Loading...</>}
                {articleQuery.isError && 'Error'}
                {article?.title} {article && (article.draft ? '[Draft]' : '[Live]')}
            </h3>
            <Button color="primary" className="ms-auto" onClick={() => save()}><i className="bi bi-save" /> Save</Button>
        </NavBar>
        <Container fluid>
            {articleQuery.isError && <Alert color="danger">Loading article: {errorMessage(articleQuery.error)}</Alert>}
            {updateArticleMutation.isError && <Alert color="danger">Saving article: {errorMessage(updateArticleMutation.error)}</Alert>}
        </Container>
        {article && <ContentEditor
            initialValue={article.contentMD}
            metaTabName="Article"
            metaTab={<ArticleMetaPane article={article} onChange={save} />}
            previewUrl={previewUrl}
            onSave={save}
            ref={editorRef}
        />}
        {blocker.state === 'blocked' && <ConfirmModal
            message="You have unsaved changed to the article.  Really navigate away?"
            isOpen={true}
            onYes={blocker.proceed}
            onNo={blocker.reset} />}
    </div>;
};

export default Article;