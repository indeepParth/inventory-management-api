using InventoryManagement.Application.Features.Payments.CreatePayment;
using InventoryManagement.Application.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ISender _sender;
        public PaymentsController(ISender sender) => _sender = sender;

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ViewPayments)]
        public async Task<IActionResult> Get(
            [FromQuery] Application.Features.Payments.GetPayments.Query query) =>
            Ok(await _sender.Send(query));

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CreateCustomerReceipts)]
        public async Task<IActionResult> Create([FromBody] Command command)
        {
            var response = await _sender.Send(command);
            return Created($"/api/payments/{response.Id}", response);
        }

        [HttpPost("{id}/reverse")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> Reverse(
            int id,
            [FromBody] Application.Features.Payments.ReversePayment.Command command) =>
            Ok(await _sender.Send(command with { Id = id }));
    }
}
