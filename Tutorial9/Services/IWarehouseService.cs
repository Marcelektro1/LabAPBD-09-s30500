using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<WarehouseServiceResult> DoSomethingAsync(ProductWarehouseDto product);
    Task<WarehouseServiceResult> ProcedureAsync(ProductWarehouseDto product);
}