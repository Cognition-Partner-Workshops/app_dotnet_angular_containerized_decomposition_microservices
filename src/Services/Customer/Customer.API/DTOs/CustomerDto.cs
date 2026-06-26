namespace Customer.API.DTOs;

public record CustomerDto(
    int Id,
    string Name,
    string Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string Gender,
    DateTime CreatedDate,
    DateTime UpdatedDate);
