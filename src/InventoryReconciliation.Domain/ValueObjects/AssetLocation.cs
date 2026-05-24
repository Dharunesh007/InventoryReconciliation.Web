namespace InventoryReconciliation.Domain.ValueObjects;

public sealed record AssetLocation(
    string? Region,
    string? Location,
    string? Building,
    string? Floor,
    string? SeatOrCubicle)
{
    public string DisplayName =>
        string.Join(" / ", new[] { Region, Location, Building, Floor, SeatOrCubicle }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
}
