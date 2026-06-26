using System.ComponentModel.DataAnnotations;

namespace Customer.Domain.Entities;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Email { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    public Gender Gender { get; set; }

    [MaxLength(40)]
    public string? CreatedBy { get; set; }

    [MaxLength(40)]
    public string? UpdatedBy { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime CreatedDate { get; set; }
}
