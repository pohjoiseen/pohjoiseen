/**
 * <Redirects>: simple editor of redirects on blog webpage.
 * TODO: issues a request per every target post/article...
 */
import * as React from 'react';
import { FormEvent, useState } from 'react';
import { Alert, Button, Container, Input, Spinner } from 'reactstrap';
import { errorMessage } from '../util';
import { useArticleQuery, usePostQuery, useRedirectsQuery } from '../data/queries';
import { useCreateRedirectMutation, useDeleteRedirectMutation } from '../data/mutations';
import useTitle from '../hooks/useTitle';
import NavBar from '../components/NavBar';
import Paginator from '../components/Paginator';
import { confirmModal } from '../components/ModalContainer';
import InsertPostLink from '../components/InsertPostLink';
import InsertArticleLink from '../components/InsertArticleLink';

const REDIRECTS_PER_PAGE = 50;

const RedirectPost = ({ postId, hash }: { postId: number, hash: string }) => {
    const postQuery = usePostQuery(postId);
    if (postQuery.isLoading) {
        return <i>Loading</i>;
    }
    if (postQuery.isError) {
        return <span className="text-danger">Error loading post</span>;
    }
    return <>
        Post: {postQuery.data.title} ({postQuery.data.date.toISOString().substring(0, 10)})
        {hash && <i> {hash}</i>}
    </>;
};

const RedirectArticle = ({ articleId, hash }: { articleId: number, hash: string }) => {
    const articleQuery = useArticleQuery(articleId);
    if (articleQuery.isLoading) {
        return <i>Loading</i>;
    }
    if (articleQuery.isError) {
        return <span className="text-danger">Error loading article</span>;
    }
    return <>
        Article: {articleQuery.data.title}
        {hash && <i> {hash}</i>}
    </>;
};

const RedirectValue = ({ value }: { value: string }) => {
    if (value.startsWith('post:')) {
        const matches = value.match(/^post:([0-9]+)(#.*)?$/);
        if (matches) {
            return <RedirectPost postId={parseInt(matches[1])} hash={matches[2]} />;
        }
    }
    if (value.startsWith('article:')) {
        const matches = value.match(/^article:([0-9]+)(#.*)?$/);
        if (matches) {
            return <RedirectArticle articleId={parseInt(matches[1])} hash={matches[2]} />;
        }
    }
    return <>{value}</>;
};

enum AddMode {
    None,
    Url,
    Post,
    Article
}

const Redirects = () => {
    const [page, setPage] = useState(0);
    const redirects = useRedirectsQuery(REDIRECTS_PER_PAGE, page * REDIRECTS_PER_PAGE);
    const totalPages = Math.ceil((redirects.data?.total ?? 0) / REDIRECTS_PER_PAGE);
    const [addMode, setAddMode] = useState(AddMode.None);
    const [urlFrom, setUrlFrom] = useState('');
    const [plainUrl, setPlainUrl] = useState('');
    const [postId, setPostId] = useState(0);
    const [articleId, setArticleId] = useState(0);
    const [anchor, setAnchor] = useState('');
    const createRedirectMutation = useCreateRedirectMutation();
    const deleteRedirectMutation = useDeleteRedirectMutation();

    useTitle('HTTP Redirects');
    
    const addRedirect = async (urlTo: string) => {
        try {
            await createRedirectMutation.mutateAsync({id: 0, urlFrom, urlTo});
            setAddMode(AddMode.None);
        } catch (e) {
            // don't throw one expected error, will flag mutation as error and will be displayed
            if ((e as Error).message !== 'Redirect already exists') {
                throw e;
            }
        }
    }
    
    const addPlainURL = async (e: FormEvent) => {
        e.preventDefault();
        await addRedirect(plainUrl);
    };

    const addPost = async (e: FormEvent) => {
        e.preventDefault();
        if (postId) {
            await addRedirect('post:' + postId + (anchor ? '#' + anchor : ''));   
        }
    };
    
    const addArticle = async (e: FormEvent) => {
        e.preventDefault();
        if (articleId) {
            await addRedirect('article:' + articleId + (anchor ? '#' + anchor : ''));
        }
    };
    
    const deleteRedirect = async (id: number) => {
        if (await confirmModal("Delete this redirect?")) {
            await deleteRedirectMutation.mutateAsync(id);
        }
    };

    return <div className="vh-100 d-flex flex-column  overflow-y-scroll overflow-x-hidden position-relative">
        <NavBar>
            <h3>{redirects.isInitialLoading ? <><Spinner type="grow" size="sm"/> Loading...</> : 'HTTP Redirects'}</h3>
        </NavBar>
        <Container className="mh-100">
            {redirects.isError && <Alert color="danger">Loading redirects: {errorMessage(redirects.error)}</Alert>}
            {createRedirectMutation.isError && <Alert color="danger">Creating redirect: {errorMessage(createRedirectMutation.error)}</Alert>}
            {deleteRedirectMutation.isError && <Alert color="danger">Deleting redirect: {errorMessage(deleteRedirectMutation.error)}</Alert>}
            {addMode === AddMode.None && <>
                <div className="d-flex gap-2 align-items-center">
                    Add redirect to:
                    <Button onClick={() => {
                        setAddMode(AddMode.Url);
                        setPlainUrl('');
                        setUrlFrom('');
                    }}>Plain URL</Button>
                    <Button onClick={() => {
                        setAddMode(AddMode.Post);
                        setUrlFrom('');
                        setPostId(0);
                        setAnchor('');
                    }}>Post</Button>
                    <Button onClick={() => {
                        setAddMode(AddMode.Article);
                        setUrlFrom('');
                        setArticleId(0);
                        setAnchor('');
                    }}>Article</Button>
                </div>
                {redirects.isSuccess && !redirects.data.total && <p className="text-muted">No redirects yet!</p>}
                {redirects.isSuccess && !!redirects.data.data.length && <table className="table"><tbody>
                    {redirects.data.data.map(r => <tr key={r.id}>
                        <td className="align-middle">{r.urlFrom}</td>
                        <td className="align-middle"><i className="bi bi-arrow-right" /></td>
                        <td className="align-middle"><RedirectValue value={r.urlTo} /></td>
                        <td className="align-middle"><Button color="danger" onClick={() => deleteRedirect(r.id)}><i className="bi bi-x-lg" /> Delete</Button></td>
                    </tr>)}
                </tbody></table>}
                <Paginator page={page} setPage={setPage} totalPages={totalPages} />
            </>}

            {addMode === AddMode.Url && <form onSubmit={addPlainURL}>
                <table className="w-100"><tbody>
                    <tr>
                        <td className="p-2"><label htmlFor="urlFrom">From:</label></td>
                        <td className="p-2 w-100"><Input id="urlFrom" value={urlFrom} onChange={(e) => setUrlFrom(e.target.value)} required /></td>
                    </tr>
                    <tr>
                        <td className="p-2"><label htmlFor="plainUrlTo">To:</label></td>
                        <td className="p-2 w-100"><Input id="plainUrlTo" value={plainUrl} onChange={(e) => setPlainUrl(e.target.value)} required /></td>
                    </tr>
                </tbody></table>
                <div className="d-flex gap-2 align-items-center mt-2">
                    <Button type="submit" color="primary">Save</Button>
                    <Button onClick={() => setAddMode(AddMode.None)}>Cancel</Button>
                </div>
            </form>}

            {addMode === AddMode.Post && <form onSubmit={addPost} id="postForm">
                <div>
                    <label htmlFor="urlFrom">From:</label>
                    <Input id="urlFrom" value={urlFrom} onChange={(e) => setUrlFrom(e.target.value)} required />
                </div>
                
                <div className="mt-4">Select post:</div>
                
                <InsertPostLink isActive={true} onSelect={(postId, insertImmediately) => {
                    setPostId(postId || 0);
                    if (insertImmediately) {
                        (document.getElementById('postForm') as HTMLFormElement).requestSubmit();
                    }
                }} />
                
                <div className="pt-2 pb-2 position-sticky bottom-0 bg-white">
                    <div>
                        <label htmlFor="anchor">Anchor on post page:</label>
                        <Input id="anchor" value={anchor} onChange={(e) => setAnchor(e.target.value)} placeholder="(optional)" />
                    </div>
                    <div className="mt-2 d-flex gap-2 align-items-center">
                        <Button type="submit" color="primary" disabled={!postId}>Save</Button>
                        <Button onClick={() => setAddMode(AddMode.None)}>Cancel</Button>
                    </div>
                </div>
            </form>}

            {addMode === AddMode.Article && <form onSubmit={addArticle} id="articleForm">
                <div>
                    <label htmlFor="urlFrom">From:</label>
                    <Input id="urlFrom" value={urlFrom} onChange={(e) => setUrlFrom(e.target.value)} required />
                </div>

                <div className="mt-4">Select article:</div>

                <InsertArticleLink isActive={true} onSelect={(articleId, insertImmediately) => {
                    setArticleId(articleId || 0)
                    if (insertImmediately) {
                        (document.getElementById('articleForm') as HTMLFormElement).requestSubmit();
                    }
                }} />

                <div className="pt-2 pb-2 position-sticky bottom-0 bg-white">
                    <div>
                        <label htmlFor="anchor">Anchor on article page:</label>
                        <Input id="anchor" value={anchor} onChange={(e) => setAnchor(e.target.value)} placeholder="(optional)" />
                    </div>
                    <div className="mt-2 d-flex gap-2 align-items-center">
                        <Button type="submit" color="primary" disabled={!articleId}>Save</Button>
                        <Button onClick={() => setAddMode(AddMode.None)}>Cancel</Button>
                    </div>
                </div>
            </form>}

        </Container>
    </div>;
};

export default Redirects;
