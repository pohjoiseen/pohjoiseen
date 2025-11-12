/**
 * <ArticleMetaPane>: form to edit everything else about the article except its text.
 */
import * as React from 'react';
import { Alert, Button, FormGroup, Label } from 'reactstrap';
import Article from '../model/Article';
import EditableInline from './EditableInline';
import { errorMessage } from '../util';
import { useDeleteArticleMutation } from '../data/mutations';
import { useNavigate } from 'react-router-dom';

interface ArticleMetaPaneProps {
    article: Article;
    onChange: (article: Article) => void;
}

const ArticleMetaPane = ({ article, onChange }: ArticleMetaPaneProps) => {
    const deleteArticleMutation = useDeleteArticleMutation();
    const navigate = useNavigate();

    const deleteArticle = async () => {
        if (window.prompt('Really delete this article?  This cannot be undone!  Can it be moved to drafts instead?  ' +
            'If you are really certain, type \'yes, delete it!\'.') === 'yes, delete it!') {
            await deleteArticleMutation.mutateAsync(article);
            navigate('/articles');
        }
    };

    return <div>
        {deleteArticleMutation.isError && <Alert color="danger">Deleting article: {errorMessage(deleteArticleMutation.error)}</Alert>}
        <div className="d-flex align-items-center">
            <EditableInline value={article.name}
                            onChange={(value) => onChange({...article, name: value})}
                            validation={{required: true}}/>
        </div>
        <EditableInline
            value={article.title}
            viewTag="h5"
            viewClassName="m-0 d-flex align-items-center"
            onChange={(value) => onChange({...article, title: value})}
            validation={{required: true}}
        />
        <div className="mb-4 mt-2">
            <FormGroup check inline>
                <input
                    type="checkbox"
                    className="form-check-input"
                    id="article-draft"
                    checked={article.draft}
                    onChange={(e) => onChange({...article, draft: e.target.checked})}
                />
                <Label htmlFor="article-draft" check>Draft</Label>
            </FormGroup>
        </div>

        <Button color="danger" onClick={deleteArticle}><i className="bi bi-x-lg" /> Delete</Button>
    </div>};

export default ArticleMetaPane;
