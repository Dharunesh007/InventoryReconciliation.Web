using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Domain.ValueObjects;

public sealed record PhysicalObservation
{
    private PhysicalObservation()
    {
        Location = new AssetLocation(null, null, null, null, null);
    }

    public PhysicalObservation(
        string? assetTag,
        string? serialNumber,
        string? hostName,
        string? userName,
        string? employeeId,
        string? department,
        AssetLocation location,
        string? assetType,
        string? stickerNumber,
        string? condition,
        AssetStatus physicalStatus,
        bool stickerPresent,
        bool isPhysicallyFound,
        string? remarks,
        decimal? latitude = null,
        decimal? longitude = null,
        string? deviceFingerprint = null)
    {
        AssetTag = assetTag;
        SerialNumber = serialNumber;
        HostName = hostName;
        UserName = userName;
        EmployeeId = employeeId;
        Department = department;
        Location = location;
        AssetType = assetType;
        StickerNumber = stickerNumber;
        Condition = condition;
        PhysicalStatus = physicalStatus;
        StickerPresent = stickerPresent;
        IsPhysicallyFound = isPhysicallyFound;
        Remarks = remarks;
        Latitude = latitude;
        Longitude = longitude;
        DeviceFingerprint = deviceFingerprint;
    }

    public string? AssetTag { get; init; }
    public string? SerialNumber { get; init; }
    public string? HostName { get; init; }
    public string? UserName { get; init; }
    public string? EmployeeId { get; init; }
    public string? Department { get; init; }
    public AssetLocation Location { get; init; }
    public string? AssetType { get; init; }
    public string? StickerNumber { get; init; }
    public string? Condition { get; init; }
    public AssetStatus PhysicalStatus { get; init; }
    public bool StickerPresent { get; init; }
    public bool IsPhysicallyFound { get; init; }
    public string? Remarks { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? DeviceFingerprint { get; init; }
}
