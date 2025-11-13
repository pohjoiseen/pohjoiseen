/**
 * <Publish>: trivial UI for "publish" feature, basically one button and showing its output.
 */
import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Container, Spinner } from 'reactstrap';
import { usePublishMutation } from '../data/mutations';
import { useStatsQuery } from '../data/queries';
import NavBar from '../components/NavBar';
import { errorMessage } from '../util';

const Publish = () => {
    const statsQuery = useStatsQuery();
    const publishMutation = usePublishMutation();
    const [results, setResults] = useState<{ exitCode: number | null, stdout: string, stderr: string }>({ exitCode: null, stdout: '', stderr: '' });
    
    return <div>
        <NavBar>
            <h3>Publish updates</h3>
        </NavBar>
        <Container>
            <p className="fs-4">Last published at: {statsQuery.data ? <b>{new Date(statsQuery.data.databaseLastPublishedAt).toString()}</b> : <Spinner size="sm" />}</p>
            <p>
                Publishing copies the draft version of the content database, used by this KoTi backoffice application,
                for use by Fennica3 frontend, thus making all changes made since last publish publicly available.
                (Content marked as draft is physically copied but Fennica3 doesn't display it in production configuration.)
            </p>
            <div className="text-center mb-2">
                <Button size="lg" color="success" disabled={publishMutation.isLoading}
                        onClick={async () => { try { setResults(await publishMutation.mutateAsync()) } catch (e) {} }}>
                    {publishMutation.isLoading ? <Spinner size="sm" /> : <i className="bi bi-check-lg" />} Publish
                </Button>
            </div>
            {publishMutation.isError && <Alert color="danger">Error calling publishing endpoint: {errorMessage(publishMutation.error)}</Alert>}
            {results.exitCode === 0 && <Alert color="success">Published successfully!
                {results.stdout && <><p className="mt-2">Publish command stdout:</p><pre className="mb-0">{results.stdout.trim()}</pre></>}
                {results.stderr && <><p className="mt-2">Publish command stderr:</p><pre className="mb-0">{results.stderr.trim()}</pre></>}
            </Alert>}
            {results.exitCode !== 0 && results.exitCode !== null && <Alert color="danger">Publishing failed with code {results.exitCode}.
                {results.stdout && <><p className="mt-2">Publish command stdout:</p><pre className="mb-0">{results.stdout.trim()}</pre></>}
                {results.stderr && <><p className="mt-2">Publish command stderr:</p><pre className="mb-0">{results.stderr.trim()}</pre></>}
            </Alert>}
        </Container>
    </div>
};

export default Publish;