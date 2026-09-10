namespace Order.Domain.Models;

/// <summary>
/// The slice of another context's record this service is allowed to show: an id and a display name.
/// </summary>
public record RelatedParty(string Id, string? Name, string? Email = null);
