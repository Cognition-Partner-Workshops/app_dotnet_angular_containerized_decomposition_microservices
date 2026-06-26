namespace Identity.Domain.DTOs;

public class AppUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public bool IsEnabled { get; set; }
}
