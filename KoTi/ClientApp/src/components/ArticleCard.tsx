/**
 * Article card for article lists.  Can be dragged but cannot be a drop target.
 */
import * as React from 'react';
import { useRef, MouseEvent, useEffect } from 'react';
import { Alert, Card, CardBody, CardTitle, Spinner } from 'reactstrap';
import { useArticleQuery } from '../data/queries';

interface ArticleCardProps {
    id: number;
    selected?: boolean;
    onSelect?: (id: number, ctrlKey: boolean) => void;
}

const ArticleCard = ({ id, selected, onSelect }: ArticleCardProps) => {
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const onDragStart = (e: DragEvent) => {
            e.dataTransfer?.clearData();
            e.dataTransfer?.setData('text/plain', `article:${id}`);
            e.stopPropagation();
        };
        const el = ref.current;
        el?.addEventListener('dragstart', onDragStart);
        return () => el?.removeEventListener('dragstart', onDragStart);
    }, [id]);

    const articleQuery = useArticleQuery(id);
    const article = articleQuery.data;

    const onClick = (e: MouseEvent) => {
        if (onSelect) {
            e.preventDefault();
            onSelect(id, e.ctrlKey);
        }
    };

    /// render ///

    return <div key={id} className="w-25 pb-1 pe-1">
        <Card className={(selected ? 'shadow-inset ' : '') + (article?.draft ? 'bg-warning-subtle' : '')} draggable={true} innerRef={ref}>
            <CardBody>
                {article && <>
                    <CardTitle tag="h5"><a href={`/app/Articles/${id}`} onClick={onClick} draggable={false}>{article.title}</a></CardTitle>
                    <p className="small m-0">{article.name}</p>
                </>}
                {articleQuery.isLoading && <Spinner size="sm" />}
                {articleQuery.isError && <Alert color="danger">Could not load article</Alert>}
            </CardBody>
        </Card>
    </div>;
}

export default ArticleCard;