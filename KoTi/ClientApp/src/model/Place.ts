import ExploreStatus from './ExploreStatus';
import { PlaceCategory } from './PlaceCategory';
import Tag from './Tag';

interface Place {
    id: number;
    areaId: number;
    name: string;
    alias: string;
    category: PlaceCategory;
    notes: string;
    links: string;
    directions: string;
    publicTransport: string;
    season: string;
    exploreStatus: ExploreStatus;
    order: number;
    lat: number;
    lng: number;
    zoom: number;
    isPrivate: boolean;
    rating: number;
    updatedAt: Date;
    thumbnailUrl?: string;
    tags: Tag[];
}

export default Place;