export const handleError = async (response: Response): Promise<never> => {
    const body = await response.text();
    let json: any = null;
    try {
        json = JSON.parse(body);
    } catch (e) {}
    
    if (json) {
        console.error(`${response.status} ${response.statusText}`, json);
        if (json.title) {
            throw new Error(json.title);
        } else {
            throw new ServerError(response, body, json);
        }
    } else {
        console.error(`${response.status} ${response.statusText}`, body);
        throw new ServerError(response, body);
    }
}

export class ServerError extends Error {
    public response: Response;
    public body: string;
    public json: any;
    
    constructor(response: Response, body: string, json: any = null) {
        super();
        this.message = `${response.status} ${response.statusText}, see console`;
        this.response = response;
        this.body = body;
        this.json = json;
    }
}
