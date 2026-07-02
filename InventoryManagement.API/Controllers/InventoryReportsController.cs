using CurrentStock = InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;
using ProductStockLedger = InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger;
using PurchaseRegister = InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister;
using SalesRegister = InventoryManagement.Application.Features.InventoryReports.GetSalesRegister;
using GrossProfit = InventoryManagement.Application.Features.InventoryReports.GetGrossProfit;
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

    [HttpGet("purchase-register")]
    public async Task<IActionResult> GetPurchaseRegister(
        [FromQuery] PurchaseRegister.Query query)
    {
        return Ok(await _sender.Send(query));
    }

    [HttpGet("sales-register")]
    public async Task<IActionResult> GetSalesRegister(
        [FromQuery] SalesRegister.Query query)
    {
        return Ok(await _sender.Send(query));
    }

    [HttpGet("gross-profit")]
    public async Task<IActionResult> GetGrossProfit(
        [FromQuery] GrossProfit.Query query)
    {
        return Ok(await _sender.Send(query));
    }
}
