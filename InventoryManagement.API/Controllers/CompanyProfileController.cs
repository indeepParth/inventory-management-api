using InventoryManagement.Application.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ApiController]
    [Route("api/company-profile")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly ISender _sender;

        public CompanyProfileController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyProfile()
        {
            return Ok(await _sender.Send(
                new Application.Features.CompanyProfile.GetCompanyProfile.Query()));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCompanyProfile(
            [FromBody] Application.Features.CompanyProfile.UpdateCompanyProfile.Command command)
        {
            return Ok(await _sender.Send(command));
        }
    }
}
