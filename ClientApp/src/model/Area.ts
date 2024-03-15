import ExploreStatus from './ExploreStatus';
import Place from './Place';

interface Area {
    id: number;
    regionId: number;
    name: string;
    notes: string;
    links: string;
    exploreStatus: ExploreStatus;
    order: number;
    lat: number;
    lng: number;
    zoom: number;
    updatedAt: Date;
    places: Place[];
}

export default Area;