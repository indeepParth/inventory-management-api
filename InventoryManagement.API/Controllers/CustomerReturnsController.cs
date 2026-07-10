using MediatR;
using InventoryManagement.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/customer-returns")]
    public class CustomerReturnsController : ControllerBase
    {
        private readonly ISender _sender;

        public CustomerReturnsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageCustomerReturns)]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.CustomerReturns
                    .GetCustomerReturnById.Query { Id = id }));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageCustomerReturns)]
        public async Task<IActionResult> Create(
            [FromBody] Application.Features.CustomerReturns
                .CreateCustomerReturn.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }

        [HttpPost("{id}/post")]
        [Authorize(Policy = AuthorizationPolicies.ManageCustomerReturns)]
        public async Task<IActionResult> Post(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.CustomerReturns
                    .PostCustomerReturn.Command { Id = id }));
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> Cancel(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.CustomerReturns
                    .CancelCustomerReturn.Command { Id = id }));
        }
    }
}
