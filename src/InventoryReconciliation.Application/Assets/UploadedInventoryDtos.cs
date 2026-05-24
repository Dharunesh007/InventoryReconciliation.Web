namespace InventoryReconciliation.Application.Assets;

public sealed record UploadedAssetRecord(
    int RowNumber,
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
    string? SentinelStatus);

public sealed record UploadedInventorySnapshot(
    string SourcePath,
    string SourceFileName,
    DateTimeOffset? SourceLastModified,
    int TotalRows,
    int TotalColumns,
    IReadOnlyList<string> Headers,
    IReadOnlyList<UploadedAssetRecord> Assets,
    IReadOnlyList<UploadedColumnProfile> Columns,
    IReadOnlyList<DuplicateValueProfile> DuplicateAssetTags,
    IReadOnlyList<DuplicateValueProfile> DuplicateSerialNumbers,
    IReadOnlyList<DuplicateValueProfile> DuplicateHostNames)
{
    public int ActiveAssets => Assets.Count(asset => string.Equals(asset.AssetStatus, "Active", StringComparison.OrdinalIgnoreCase));
    public int NotActiveAssets => Assets.Count(asset => !string.Equals(asset.AssetStatus, "Active", StringComparison.OrdinalIgnoreCase));
    public int FormulaErrorCells => Columns.Sum(column => column.ErrorCount);
}

public sealed record UploadedColumnProfile(
    string Header,
    int NonBlankCount,
    int UniqueCount,
    int ErrorCount,
    IReadOnlyList<string> Samples);

public sealed record DuplicateValueProfile(string Value, int Count);
