using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/sales-invoices")]
    public class SalesInvoicesController : ControllerBase
    {
        private readonly ISender _sender;

        public SalesInvoicesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.SalesInvoices.GetSalesInvoiceById.Query
                {
                    Id = id
                }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateDraft(
            [FromBody] Application.Features.SalesInvoices.CreateSalesInvoice.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
    }
}
