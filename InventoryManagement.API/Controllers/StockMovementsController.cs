using InventoryManagement.Application.Features.StockMovements.GetStockMovements;
using InventoryManagement.Application.Authorization;
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
        [Authorize(Policy = AuthorizationPolicies.ViewStockMovements)]
        public async Task<IActionResult> Get([FromQuery] Query query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpPost("damage")]
        [Authorize(Policy = AuthorizationPolicies.RecordStockDamage)]
        public async Task<IActionResult> RecordDamage(
            [FromBody] Application.Features.StockMovements
                .RecordDamage.Command command)
        {
            return Ok(await _sender.Send(command));
        }

        [HttpPost("adjustment")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> RecordAdjustment(
            [FromBody] Application.Features.StockMovements
                .RecordAdjustment.Command command)
        {
            return Ok(await _sender.Send(command));
        }

        [HttpPost("{id}/reverse")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> Reverse(
            int id,
            [FromBody] Application.Features.StockMovements
                .ReverseManualCorrection.Command command)
        {
            command.Id = id;
            return Ok(await _sender.Send(command));
        }
    }
}
