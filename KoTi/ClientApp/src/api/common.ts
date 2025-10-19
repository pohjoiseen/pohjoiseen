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
            throw new Error(`${response.status} ${response.statusText}, see console`);
        }
    } else {
        console.error(`${response.status} ${response.statusText}`, body);
        throw new Error(`${response.status} ${response.statusText}, see console`);
    }
}