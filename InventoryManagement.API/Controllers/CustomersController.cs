using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ISender _sender;

        public CustomersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Customers.GetCustomerById.Query { Id = id }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(
            [FromBody] Application.Features.Customers.CreateCustomer.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetCustomerById), new { id = response.Id }, response);
        }
    }
}
