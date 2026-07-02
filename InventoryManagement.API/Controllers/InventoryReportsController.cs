using CurrentStock = InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;
using ProductStockLedger = InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory-reports")]
public class InventoryReportsController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("current-stock")]
    public async Task<IActionResult> GetCurrentStock([FromQuery] CurrentStock.Query query)
    {
        return Ok(await _sender.Send(query));
    }

    [HttpGet("products/{productId}/ledger")]
    public async Task<IActionResult> GetProductStockLedger(
        int productId,
        [FromQuery] ProductStockLedger.Query query)
    {
        query.ProductId = productId;
        return Ok(await _sender.Send(query));
    }
}
