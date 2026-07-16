using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.UpdateDeliveryChallan
{
    public class Handler : IRequestHandler<Command, DeliveryChallanResponse>
    {
        private readonly IDeliveryChallanRepository _challans;
        private readonly ICustomerRepository _customers;
        private readonly IDriverRepository _drivers;
        private readonly IProductRepository _products;

        public Handler(
            IDeliveryChallanRepository challans,
            ICustomerRepository customers,
            IDriverRepository drivers,
            IProductRepository products)
        {
            _challans = challans;
            _customers = customers;
            _drivers = drivers;
            _products = products;
        }

        public async Task<DeliveryChallanResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var challan = await _challans.GetForUpdateAsync(
                request.Id, cancellationToken) ??
                throw new NotFoundException("Delivery challan not found.");
            if (challan.Status != DeliveryChallanStatus.Draft)
                throw new BadRequestException("Only Draft delivery challans may be edited.");

            var number = request.ChallanNumber.Trim();
            if (await _challans.ChallanNumberExistsForOtherAsync(
                number, challan.Id, cancellationToken))
                throw new BadRequestException("Challan number already exists.");

            var customer = await _customers.GetByIdAsync(
                request.CustomerId, cancellationToken);
            if (customer is null)
                throw new NotFoundException("Customer not found.");
            if (!customer.IsActive)
                throw new BadRequestException("Customer is inactive.");

            Driver? driver = null;
            if (request.DriverId.HasValue)
            {
                driver = await _drivers.GetByIdAsync(
                    request.DriverId.Value, cancellationToken);
                if (driver is null)
                    throw new NotFoundException("Driver not found.");
                if (!driver.IsActive)
                    throw new BadRequestException("Driver is inactive.");
            }

            var replacementItems = new List<DeliveryChallanItem>();
            foreach (var input in request.Items)
            {
                var product = await _products.GetProductByIdAsync(
                    input.ProductId, cancellationToken);
                if (product is null)
                    throw new NotFoundException($"Product {input.ProductId} not found.");
                replacementItems.Add(new DeliveryChallanItem
                {
                    DeliveryChallanId = challan.Id,
                    ProductId = product.Id,
                    Product = product,
                    Quantity = input.Quantity
                });
            }

            challan.ChallanNumber = number;
            challan.CustomerId = customer.Id;
            challan.Customer = customer;
            challan.ChallanDate = request.ChallanDate;
            challan.VehicleNumber = Normalize(request.VehicleNumber);
            challan.DriverId = driver?.Id;
            challan.Driver = driver;
            challan.DriverName = Normalize(request.DriverName);
            challan.DeliveryFromAddress = request.DeliveryFromAddress.Trim();
            challan.DeliveryAddress = request.DeliveryAddress.Trim();
            challan.DeliveryCharge = request.DeliveryCharge;
            challan.Notes = Normalize(request.Notes);
            challan.UpdatedAtUtc = DateTime.UtcNow;
            _challans.RemoveItems(challan.Items);
            challan.Items.Clear();
            foreach (var item in replacementItems) challan.Items.Add(item);

            await _challans.SaveChangesAsync(cancellationToken);
            return challan.ToResponse();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
