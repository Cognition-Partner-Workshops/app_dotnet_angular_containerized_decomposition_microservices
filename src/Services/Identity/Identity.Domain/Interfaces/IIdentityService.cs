using Identity.Domain.DTOs;

namespace Identity.Domain.Interfaces;

public interface IIdentityService
{
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<AppUserDto?> GetUserByIdAsync(Guid id);
    Task<IEnumerable<AppUserDto>> GetAllUsersAsync();
}
