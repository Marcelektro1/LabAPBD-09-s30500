namespace Tutorial9.Services;

public class WarehouseServiceResult(bool success, string message)
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = message;
}