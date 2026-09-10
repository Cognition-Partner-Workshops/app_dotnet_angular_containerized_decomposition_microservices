// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

using System.Security.Claims;
using Identity.Domain.Interfaces;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.API.Services
{
    public class UserIdAccessor(IHttpContextAccessor httpContextAccessor) : IUserIdAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public string? GetCurrentUserId() => _httpContextAccessor.HttpContext?.User.FindFirstValue(Claims.Subject);
    }
}
