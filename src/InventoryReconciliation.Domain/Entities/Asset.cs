using InventoryReconciliation.Domain.Enums;
using InventoryReconciliation.Domain.ValueObjects;

namespace InventoryReconciliation.Domain.Entities;

public sealed class Asset : Entity
{
    private readonly List<AssetAuditLog> _auditLogs = [];
    private readonly List<AssetVerification> _verifications = [];

    private Asset()
    {
    }

    public Asset(
        string assetTag,
        string? serialNumber,
        string assetType,
        string? manufacturer,
        string? modelNumber,
        AssetLocation location,
        string createdBy)
    {
        AssetTag = assetTag.Trim();
        NormalizedAssetTag = AssetTag.ToUpperInvariant();
        SerialNumber = serialNumber?.Trim();
        NormalizedSerialNumber = SerialNumber?.ToUpperInvariant();
        AssetType = assetType;
        Manufacturer = manufacturer;
        ModelNumber = modelNumber;
        Location = location;
        CreatedBy = createdBy;
        Status = AssetStatus.Active;
    }

    public string AssetTag { get; private set; } = string.Empty;
    public string NormalizedAssetTag { get; private set; } = string.Empty;
    public string? HostName { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? NormalizedSerialNumber { get; private set; }
    public string AssetType { get; private set; } = string.Empty;
    public string? AssetCategory { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? ModelNumber { get; private set; }
    public string? UserName { get; private set; }
    public string? EmployeeId { get; private set; }
    public string? Department { get; private set; }
    public string? CostCenter { get; private set; }
    public string? StickerNumber { get; private set; }
    public AssetLocation Location { get; private set; } = new(null, null, null, null, null);
    public AssetStatus Status { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDisposed { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public DateOnly? WarrantyStartDate { get; private set; }
    public DateOnly? WarrantyEndDate { get; private set; }
    public string? PurchaseOrderNumber { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public string? Remarks { get; private set; }
    public Guid? CurrentSnapshotId { get; private set; }
    public int RowVersion { get; private set; }

    public IReadOnlyCollection<AssetAuditLog> AuditLogs => _auditLogs;
    public IReadOnlyCollection<AssetVerification> Verifications => _verifications;

    public bool IsWarrantyExpired(DateOnly today) =>
        WarrantyEndDate.HasValue && WarrantyEndDate.Value < today;

    public void ApplyImportedFields(ImportedAssetFields fields, Guid snapshotId, string userId)
    {
        var before = CaptureComparableState();

        HostName = fields.HostName;
        SerialNumber = fields.SerialNumber;
        NormalizedSerialNumber = SerialNumber?.Trim().ToUpperInvariant();
        AssetType = fields.AssetType ?? AssetType;
        AssetCategory = fields.AssetCategory ?? AssetCategory;
        Manufacturer = fields.Manufacturer ?? Manufacturer;
        ModelNumber = fields.ModelNumber ?? ModelNumber;
        UserName = fields.UserName ?? UserName;
        EmployeeId = fields.EmployeeId ?? EmployeeId;
        Department = fields.Department ?? Department;
        CostCenter = fields.CostCenter ?? CostCenter;
        StickerNumber = fields.StickerNumber ?? StickerNumber;
        Location = fields.Location ?? Location;
        Status = fields.Status ?? Status;
        IsActive = Status is not AssetStatus.Inactive and not AssetStatus.Retired and not AssetStatus.Disposed;
        IsDisposed = Status is AssetStatus.Disposed;
        PurchaseDate = fields.PurchaseDate ?? PurchaseDate;
        WarrantyStartDate = fields.WarrantyStartDate ?? WarrantyStartDate;
        WarrantyEndDate = fields.WarrantyEndDate ?? WarrantyEndDate;
        PurchaseOrderNumber = fields.PurchaseOrderNumber ?? PurchaseOrderNumber;
        InvoiceNumber = fields.InvoiceNumber ?? InvoiceNumber;
        Remarks = fields.Remarks ?? Remarks;
        CurrentSnapshotId = snapshotId;

        Touch(userId);
        RecordChangedValues(before, CaptureComparableState(), userId, AuditEventType.InventoryImported, "Inventory master import applied.");
    }

    public AssetVerification RecordVerification(PhysicalObservation observation, string userId, string? campaignName)
    {
        var verification = new AssetVerification(Id, observation, userId, campaignName);
        _verifications.Add(verification);
        Touch(userId);
        _auditLogs.Add(AssetAuditLog.ForEvent(Id, AuditEventType.VerificationRecorded, userId, "Physical verification captured."));
        return verification;
    }

    public void AddVarianceAudit(ReconciliationVariance variance, string userId)
    {
        _auditLogs.Add(AssetAuditLog.ForVariance(Id, variance, userId));
    }

    private Dictionary<string, string?> CaptureComparableState() => new()
    {
        [nameof(HostName)] = HostName,
        [nameof(SerialNumber)] = SerialNumber,
        [nameof(AssetType)] = AssetType,
        [nameof(Manufacturer)] = Manufacturer,
        [nameof(ModelNumber)] = ModelNumber,
        [nameof(UserName)] = UserName,
        [nameof(EmployeeId)] = EmployeeId,
        [nameof(Department)] = Department,
        [nameof(CostCenter)] = CostCenter,
        [nameof(StickerNumber)] = StickerNumber,
        [nameof(Status)] = Status.ToString(),
        [nameof(Location)] = Location.DisplayName
    };

    private void RecordChangedValues(
        IReadOnlyDictionary<string, string?> before,
        IReadOnlyDictionary<string, string?> after,
        string userId,
        AuditEventType eventType,
        string remarks)
    {
        foreach (var key in before.Keys)
        {
            if (string.Equals(before[key], after[key], StringComparison.Ordinal))
            {
                continue;
            }

            _auditLogs.Add(AssetAuditLog.ForFieldChange(Id, key, before[key], after[key], userId, eventType, remarks));
        }
    }
}

public sealed record ImportedAssetFields(
    string? HostName,
    string? SerialNumber,
    string? AssetType,
    string? AssetCategory,
    string? Manufacturer,
    string? ModelNumber,
    string? UserName,
    string? EmployeeId,
    string? Department,
    string? CostCenter,
    string? StickerNumber,
    AssetLocation? Location,
    AssetStatus? Status,
    DateOnly? PurchaseDate,
    DateOnly? WarrantyStartDate,
    DateOnly? WarrantyEndDate,
    string? PurchaseOrderNumber,
    string? InvoiceNumber,
    string? Remarks);
