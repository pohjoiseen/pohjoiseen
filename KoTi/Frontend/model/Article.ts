interface Article {
    id: number;
    draft: boolean;
    name: string;
    title: string;
    contentMD: string;
    language: string;
    updatedAt: Date;
}

export default Article;