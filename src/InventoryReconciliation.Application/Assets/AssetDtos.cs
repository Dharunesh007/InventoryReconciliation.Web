using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Application.Assets;

public sealed record AssetListItemDto(
    Guid Id,
    string AssetTag,
    string? SerialNumber,
    string? HostName,
    string AssetType,
    string? Manufacturer,
    string? ModelNumber,
    string? UserName,
    string? EmployeeId,
    string? Department,
    string Location,
    AssetStatus Status,
    bool WarrantyExpired,
    int OpenVarianceCount);

public sealed record AssetDetailDto(
    Guid Id,
    string AssetTag,
    string? SerialNumber,
    string? HostName,
    string AssetType,
    string? Manufacturer,
    string? ModelNumber,
    string? UserName,
    string? EmployeeId,
    string? Department,
    string? CostCenter,
    string Location,
    AssetStatus Status,
    DateOnly? PurchaseDate,
    DateOnly? WarrantyEndDate,
    IReadOnlyCollection<AssetAuditDto> AuditTimeline);

public sealed record AssetAuditDto(
    DateTimeOffset CreatedAtUtc,
    string EventType,
    string? FieldName,
    string? PreviousValue,
    string? NewValue,
    string ChangedBy,
    string Remarks);
