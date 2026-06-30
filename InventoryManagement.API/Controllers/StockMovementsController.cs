using InventoryManagement.Application.Features.StockMovements.GetStockMovements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/stock-movements")]
    public class StockMovementsController : ControllerBase
    {
        private readonly ISender _sender;

        public StockMovementsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Query query)
        {
            return Ok(await _sender.Send(query));
        }
    }
}
