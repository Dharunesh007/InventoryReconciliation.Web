namespace InventoryReconciliation.Application.Assets;

public sealed record AssetFieldChange(
    string FieldName,
    string ChangeType,
    string? PreviousValue,
    string? NewValue);

public sealed record AssetEditRequest(
    string SourceAssetTag,
    string AssetTag,
    string? SerialNumber,
    string? HostName,
    string? UserName,
    string? EmployeeId,
    string? Department,
    string? AssetFloor,
    string? AssetType,
    string? AssetCategory,
    string? SingleOrGroup,
    string? AssetStatus,
    string? Manufacturer,
    string? ModelNumber,
    DateOnly? WarrantyStart,
    DateOnly? WarrantyEnd,
    string? PurchaseOrderNumber,
    DateOnly? PurchaseOrderDate,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    string? Remarks,
    string? WindowsPatch,
    string? SentinelStatus)
{
    public AssetFieldChange[] Changes { get; init; } = [];
}

public sealed record AssetEditEntry(
    string SourceAssetTag,
    string AssetTag,
    string? SerialNumber,
    string? HostName,
    string? UserName,
    string? EmployeeId,
    string? Department,
    string? AssetFloor,
    string? AssetType,
    string? AssetCategory,
    string? SingleOrGroup,
    string? AssetStatus,
    string? Manufacturer,
    string? ModelNumber,
    DateOnly? WarrantyStart,
    DateOnly? WarrantyEnd,
    string? PurchaseOrderNumber,
    DateOnly? PurchaseOrderDate,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    string? Remarks,
    string? WindowsPatch,
    string? SentinelStatus,
    DateTimeOffset SavedAtUtc)
{
    public AssetFieldChange[] Changes { get; init; } = [];
}
