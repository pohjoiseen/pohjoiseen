/**
 * <PostMetaPane>: form to edit everything else about the post except its text.
 * Has four tabs and is quite messy for the moment.
 * Coats of arms and extra links in geos should be reorderable, should be possible to filter by name when picking an
 * existing CoA, otherwise shooould be feature complete...
 */
import * as React from 'react';
import { useState } from 'react';
import { Alert, Button, Col, FormGroup, Input, Label, Nav, NavItem, NavLink, Row, Spinner } from 'reactstrap';
import { useNavigate } from 'react-router-dom';
import Post, { CoatOfArms, GeoPoint, Link as LinkModel } from '../model/Post';
import { usePictureQuery, usePictureSetByNameQuery } from '../data/queries';
import EditableInline from './EditableInline';
import EditableTextarea from './EditableTextarea';
import InsertPicture from './InsertPicture';
import Upload from './Upload';
import { PicturesViewMode } from './pictureViewCommon';
import { COATS_OF_ARMS_SET } from '../model/PictureSet';
import { errorMessage } from '../util';
import MapPointPicker, { DEFAULT_LAT, DEFAULT_LNG } from './MapPointPicker';
import { MapType } from '../model/MapDefinitions';
import GeoIconPicker from './GeoIconPicker';
import PostCard from './PostCard';
import InsertPostLink from './InsertPostLink';
import { useDeletePostMutation } from '../data/mutations';

interface PostMetaPaneProps {
    post: Post;
    onChange: (post: Post) => void;
}

enum PostMetaPaneMode {
    Main,
    TitlePicture,
    CoatsOfArms,
    Geo
}

const PostMetaMain = ({ post, onChange }: PostMetaPaneProps) => {
    const deletePostMutation = useDeletePostMutation();
    const navigate = useNavigate();
    
    const deletePost = async () => {
        if (window.prompt('Really delete this post?  This cannot be undone!  Can it be moved to drafts instead?  ' +
            'If you are really certain, type \'yes, delete it!\'.') === 'yes, delete it!') {
            await deletePostMutation.mutateAsync(post);
            navigate('/blog');
        }
    };
    
    return <>
        {deletePostMutation.isError && <Alert color="danger">Deleting post: {errorMessage(deletePostMutation.error)}</Alert>}
        <Row className="mb-4 d-flex align-items-center">
            <Col xs={3}>
                <Input type="date" value={post.date.toISOString().substring(0, 10)}
                       onChange={(e) => onChange({...post, date: new Date(e.target.value)})}/>
            </Col>
            <Col xs={9}>
                <EditableInline value={post.name}
                                onChange={(value) => onChange({...post, name: value})}
                                validation={{required: true}}/>
            </Col>
        </Row>
        <EditableInline
            value={post.title}
            viewTag="h5"
            viewClassName="m-0 d-flex align-items-center"
            onChange={(value) => onChange({...post, title: value})}
            validation={{required: true}}
        />
        <div className="mb-4 mt-2">
            <FormGroup check inline>
                <input
                    type="checkbox"
                    className="form-check-input"
                    id="post-draft"
                    checked={post.draft}
                    onChange={(e) => onChange({...post, draft: e.target.checked})}
                />
                <Label htmlFor="post-draft" check>Draft</Label>
            </FormGroup>
            <FormGroup check inline>
                <input
                    type="checkbox"
                    className="form-check-input"
                    id="post-mini"
                    checked={post.mini}
                    onChange={(e) => onChange({...post, mini: e.target.checked})}
                />
                <Label htmlFor="post-mini" check>Mini</Label>
            </FormGroup>
        </div>

        <EditableTextarea
            emptyValueString="Post description..."
            value={post.description}
            onChange={(value) => onChange({...post, description: value})}
        />

        <EditableTextarea
            titleString="Date description"
            emptyValueString="(optional)"
            value={post.dateDescription || ''}
            onChange={(value) => onChange({...post, dateDescription: value})}
        />
        <EditableTextarea
            titleString="Location description"
            emptyValueString="(optional)"
            value={post.locationDescription || ''}
            onChange={(value) => onChange({...post, locationDescription: value})}
        />
        <EditableTextarea
            titleString="Address"
            emptyValueString="(optional)"
            value={post.address || ''}
            onChange={(value) => onChange({...post, address: value})}
        />
        <EditableTextarea
            titleString="Public transport"
            emptyValueString="(optional)"
            value={post.publicTransport || ''}
            onChange={(value) => onChange({...post, publicTransport: value})}
        />
        
        <Button color="danger" onClick={deletePost}><i className="bi bi-x-lg" /> Delete</Button>
    </>
};

const PostMetaTitlePicture = ({ post, onChange }: PostMetaPaneProps) => {
    const [showPicker, setShowPicker] = useState(false);
    const [selectedPictureId, setSelectedPictureId] = useState<number | null>(null);
    const titlePictureQuery = usePictureQuery(post.titlePictureId || null);
    
    if (showPicker) {
        const select = (id: number | null, doSelect: boolean | undefined) => {
            setSelectedPictureId(id);
            if (doSelect) {
                onChange({ ...post, titlePictureId: id || undefined });
                setShowPicker(false);
            }
        };
        
        return <>
            <div className="position-sticky bg-white z-1 pb-2" style={{ top: '-8px' }}>
                <Button color="primary" className="me-2" disabled={!selectedPictureId} onClick={() => select(selectedPictureId, true)}>Select</Button>
                <Button className="me-2" onClick={() => select(null, true)}>Unset</Button>
                <Button className="me-2" onClick={() => setShowPicker(false)}>Cancel</Button>
            </div>
            <InsertPicture isActive={true} onSelect={select} />
        </>;
    }
    
    return <>
        {titlePictureQuery.isLoading && <Spinner type="grow" />}
        {titlePictureQuery.isError && <Alert color="danger">Failed to load title picture</Alert>}
        {titlePictureQuery.data && <>
            <img src={titlePictureQuery.data.detailsUrl} className="mw-100" alt="" /><br/>
            <div className="text-muted">{titlePictureQuery.data.filename}</div>
        </>}
        {!post.titlePictureId && <div className="text-muted">No title picture.</div>}
        <Button onClick={() => setShowPicker(true)}>{post.titlePictureId ? 'Change...' : 'Add...'}</Button>
        {post.titlePictureId && <>
            <div className="d-flex form-switch mt-2 fw-bold ps-0">
                <Label htmlFor="title-image-in-text" className="me-5">In page header</Label>
                <Input
                    type="switch"
                    id="title-image-in-text"
                    checked={post.titleImageInText}
                    onChange={(e) => onChange({ ...post, titleImageInText: e.target.checked })}
                />
                <Label htmlFor="title-image-in-text" className="ms-2">In post text</Label>
            </div>
            {!post.titleImageInText && <label className="d-flex align-items-center">
                <div className="me-1 fw-bold">Vertical offset for picture, %:</div>
                <EditableInline
                    value={post.titleImageOffsetY?.toString() || ''}
                    placeholder="50"
                    onChange={(value) => onChange({ ...post, titleImageOffsetY: isNaN(parseInt(value)) ? undefined : parseInt(value) })}
                    validation={{ valueAsNumber: true }}
                />
            </label>}
            {post.titleImageInText && <EditableTextarea
                titleString="Caption"
                emptyValueString="(optional)"
                value={post.titleImageCaption || ''}
                onChange={(value) => onChange({ ...post, titleImageCaption: value })}
            />}
        </>}
    </>;
};

const CoA = ({ coa, onChange, onDelete }: { coa: CoatOfArms, onChange: (coa: CoatOfArms) => void, onDelete: () => void }) => {
    const idParsed = parseInt(coa.url.replace('picture:', ''));
    const id = isNaN(idParsed) ? null : idParsed;
    const coaPictureQuery = usePictureQuery(idParsed);
    
    return <div>
        {!id && <Alert color="text-danger">Invalid coat of arms record.
            <Button className="d-inline-block w-auto ms-2" color="danger" onClick={() => window.confirm('Delete coat of arms?') && onDelete()}>
                <i className="bi bi-x" /> Delete
            </Button>
        </Alert>}
        {coaPictureQuery.isLoading && <Spinner type="grow" />}
        {coaPictureQuery.isError && <Alert color="text-danger">Coat of arms loading failed.</Alert>}
        {coaPictureQuery.data && <div className="d-flex gap-2">
            <img src={coaPictureQuery.data.url} width="200" alt="" />
            <div className="d-flex flex-column gap-2 align-items-start">
                <div className="text-muted">{coaPictureQuery.data.filename}</div>
                <label className="d-flex align-items-center">
                    <div className="me-1 fw-bold">Override size, px:</div>
                    <EditableInline
                        value={coa.size?.toString() || ''}
                        placeholder="150"
                        onChange={(value) => onChange({ ...coa, size: isNaN(parseInt(value)) ? undefined : parseInt(value) })}
                        validation={{ valueAsNumber: true }}
                    />
                </label>
                <Button className="d-inline-block w-auto" color="danger" onClick={() => window.confirm('Delete coat of arms?') && onDelete()}>
                    <i className="bi bi-x" /> Delete
                </Button>
            </div>
        </div>}
        <hr />
    </div>;
};

const PostMetaCoA = ({ post, onChange }: PostMetaPaneProps) => {
    const [showUpload, setUpload] = useState(false);
    const [selectedPictureId, setSelectedPictureId] = useState<number | null>(null);
    const coaPictureSetQuery = usePictureSetByNameQuery(COATS_OF_ARMS_SET);
    
    if (showUpload) {
        if (coaPictureSetQuery.isLoading) {
            return <Spinner type="grow" />;
        }
        const select = (id: number | null, doSelect: boolean | undefined) => {
            setSelectedPictureId(id);
            if (doSelect) {
                onChange({ ...post, coatsOfArms: [...(post.coatsOfArms || []), { url: 'picture:' + id }]});
                setUpload(false);
            }
        };

        return <>
            <div className="position-sticky bg-white z-1 pb-2" style={{ top: '-8px' }}>
                <div className="mb-2">
                    <Button color="primary" className="me-2" disabled={!selectedPictureId} onClick={() => select(selectedPictureId, true)}>Select</Button>
                    <Button className="me-2" onClick={() => setUpload(false)}>Cancel</Button>
                </div>
                {coaPictureSetQuery.isError && <Alert color="danger">Loading picture set: {errorMessage(coaPictureSetQuery.error)}</Alert>}
                {coaPictureSetQuery.isSuccess && !coaPictureSetQuery.data && <Alert color="warning">
                    Coats of arms are saved to '{COATS_OF_ARMS_SET}' folder,
                    which is not found. Please create the folder. Pictures will be saved to root folder instead.
                </Alert>}
                {coaPictureSetQuery.isSuccess && coaPictureSetQuery.data && <Alert color="success">
                    Coats of arms will be placed into '{COATS_OF_ARMS_SET}' folder.
                </Alert>}           
            </div>
            <Upload
                viewMode={PicturesViewMode.THUMBNAILS}
                onSelect={select}
                setId={coaPictureSetQuery?.data?.id}
            />
        </>;
    }

    return <>
        {!post.coatsOfArms?.length && <div className="mb-2 text-muted">No coats of arms.</div>}
        {(post.coatsOfArms || []).map((coa, index) => 
            <CoA key={index} coa={coa} 
                 onChange={(coa) => {
                     const coas = [...post.coatsOfArms!];
                     coas[index] = coa;
                     onChange({ ...post, coatsOfArms: coas });
                 }}
                 onDelete={() => {
                     const coas = [...post.coatsOfArms!];
                     coas.splice(index, 1);
                     onChange({ ...post, coatsOfArms: coas });
                 }} />)}
        <div>
            <Button className="me-2" onClick={() => {
                setSelectedPictureId(null);
                setUpload(true);
            }}>Upload...</Button>
        </div>
    </>;
};

const GeoLink = ({ link, onChange, onDelete }: { link: LinkModel, onChange: (link: LinkModel) => void, onDelete: () => void }) => {
    const postIdRaw = parseInt(link.path.replace('post:', '') || '');
    const postId = isNaN(postIdRaw) ? null : postIdRaw;
    const hashIndex = link.path.indexOf('#');
    const hash = hashIndex !== -1 ? link.path.substring(hashIndex + 1) : '';

    return <div className="d-flex gap-2 align-items-start">
        {postId && <PostCard id={postId} onSelect={() => {}} />}
        <div>
            <EditableTextarea
                titleString="Link text"
                value={link.label || ''}
                onChange={(value) => onChange({ ...link, label: value })}
            />
            <EditableTextarea
                titleString="Anchor in target post"
                value={hash}
                emptyValueString="(optional)"
                onChange={(value) => onChange({ ...link, path: `post:${postId}${value ? '#' + value : ''}` })}
            />
            <Button className="d-inline-block w-auto" color="danger" onClick={() => window.confirm('Delete link?') && onDelete()}>
                <i className="bi bi-x" /> Delete
            </Button>
        </div>
    </div>;
};

const MAPS = {
    'index': 'Default (Finland)',
    'osm': 'Global/OSM'
};

const Geo = ({ geo, onChange, onDelete }: { geo: GeoPoint, onChange: (geo: GeoPoint) => void, onDelete: () => void }) => {
    const titlePictureIdRaw = parseInt(geo.titleImage?.replace('picture:', '') || '');
    const titlePictureId = isNaN(titlePictureIdRaw) ? null : titlePictureIdRaw; 
    const titlePictureQuery = usePictureQuery(titlePictureId);
    const [showPicturePicker, setShowPicturePicker] = useState(false);
    const [selectedPictureId, setSelectedPictureId] = useState<number | null>(null);
    const [showPostPicker, setShowPostPicker] = useState(false);
    const [selectedPostId, setSelectedPostId] = useState<number | null>(null);
    
    if (showPicturePicker) {
        const select = (id: number | null, doSelect: boolean | undefined) => {
            setSelectedPictureId(id);
            if (doSelect) {
                onChange({ ...geo, titleImage: id ? 'picture:' + id : undefined });
                setShowPicturePicker(false);
            }
        };

        return <>
            <div className="position-sticky bg-white z-1 pb-2" style={{ top: '-8px' }}>
                <Button color="primary" className="me-2" disabled={!selectedPictureId} onClick={() => select(selectedPictureId, true)}>Select</Button>
                <Button className="me-2" onClick={() => select(null, true)}>Unset</Button>
                <Button className="me-2" onClick={() => setShowPicturePicker(false)}>Cancel</Button>
            </div>
            <InsertPicture isActive={true} onSelect={select} />
        </>;
    }
    
    if (showPostPicker) {
        const select = (id: number | null, doSelect: boolean | undefined) => {
            setSelectedPostId(id);
            if (doSelect && id) {
                onChange({ ...geo, links: [...(geo.links || []), { path: 'post:' + id, label: 'new link' }]});
                setShowPostPicker(false);
            }
        };

        return <>
            <div className="position-sticky bg-white z-1 pb-2" style={{ top: '-8px' }}>
                <Button color="primary" className="me-2" disabled={!selectedPostId} onClick={() => select(selectedPostId, true)}>Select</Button>
                <Button className="me-2" onClick={() => setShowPostPicker(false)}>Cancel</Button>
            </div>
            <InsertPostLink isActive={true} onSelect={select} />
        </>;
    }
    
    return <>
        <MapPointPicker 
            mapType={MapType.DefaultOSM}
            lat={geo.lat}
            lng={geo.lng} 
            zoom={geo.zoom || 4} 
            onChange={(lat, lng, zoom) => {
                if (!lat && !lng) {
                    if (!window.confirm('Delete this point?')) {
                        onDelete();
                    }
                } else {
                    onChange({...geo, lat, lng, zoom});
                }
            }}
        />
        
        <label className="fw-bold d-flex align-items-center gap-2 mb-4">
            Minimal zoom level to display:
            <Input type="number" min="0" max="10" size={5} className="flex-grow-0" style={{ width: '72px' }}
                   value={geo.zoom || 0} onChange={e => onChange({ ...geo, zoom: e.target.valueAsNumber })}/>
        </label>
        
        <EditableTextarea
            titleString="Title"
            value={geo.title || ''}
            emptyValueString="default to post title"
            onChange={(value) => onChange({ ...geo, title: value })}
        />
        <EditableTextarea
            titleString="Subtitle"
            value={geo.subtitle || ''}
            emptyValueString="(optional)"
            onChange={(value) => onChange({ ...geo, subtitle: value })}
        />

        <div className="fw-bold">Title picture:</div>
        {titlePictureQuery.isLoading && <Spinner type="grow" />}
        {titlePictureQuery.isError && <Alert color="danger">Failed to load title picture</Alert>}
        {titlePictureQuery.data && <>
            <img src={titlePictureQuery.data.detailsUrl} className="mw-100" alt="" /><br/>
            <div className="text-muted">{titlePictureQuery.data.filename}</div>
        </>}
        {!titlePictureId && <div className="text-muted">Not set/default to post</div>}
        <Button className="mb-3" onClick={() => setShowPicturePicker(true)}>{titlePictureId ? 'Change...' : 'Add...'}</Button>

        <EditableTextarea
            titleString="Description"
            value={geo.description || ''}
            emptyValueString="default to post description"
            onChange={(value) => onChange({ ...geo, description: value })}
        />
        <EditableTextarea
            titleString="Anchor in post"
            value={geo.anchor || ''}
            emptyValueString="(optional)"
            onChange={(value) => onChange({ ...geo, anchor: value })}
        />

        <div className="d-flex align-items-center">
            <b>Icon: </b>
            <GeoIconPicker icon={geo.icon || 'star'} onChange={(icon) => onChange({ ...geo, icon})} />
        </div>

        <div className="mt-2 mb-2">
            <b>Show on maps: </b>
            {Object.entries(MAPS).map(([key, title]) =>
                <FormGroup check inline key={key}>
                    <input
                        type="checkbox"
                        className="form-check-input"
                        checked={(geo.maps || []).includes(key)}
                        onChange={(e) => onChange({ ...geo, maps:
                            e.target.checked
                                ? [...(geo.maps || []), key]
                                : (geo.maps || []).filter(m => m !== key)
                        })}
                    />
                    <Label>{title}</Label>
                </FormGroup>
            )}
        </div>
        
        <div className="mt-2 mb-2">
            <b>Links to other posts:</b>
            {(geo.links || []).map((link, index) => 
                <GeoLink link={link} key={index}
                      onChange={(link) => {
                          const links = [...geo.links!];
                          links[index] = link;
                          onChange({ ...geo, links });
                      }}
                      onDelete={() => {
                          const links = [...geo.links!];
                          links.splice(index, 1);
                          onChange({ ...geo, links });
                      }} />
            )}
            {!geo.links?.length && <div className="text-muted">None</div>}
            <Button onClick={() => setShowPostPicker(true)}>Add link...</Button>
        </div>

        <hr style={{ clear: 'both'}}/>
    </>
};

const PostMetaGeo = ({ post, onChange }: PostMetaPaneProps) => {
    return <>
        {!post.geo?.length && <div className="mb-2 text-muted">No geo points.</div>}
        {(post.geo || []).map((geo, index) =>
            <Geo key={index} geo={geo}
                 onChange={(geo) => {
                     const geos = [...post.geo!];
                     geos[index] = geo;
                     onChange({ ...post, geo: geos });
                 }}
                 onDelete={() => {
                     const geos = [...post.geo!];
                     geos.splice(index, 1);
                     onChange({ ...post, geo: geos });
                 }} />)}
        <div>
            <Button className="me-2" onClick={() => onChange({ ...post, geo: [...(post.geo || []), { lat: DEFAULT_LAT, lng: DEFAULT_LNG, zoom: 0 }]})}>Add...</Button>
        </div>
    </>;
};

const PostMetaPane = ({ post, onChange }: PostMetaPaneProps) => {
    const [mode, setMode] = useState(PostMetaPaneMode.Main);
    const [geoInitialized, setGeoInitialized] = useState(false);
    return <>
        <Nav pills className="mb-2">
            <NavItem>
                <NavLink className="cursor-pointer"
                         active={mode === PostMetaPaneMode.Main}
                         onClick={() => setMode(PostMetaPaneMode.Main)}>
                    Main
                </NavLink>
            </NavItem>
            <NavItem>
                <NavLink className="cursor-pointer"
                         active={mode === PostMetaPaneMode.TitlePicture}
                         onClick={() => setMode(PostMetaPaneMode.TitlePicture)}>
                    Title picture
                </NavLink>
            </NavItem>
            <NavItem>
                <NavLink className="cursor-pointer"
                         active={mode === PostMetaPaneMode.CoatsOfArms}
                         onClick={() => setMode(PostMetaPaneMode.CoatsOfArms)}>
                    Coat of arms
                </NavLink>
            </NavItem>
            <NavItem>
                <NavLink className="cursor-pointer"
                         active={mode === PostMetaPaneMode.Geo}
                         onClick={() => {
                             setMode(PostMetaPaneMode.Geo);
                             setGeoInitialized(true);
                         }}>
                    Geo
                </NavLink>
            </NavItem>
        </Nav>
        <div className={`overflow-y-auto overflow-x-hidden p-2 ${mode !== PostMetaPaneMode.Main ? 'd-none' : ''}`}>
            <PostMetaMain post={post} onChange={onChange} />
        </div>
        <div className={`overflow-y-auto overflow-x-hidden p-2 ${mode !== PostMetaPaneMode.TitlePicture ? 'd-none' : ''}`}>
            <PostMetaTitlePicture post={post} onChange={onChange} />
        </div>
        <div className={`overflow-y-auto overflow-x-hidden p-2 ${mode !== PostMetaPaneMode.CoatsOfArms ? 'd-none' : ''}`}>
            <PostMetaCoA post={post} onChange={onChange} />
        </div>
        <div className={`overflow-y-auto overflow-x-hidden p-2 ${mode !== PostMetaPaneMode.Geo ? 'd-none' : ''}`}>
            {/* Initialize geo tab lazily to avoid map sizing issues */}
            {geoInitialized && <PostMetaGeo post={post} onChange={onChange} />}
        </div>

    </>;
};

export default PostMetaPane;
