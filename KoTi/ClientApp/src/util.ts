export const errorMessage = (e: unknown): string => {
    if (e instanceof Error) {
        return e.message;
    }
    return 'Unknown error';
};
