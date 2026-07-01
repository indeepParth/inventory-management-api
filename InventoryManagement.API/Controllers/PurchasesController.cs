using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PurchasesController : ControllerBase
    {
        private readonly ISender _sender;

        public PurchasesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPurchaseById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Purchases.GetPurchaseById.Query { Id = id }));
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchase(
            [FromBody] Application.Features.Purchases.CreatePurchase.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetPurchaseById), new { id = response.Id }, response);
        }
    }
}
