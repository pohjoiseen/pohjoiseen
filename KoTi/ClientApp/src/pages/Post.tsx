import * as React from 'react';
import { useCallback, useRef } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { Link, useParams } from 'react-router-dom';
import { usePostQuery } from '../data/queries';
import NavBar from '../components/NavBar';
import { errorMessage } from '../util';
import useTitle from '../hooks/useTitle';
import ContentEditor, { ContentEditorRef } from '../components/ContentEditor';
import PostModel from '../model/Post';
import { useUpdatePostMutation } from '../data/mutations';
import PostMetaPane from '../components/PostMetaPane';

const Post = () => {
    // post id from route
    const routeParams = useParams();
    const postId = parseInt(routeParams['postId']!);

    const postQuery = usePostQuery(postId);
    const post = postQuery.data;
    const updatePostMutation = useUpdatePostMutation();
    
    const editorRef = useRef<ContentEditorRef>(null);
    
    useTitle(post ? `${post.title} (${post.date.toLocaleDateString('fi')})` : '', [post]);
    
    const save = useCallback(async (updatedPost?: PostModel) => {
        await updatePostMutation.mutateAsync({ ...(updatedPost ?? post!), contentMD: editorRef.current!.getValue() });
    }, [updatePostMutation, post]);
    
    const previewUrl = post ?
        `/${post.language}/${post.date.getFullYear()}` + 
        `/${post.date.getMonth() < 9 ? '0' : ''}${post.date.getMonth() + 1}` +
        `/${post.date.getDate() < 10 ? '0' : ''}${post.date.getDate()}` +
        `/${post.name}/` : '';

    return <div className="vh-100 d-flex flex-column">
        <NavBar>
            <h3>
                <Link to="/blog">Blog</Link>
                &nbsp;&rsaquo;&nbsp;
                {postQuery.isLoading && <><Spinner type="grow" size="sm"/> Loading...</>}
                {postQuery.isError && 'Error'}
                {post?.title} {post && (post.draft ? '[Draft]' : '[Live]')}
            </h3>
            <Button color="primary" className="ms-auto" onClick={() => save()}><i className="bi bi-save" /> Save</Button>
        </NavBar>
        <Container fluid>
            {postQuery.isError && <Alert color="danger">Loading post: {errorMessage(postQuery.error)}</Alert>}
            {updatePostMutation.isError && <Alert color="danger">Saving post: {errorMessage(updatePostMutation.error)}</Alert>}
        </Container>
        {post && <ContentEditor
            initialValue={post.contentMD}
            metaTabName="Post"
            metaTab={<PostMetaPane post={post} onChange={save} />}
            previewUrl={previewUrl}
            onSave={save}
            ref={editorRef}
        />}
    </div>;
};

export default Post;