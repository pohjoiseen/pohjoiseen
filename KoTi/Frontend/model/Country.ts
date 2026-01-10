import { MapType } from './MapDefinitions';

interface Country {
    id: number;
    name: string;
    flagEmoji: string;
    mapType: MapType;
    order: number;
}

export default Country;
