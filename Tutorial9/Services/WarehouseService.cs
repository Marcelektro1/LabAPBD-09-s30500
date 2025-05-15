using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class WarehouseService(IConfiguration configuration) : IWarehouseService
{
    public async Task<WarehouseServiceResult> DoSomethingAsync(ProductWarehouseDto product)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = """
                                  SELECT 1 
                                  FROM Warehouse
                                  WHERE Warehouse.IdWarehouse = @IdWarehouse;
                                  """;
            command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        
            var warehouseExists = await command.ExecuteScalarAsync();
            if (warehouseExists == null)
            {
                throw new ArgumentException("Warehouse by provided id does not exist.");
            }
        
            command.Parameters.Clear();
            
            command.CommandText = """
                                  SELECT Product.Price
                                  FROM Product
                                  WHERE Product.IdProduct = @IdProduct;
                                  """;
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
        
            var productExists = await command.ExecuteScalarAsync();
            if (productExists == null)
            {
                throw new ArgumentException("Product by provided id does not exist.");
            }
            
            var producePrice = Convert.ToInt32(productExists);
            
            command.Parameters.Clear();
        

            command.CommandText = """
                                  SELECT [Order].IdOrder 
                                  FROM [Order]
                                  WHERE [Order].IdProduct = @IdProduct AND [Order].Amount = @Amount AND [Order].CreatedAt < @ReqCreated;
                                  """;
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command.Parameters.AddWithValue("@Amount", product.Amount);
            command.Parameters.AddWithValue("@ReqCreated", product.CreatedAt);
            
            var orderExists = await command.ExecuteScalarAsync();
            if (orderExists == null)
            {
                throw new ArgumentException("Past order with provided product and amount does not exist.");
            }
            
            var orderId = Convert.ToInt32(orderExists);
            
            command.Parameters.Clear();

            command.CommandText = """
                                  SELECT 1
                                  FROM Product_Warehouse
                                  WHERE Product_Warehouse.IdOrder = @IdOrder;
                                  """;
            command.Parameters.AddWithValue("@IdOrder", orderId);
            var produceWarehouseExists = await command.ExecuteScalarAsync();
            if (produceWarehouseExists != null)
            {
                await transaction.RollbackAsync();
                return new WarehouseServiceResult(false, "Order is already fulfilled.");
            }
            
            command.Parameters.Clear();

            command.CommandText = """
                                  UPDATE [Order]
                                  SET [Order].FulfilledAt = @FulfilledAt
                                  WHERE [Order].IdOrder = @IdOrder;
                                  """;
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now); // task says "current system time"... why? (I'd go for using product.CreatedAt)
            command.Parameters.AddWithValue("@IdOrder", orderId);
            
            var updateFulfilledAt = await command.ExecuteNonQueryAsync();
            if (updateFulfilledAt == 0)
            {
                await transaction.RollbackAsync();
                throw new InvalidDataException("No order to be fulfilled."); // should never happen
            }
            
            command.Parameters.Clear();


            command.CommandText = """
                                  INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                  VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                                  SELECT CAST(SCOPE_IDENTITY() AS INT);
                                  """;
            command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", product.Amount);
            command.Parameters.AddWithValue("@Price", product.Amount * producePrice); // TODO: check if that's what they want xD
            command.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);
            
            var inserted = await command.ExecuteScalarAsync();

            if (inserted == null)
            {
                await transaction.RollbackAsync();
                throw new InvalidDataException("Failed to mark order as fulfilled.");
            }
            
            var insertedId = Convert.ToInt32(inserted);
            
            await transaction.CommitAsync();
            return new WarehouseServiceResult(true, insertedId.ToString());

        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task<WarehouseServiceResult> ProcedureAsync(ProductWarehouseDto product)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "NazwaProcedury";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@Id", 2);
        
        await command.ExecuteNonQueryAsync();
        
        throw new NotImplementedException();
    }
}