import ExploreStatus from './ExploreStatus';
import { PlaceCategory } from './PlaceCategory';

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
    thumbnailUrl?: string;
}

export default Place;