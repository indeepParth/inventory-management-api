using MediatR;
using InventoryManagement.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly ISender _sender;

        public DriversController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ReadDrivers)]
        public async Task<IActionResult> GetDrivers(
            [FromQuery] Application.Features.Drivers.GetDrivers.Query query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ReadDrivers)]
        public async Task<IActionResult> GetDriverById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Drivers.GetDriverById.Query { Id = id }));
        }

        [HttpGet("{id}/deliveries")]
        [Authorize(Policy = AuthorizationPolicies.ReadDrivers)]
        public async Task<IActionResult> GetDriverDeliveries(
            int id,
            [FromQuery] Application.Features.Drivers.GetDriverDeliveries.Query query)
        {
            query.DriverId = id;
            return Ok(await _sender.Send(query));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageDrivers)]
        public async Task<IActionResult> CreateDriver(
            [FromBody] Application.Features.Drivers.CreateDriver.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetDriverById), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageDrivers)]
        public async Task<IActionResult> UpdateDriver(
            int id,
            [FromBody] Application.Features.Drivers.UpdateDriver.Command command)
        {
            return Ok(await _sender.Send(command with { Id = id }));
        }

        [HttpPatch("{id}/deactivate")]
        [Authorize(Policy = AuthorizationPolicies.ManageDrivers)]
        public async Task<IActionResult> DeactivateDriver(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Drivers.DeactivateDriver.Command { Id = id }));
        }
    }
}
