import * as React from 'react';
import { Pagination, PaginationItem, PaginationLink } from 'reactstrap';
import { ReactNode, useEffect } from 'react';

interface PaginatorProps {
    page: number;
    setPage: (page: number) => void;
    totalPages: number;
    disableKeyboardNav?: boolean;
}

const Paginator = ({ page, setPage, totalPages, disableKeyboardNav }: PaginatorProps) => {

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            const targetElem = e.target as HTMLElement;
            // page navigation
            if (!disableKeyboardNav && e.ctrlKey &&
                targetElem.tagName !== 'INPUT' && targetElem.tagName !== 'TEXTAREA') {
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
    
    let pages: ReactNode;
    if (totalPages < 15) {
        // show all pages if there are few enough
        pages = Array.from(Array(totalPages).keys()).map(p => <PaginationItem key={p} active={p === page}>
            <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
        </PaginationItem>);
    } else if (page <= 7) {
        // 1,2,3,*4*,5,6...98,99,100
        pages = <>
            {Array.from(Array(page + 3).keys()).map(p => <PaginationItem key={p} active={p === page}>
                <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
            </PaginationItem>)}
            <PaginationItem><PaginationLink><i className="bi bi-three-dots" /></PaginationLink></PaginationItem>
            {Array.from(Array(3).keys()).map(k => {
                const p = totalPages - 3 + k;
                return <PaginationItem key={p} active={p === page}>
                    <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
                </PaginationItem>;
            })}
        </>;
    } else if (page >= totalPages - 8) {
        // 1,2,3...95,96,*97*,98,99,100
        pages = <>
            {Array.from(Array(3).keys()).map(p => <PaginationItem key={p} active={p === page}>
                <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
            </PaginationItem>)}
            <PaginationItem><PaginationLink><i className="bi bi-three-dots" /></PaginationLink></PaginationItem>
            {Array.from(Array(totalPages - page + 2).keys()).map(k => {
                const p = totalPages - (totalPages - page + 2) + k;
                return <PaginationItem key={p} active={p === page}>
                    <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
                </PaginationItem>;
            })}
        </>;
    } else {
        // 1,2,3...48,49,*50*,51,52...98,99,100
        pages = <>
            {Array.from(Array(3).keys()).map(p => <PaginationItem key={p} active={p === page}>
                <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
            </PaginationItem>)}
            <PaginationItem><PaginationLink><i className="bi bi-three-dots" /></PaginationLink></PaginationItem>
            {Array.from(Array(5).keys()).map(k => {
                const p = page - 2 + k;
                return <PaginationItem key={p} active={p === page}>
                    <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
                </PaginationItem>;
            })}
            <PaginationItem><PaginationLink><i className="bi bi-three-dots" /></PaginationLink></PaginationItem>
            {Array.from(Array(3).keys()).map(k => {
                const p = totalPages - 3 + k;
                return <PaginationItem key={p} active={p === page}>
                    <PaginationLink onClick={() => setPage(p)}>{p + 1}</PaginationLink>
                </PaginationItem>;
            })}
        </>;
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
        {pages}
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
