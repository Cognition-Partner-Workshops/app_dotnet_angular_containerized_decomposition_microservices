using System.ComponentModel.DataAnnotations;

namespace Order.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string? CreatedBy { get; set; }

    [MaxLength(40)]
    public string? UpdatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }
}
