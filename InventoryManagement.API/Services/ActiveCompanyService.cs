using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;

namespace InventoryManagement.API.Services
{
    public class ActiveCompanyService : IActiveCompanyService
    {
        private const string CompanyIdHeaderName = "X-Company-Id";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActiveCompanyService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int CompanyId
        {
            get
            {
                var headers = _httpContextAccessor.HttpContext?.Request.Headers;

                if (headers is null ||
                    !headers.TryGetValue(CompanyIdHeaderName, out var companyHeader) ||
                    !int.TryParse(companyHeader.FirstOrDefault(), out var companyId))
                {
                    throw new ForbiddenException("Active company is required.");
                }

                return companyId;
            }
        }
    }
}
