import * as React from 'react';
import { Pagination, PaginationItem, PaginationLink } from 'reactstrap';
import { useEffect } from 'react';

interface PaginatorProps {
    page: number;
    setPage: (page: number) => void;
    totalPages: number;
    disableKeyboardNav?: boolean;
}

const Paginator = ({ page, setPage, totalPages, disableKeyboardNav }: PaginatorProps) => {

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            // page navigation
            if (!disableKeyboardNav && e.ctrlKey) {
                if (e.key === 'Home' && page > 0) {
                    e.preventDefault();
                    setPage(0);
                }
                if (e.key === 'ArrowLeft' && page > 0) {
                    e.preventDefault();
                    setPage(page - 1);
                }
                if (e.key === 'ArrowRight' && page < totalPages - 1) {
                    e.preventDefault();
                    setPage(page + 1);
                }
                if (e.key === 'End' && page < totalPages - 1) {
                    e.preventDefault();
                    setPage(totalPages - 1);
                }
            }
        };
        document.addEventListener('keydown', handler);
        return () => document.removeEventListener('keydown', handler);
    }, [page, totalPages, setPage]);

    if (totalPages <= 1) {
        return null;
    }
    return <Pagination className="m-auto d-flex justify-content-center">
        <PaginationItem disabled={page === 0}>
            <PaginationLink onClick={() => setPage(0)}>
                <i className="bi bi-rewind-fill" title="Ctrl-Home"/>
            </PaginationLink>
        </PaginationItem>
        <PaginationItem disabled={page === 0}>
            <PaginationLink onClick={() => setPage(page - 1)}>
                <i className="bi bi-caret-left-fill" title="Ctrl-Left"/>
            </PaginationLink>
        </PaginationItem>
        {Array.from(Array(totalPages).keys()).map(p => <PaginationItem key={p} active={p === page}>
            <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
        </PaginationItem>)}
        <PaginationItem disabled={page === totalPages - 1}>
            <PaginationLink onClick={() => setPage(page + 1)}>
                <i className="bi bi-caret-right-fill" title="Ctrl-Right"/>
            </PaginationLink>
        </PaginationItem>
        <PaginationItem disabled={page === totalPages - 1}>
            <PaginationLink onClick={() => setPage(totalPages - 1)}>
                <i className="bi bi-fast-forward-fill" title="Ctrl-End"/>
            </PaginationLink>
        </PaginationItem>
    </Pagination>;
};

export default Paginator;
