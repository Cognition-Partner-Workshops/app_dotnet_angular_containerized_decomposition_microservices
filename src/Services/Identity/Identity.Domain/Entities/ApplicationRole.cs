using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

/// <summary>
/// Identity role for the Identity microservice. String GUID primary key inherited
/// from <see cref="IdentityRole"/>.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public string? Description { get; set; }
}
