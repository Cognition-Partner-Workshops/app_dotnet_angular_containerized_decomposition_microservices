using System.ComponentModel.DataAnnotations;

namespace Customer.API.ViewModels;

public class CustomerVM
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [Required]
    public string? Gender { get; set; }
}
