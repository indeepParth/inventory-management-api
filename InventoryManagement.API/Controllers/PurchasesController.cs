using MediatR;
using InventoryManagement.Application.Authorization;
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

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagePurchases)]
        public async Task<IActionResult> GetPurchases(
            [FromQuery] Application.Features.Purchases.GetPurchases.Query query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagePurchases)]
        public async Task<IActionResult> GetPurchaseById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Purchases.GetPurchaseById.Query { Id = id }));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagePurchases)]
        public async Task<IActionResult> CreatePurchase(
            [FromBody] Application.Features.Purchases.CreatePurchase.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetPurchaseById), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagePurchases)]
        public async Task<IActionResult> UpdatePurchase(
            int id,
            [FromBody] Application.Features.Purchases.UpdatePurchase.Command command)
        {
            return Ok(await _sender.Send(command with { Id = id }));
        }

        [HttpPost("{id}/post")]
        [Authorize(Policy = AuthorizationPolicies.ManagePurchases)]
        public async Task<IActionResult> PostPurchase(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Purchases.PostPurchase.Command { Id = id }));
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> CancelPurchase(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Purchases.CancelPurchase.Command { Id = id }));
        }
    }
}
