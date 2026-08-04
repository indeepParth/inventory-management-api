using InventoryManagement.Application.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UnitsController : ControllerBase
    {
        private readonly ISender _sender;

        public UnitsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetUnits()
        {
            var response = await _sender.Send(new Application.Features.Units.GetUnits.Query());

            return Ok(response);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetUnitById(int id)
        {
            var response = await _sender.Send(
                new Application.Features.Units.GetUnitById.Query
                {
                    Id = id
                });

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> CreateUnit([FromBody] Application.Features.Units.CreateUnit.Command command)
        {
            var response = await _sender.Send(command);

            return CreatedAtAction(
                nameof(GetUnitById),
                new { id = response.Id },
                response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> UpdateUnit(int id, [FromBody] Application.Features.Units.UpdateUnit.Command command)
        {
            var request = new Application.Features.Units.UpdateUnit.Command(
                id,
                command.Name,
                command.ShortName,
                command.FactorToBaseUnit,
                command.BaseUnitId,
                command.IsActive);

            var response = await _sender.Send(request);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            var response = await _sender.Send(
                new Application.Features.Units.DeleteUnit.Command
                {
                    Id = id
                });

            return Ok(response);
        }
    }
}
