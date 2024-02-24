namespace KoTi.ResponseModels;

public class ListWithTotal<T>
{
    public int Total { get; set; }
    
    public IEnumerable<T> Data { get; set; } 
}
