using MediatR;
using InventoryManagement.Application.Authorization;
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

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManageSalesInvoices)]
        public async Task<IActionResult> GetSalesInvoices(
            [FromQuery] Application.Features.SalesInvoices.GetSalesInvoices.Query query)
        {
            return Ok(await _sender.Send(query));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageSalesInvoices)]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.SalesInvoices.GetSalesInvoiceById.Query
                {
                    Id = id
                }));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageSalesInvoices)]
        public async Task<IActionResult> CreateDraft(
            [FromBody] Application.Features.SalesInvoices.CreateSalesInvoice.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPost("from-challans")]
        [Authorize(Policy = AuthorizationPolicies.ManageSalesInvoices)]
        public async Task<IActionResult> CreateFromChallans(
            [FromBody] Application.Features.SalesInvoices.CreateFromChallans.Command command)
        {
            var response = await _sender.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageSalesInvoices)]
        public async Task<IActionResult> UpdateDraft(
            int id,
            [FromBody] Application.Features.SalesInvoices.UpdateSalesInvoice.Command command)
        {
            return Ok(await _sender.Send(command with { Id = id }));
        }

        [HttpPost("{id}/post")]
        [Authorize(Policy = AuthorizationPolicies.ManageSalesInvoices)]
        public async Task<IActionResult> PostDirectInvoice(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.SalesInvoices.PostSalesInvoice.Command
                {
                    Id = id
                }));
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> CancelInvoice(int id)
        {
            return Ok(await _sender.Send(
                new Application.Features.SalesInvoices.CancelSalesInvoice.Command
                {
                    Id = id
                }));
        }
    }
}
