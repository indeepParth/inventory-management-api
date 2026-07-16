using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan
{
    public class Handler : IRequestHandler<Command, DeliveryChallanResponse>
    {
        private readonly IDeliveryChallanRepository _challans;
        private readonly ICustomerRepository _customers;
        private readonly IDriverRepository _drivers;
        private readonly IProductRepository _products;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            IDeliveryChallanRepository challans,
            ICustomerRepository customers,
            IDriverRepository drivers,
            IProductRepository products,
            ICurrentUserService currentUser)
        {
            _challans = challans;
            _customers = customers;
            _drivers = drivers;
            _products = products;
            _currentUser = currentUser;
        }

        public async Task<DeliveryChallanResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var number = request.ChallanNumber.Trim();
            if (await _challans.ChallanNumberExistsAsync(number, cancellationToken))
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

            var now = DateTime.UtcNow;
            var challan = new DeliveryChallan
            {
                ChallanNumber = number,
                CustomerId = customer.Id,
                Customer = customer,
                ChallanDate = request.ChallanDate,
                Status = DeliveryChallanStatus.Draft,
                VehicleNumber = Normalize(request.VehicleNumber),
                DriverId = driver?.Id,
                Driver = driver,
                DriverName = Normalize(request.DriverName),
                DeliveryFromAddress = request.DeliveryFromAddress.Trim(),
                DeliveryAddress = request.DeliveryAddress.Trim(),
                DeliveryCharge = request.DeliveryCharge,
                IsDeliveryChargePaid = false,
                Notes = Normalize(request.Notes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBy = _currentUser.Username
            };

            foreach (var input in request.Items)
            {
                var product = await _products.GetProductByIdAsync(
                    input.ProductId, cancellationToken);
                if (product is null)
                    throw new NotFoundException($"Product {input.ProductId} not found.");

                challan.Items.Add(new DeliveryChallanItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = input.Quantity
                });
            }

            await _challans.AddAsync(challan, cancellationToken);
            await _challans.SaveChangesAsync(cancellationToken);
            return challan.ToResponse();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
