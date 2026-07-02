using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : ControllerBase
    {
        private readonly ISender _sender;

        public SuppliersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] Application.Features.Suppliers.GetSuppliers.Query query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Suppliers.GetSupplierById.Query { Id = id }));
        }

        [HttpGet("{id}/statement")]
        public async Task<IActionResult> GetStatement(
            int id,
            [FromQuery] Application.Features.Statements.SupplierStatement.Query query)
        {
            query.SupplierId = id;
            return Ok(await _sender.Send(query));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier(
            [FromBody] Application.Features.Suppliers.CreateSupplier.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetSupplierById), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(
            int id,
            [FromBody] Application.Features.Suppliers.UpdateSupplier.Command command)
        {
            var request = command with { Id = id };
            return Ok(await _sender.Send(request));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.Suppliers.DeleteSupplier.Command { Id = id }));
        }
    }
}
