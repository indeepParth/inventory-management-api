using MediatR;
using InventoryManagement.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/delivery-challans")]
    public class DeliveryChallansController : ControllerBase
    {
        private readonly ISender _sender;

        public DeliveryChallansController(ISender sender) => _sender = sender;

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManageDeliveryChallans)]
        public async Task<IActionResult> GetDeliveryChallans(
            [FromQuery] Application.Features.DeliveryChallans.GetDeliveryChallans.Query query) =>
            Ok(await _sender.Send(query));

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageDeliveryChallans)]
        public async Task<IActionResult> GetDeliveryChallanById(int id) =>
            Ok(await _sender.Send(
                new Application.Features.DeliveryChallans.GetDeliveryChallanById.Query
                {
                    Id = id
                }));

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageDeliveryChallans)]
        public async Task<IActionResult> CreateDraft(
            [FromBody] Application.Features.DeliveryChallans.CreateDeliveryChallan.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(
                nameof(GetDeliveryChallanById),
                new { id = response.Id },
                response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageDeliveryChallans)]
        public async Task<IActionResult> UpdateDraft(
            int id,
            [FromBody] Application.Features.DeliveryChallans.UpdateDeliveryChallan.Command command) =>
            Ok(await _sender.Send(command with { Id = id }));

        [HttpPost("{id}/post")]
        [Authorize(Policy = AuthorizationPolicies.ManageDeliveryChallans)]
        public async Task<IActionResult> Post(int id) =>
            Ok(await _sender.Send(
                new Application.Features.DeliveryChallans.PostDeliveryChallan.Command
                {
                    Id = id
                }));

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> Cancel(int id) =>
            Ok(await _sender.Send(
                new Application.Features.DeliveryChallans.CancelDeliveryChallan.Command
                {
                    Id = id
                }));
    }
}
