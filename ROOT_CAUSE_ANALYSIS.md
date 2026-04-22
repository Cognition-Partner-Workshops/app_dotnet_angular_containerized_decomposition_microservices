# Root Cause Analysis: Notification Currency Display Bug

## Bug Summary

Order confirmation notification emails display incorrect monetary amounts. A $149.99 order shows as $1.50 in the email preview.

## Root Cause

The `FormatCurrency` method in `NotificationRenderer.cs` divides the amount by 100, assuming the input value is in **cents** (integer representation):

```csharp
private static string FormatCurrency(decimal amount)
{
    // The OrderPlacedEvent.TotalAmount is transmitted in cents (integer
    // representation) to avoid floating-point precision issues across
    // service boundaries. Convert back to dollars for display.
    var dollars = amount / 100m;
    return dollars.ToString("C2");
}
```

However, the `OrderPlacedEvent` contract defines `TotalAmount` as a `decimal` representing **dollars**, not cents:

```csharp
public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,  // Already in dollars (e.g., 149.99)
    DateTime PlacedAt
);
```

The division `149.99 / 100 = 1.4999` then formats to `$1.50`, which is off by a factor of 100.

## Why It Happened

The misleading comment in `FormatCurrency` claimed the amount was "transmitted in cents" but neither the shared contract (`OrderPlacedEvent`), the HTTP DTO (`OrderPlacedEventDto`), nor the domain entity (`OrderNotification.OrderTotal`) treat the value as cents. The entire data pipeline passes the amount as a dollar-denominated decimal. The `/100m` division was incorrect from the start.

## Fix

Removed the erroneous `/100m` division so the amount formats directly:

```csharp
private static string FormatCurrency(decimal amount)
{
    return amount.ToString("C2");
}
```

## Files Changed

| File | Change |
|------|--------|
| `src/Services/Notification/Notification.API/Services/NotificationRenderer.cs` | Removed `/100m` division in `FormatCurrency` |
| `src/Services/Notification/Notification.API/Notification.API.csproj` | Fixed project reference paths to `Shared.*` projects |
| `src/global.json` | Updated SDK version to match installed .NET 10 RC |

## Verification

- **Before fix**: POST `totalAmount: 149.99` -> email preview shows $1.50
- **After fix**: POST `totalAmount: 149.99` -> email preview shows $149.99
