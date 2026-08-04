using MediatR;
using InventoryManagement.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ISender _sender;

        public ProductsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetAllProducts([FromQuery] Application.Features.Products.GetProducts.Query query)
        {
            var response = await _sender.Send(new Application.Features.Products.GetProducts.Query
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                Search = query.Search
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetProductById(int id)
        {
            var response = await _sender.Send
            (
                new Application.Features.Products.GetProductById.Query
                {
                    Id = id
                }
            );

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> AddProduct([FromBody] Application.Features.Products.CreateProduct.Command command)
        {
            var response = await _sender.Send(command);

            return CreatedAtAction(
                    nameof(GetProductById),
                    new { id = response.Id },
                    response
            );
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Application.Features.Products.UpdateProduct.Command command)
        {
            var request = new Application.Features.Products.UpdateProduct.Command
            (
                id,
                command.Name,
                command.SKU,
                command.BaseUnitId,
                command.DefaultSellingPrice,
                command.CategoryId
            );
            var response = await _sender.Send(request);
            return Ok(response);
        }

        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var response = await _sender.Send(
                new Application.Features.Products.DeleteProduct.Command
                {
                    Id = id
                }
            );
            return Ok(response);
        }

        [HttpGet("{productId}/unit-conversions")]
        [Authorize(Policy = AuthorizationPolicies.ReadProducts)]
        public async Task<IActionResult> GetProductUnitConversions(int productId)
        {
            var response = await _sender.Send(
                new Application.Features.ProductUnitConversions.GetProductUnitConversions.Query
                {
                    ProductId = productId
                });

            return Ok(response);
        }

        [HttpPost("{productId}/unit-conversions")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> CreateProductUnitConversion(
            int productId,
            [FromBody] Application.Features.ProductUnitConversions.CreateProductUnitConversion.Command command)
        {
            var request = new Application.Features.ProductUnitConversions.CreateProductUnitConversion.Command
            {
                ProductId = productId,
                UnitId = command.UnitId,
                FactorToBaseUnit = command.FactorToBaseUnit
            };

            var response = await _sender.Send(request);

            return CreatedAtAction(
                nameof(GetProductUnitConversions),
                new { productId },
                response);
        }

        [HttpPut("{productId}/unit-conversions/{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> UpdateProductUnitConversion(
            int productId,
            int id,
            [FromBody] Application.Features.ProductUnitConversions.UpdateProductUnitConversion.Command command)
        {
            var request = new Application.Features.ProductUnitConversions.UpdateProductUnitConversion.Command(
                productId,
                id,
                command.FactorToBaseUnit,
                command.IsActive);

            var response = await _sender.Send(request);

            return Ok(response);
        }

        [HttpPatch("{productId}/unit-conversions/{id}/deactivate")]
        [Authorize(Policy = AuthorizationPolicies.ManageProducts)]
        public async Task<IActionResult> DeactivateProductUnitConversion(
            int productId,
            int id)
        {
            var response = await _sender.Send(
                new Application.Features.ProductUnitConversions.DeactivateProductUnitConversion.Command
                {
                    ProductId = productId,
                    Id = id
                });

            return Ok(response);
        }
    }
}
