import * as React from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { Link, useParams } from 'react-router-dom';
import { usePostQuery } from '../data/queries';
import NavBar from '../components/NavBar';
import { errorMessage } from '../util';
import useTitle from '../hooks/useTitle';
import ContentEditor, { ContentEditorRef } from '../components/ContentEditor';
import { useUpdatePostMutation } from '../data/mutations';
import { useCallback, useRef } from 'react';

const Post = () => {
    // post id from route
    const routeParams = useParams();
    const postId = parseInt(routeParams['postId']!);

    const postQuery = usePostQuery(postId);
    const post = postQuery.data;
    const updatePostMutation = useUpdatePostMutation();
    
    const editorRef = useRef<ContentEditorRef>(null);
    
    useTitle(post ? `${post.title} (${post.date.toLocaleDateString('fi')})` : '');
    
    const save = useCallback(async () => {
        await updatePostMutation.mutateAsync({ ...post!, contentMD: editorRef.current!.getValue() });
    }, [updatePostMutation, post]);

    return <div className="vh-100 d-flex flex-column">
        <NavBar>
            <h3>
                <Link to="/blog">Blog</Link>
                &nbsp;&rsaquo;&nbsp;
                {postQuery.isLoading && <><Spinner type="grow" size="sm"/> Loading...</>}
                {postQuery.isError && 'Error'}
                {post && <>{post.date.toLocaleDateString('fi')}&nbsp;&rsaquo;&nbsp;{post.title}</>}
            </h3>
            <Button color="primary" className="ms-auto" onClick={() => save()}><i className="bi bi-save" /> Save</Button>
        </NavBar>
        <Container fluid>
            {postQuery.isError && <Alert color="danger">Loading post: {errorMessage(postQuery.error)}</Alert>}
            {updatePostMutation.isError && <Alert color="danger">Saving post: {errorMessage(updatePostMutation.error)}</Alert>}
        </Container>
        <Container fluid className="flex-grow-1">
            {post && <>
                <div className="w-50 h-100 m-auto">
                    <ContentEditor
                        initialValue={post.contentMD}
                        onSave={save}
                        ref={editorRef}
                    />
                </div>
            </>}
        </Container>
    </div>;
};

export default Post;