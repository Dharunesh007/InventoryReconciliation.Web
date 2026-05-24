using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Application.Reconciliation;

public sealed record VerificationInputDto(
    Guid? AssetId,
    string? AssetTag,
    string? SerialNumber,
    string? HostName,
    string? UserName,
    string? EmployeeId,
    string? Department,
    string? Location,
    string? Building,
    string? Floor,
    string? SeatOrCubicle,
    string? AssetType,
    string? StickerNumber,
    string? Condition,
    bool StickerPresent,
    bool IsPhysicallyFound,
    string? Remarks,
    decimal? Latitude,
    decimal? Longitude,
    string? DeviceFingerprint);

public sealed record VarianceDto(
    VarianceType Type,
    Severity Severity,
    string FieldName,
    string? SystemValue,
    string? PhysicalValue,
    string Message,
    int ConfidenceImpact);

public sealed record ReconciliationResultDto(
    ReconciliationOutcome Outcome,
    int ConfidenceScore,
    IReadOnlyCollection<VarianceDto> Variances,
    string Recommendation);
