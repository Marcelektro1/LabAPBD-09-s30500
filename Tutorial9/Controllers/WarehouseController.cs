using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpPost("fulfilled")]
    public async Task<IActionResult> RegisterProductFulfilled([FromBody] ProductWarehouseDto productWarehouseDto)
    {
        if (productWarehouseDto.Amount <= 0) 
            return BadRequest("Amount must be greater than zero");

        WarehouseServiceResult res;
        try
        {
            res = await warehouseService.DoSomethingAsync(productWarehouseDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        if (!res.Success)
        {
            return BadRequest(res.Message);
        }
        
        return Ok(res.Message);
        
    }
    
}