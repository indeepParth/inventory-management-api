using MediatR;

namespace InventoryManagement.Application.Features.CompanyProfile.UpdateCompanyProfile
{
    public sealed record Command(
        string CompanyName,
        string? Address,
        string? GstNumber,
        string? Email,
        string? Phone,
        string? Website) : IRequest<CompanyProfileResponse>;
}
