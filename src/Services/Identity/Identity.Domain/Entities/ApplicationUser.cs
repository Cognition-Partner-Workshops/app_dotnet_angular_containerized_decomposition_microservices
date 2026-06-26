using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

/// <summary>
/// Identity user for the Identity microservice. Mirrors the monolith's user model:
/// string GUID primary key (inherited from <see cref="IdentityUser"/>), username/email,
/// and password hashing handled by ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public bool IsEnabled { get; set; } = true;
}
