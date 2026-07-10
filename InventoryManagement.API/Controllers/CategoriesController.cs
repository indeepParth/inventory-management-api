using MediatR;
using InventoryManagement.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ISender _sender;

        public CategoriesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetCategories()
        {
            var response = await _sender.Send(new Application.Features.Categories.GetCategories.Query());

            return Ok(response);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var response = await _sender.Send(
                new Application.Features.Categories.GetCategoryById.Query
                {
                    Id = id
                });

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> CreateCategory([FromBody] Application.Features.Categories.CreateCategory.Command command)
        {
            var response = await _sender.Send(command);

            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = response.Id },
                response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Application.Features.Categories.UpdateCategory.Command command)
        {
            var request = new Application.Features.Categories.UpdateCategory.Command(
                id,
                command.Name,
                command.Description,
                command.IsActive);

            var response = await _sender.Send(request);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var response = await _sender.Send(
                new Application.Features.Categories.DeleteCategory.Command
                {
                    Id = id
                });

            return Ok(response);
        }
    }
}
