export default interface ListWithTotal<T> {
    total: number;
    data: T[];
}