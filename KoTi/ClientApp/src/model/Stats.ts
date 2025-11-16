interface Stats {
    totalPictures: number;
    totalPicturesWithNoLocation: number;
    totalPlaces: number;
    totalPosts: number;
    totalArticles: number;
    databaseLastPublishedAt: string;
    databaseSize: number;
    s3Bucket: string;
}

export default Stats;