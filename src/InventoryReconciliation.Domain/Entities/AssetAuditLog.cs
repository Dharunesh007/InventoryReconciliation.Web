using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Domain.Entities;

public sealed class AssetAuditLog : Entity
{
    private AssetAuditLog()
    {
    }

    public Guid AssetId { get; private set; }
    public Guid? VerificationId { get; private set; }
    public AuditEventType EventType { get; private set; }
    public string? FieldName { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public string Remarks { get; private set; } = string.Empty;
    public string? DeviceFingerprint { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    public static AssetAuditLog ForFieldChange(
        Guid assetId,
        string fieldName,
        string? previousValue,
        string? newValue,
        string userId,
        AuditEventType eventType,
        string remarks) =>
        new()
        {
            AssetId = assetId,
            FieldName = fieldName,
            PreviousValue = previousValue,
            NewValue = newValue,
            CreatedBy = userId,
            EventType = eventType,
            Remarks = remarks
        };

    public static AssetAuditLog ForEvent(Guid assetId, AuditEventType eventType, string userId, string remarks) =>
        new()
        {
            AssetId = assetId,
            EventType = eventType,
            CreatedBy = userId,
            Remarks = remarks
        };

    public static AssetAuditLog ForVariance(Guid assetId, ReconciliationVariance variance, string userId) =>
        new()
        {
            AssetId = assetId,
            VerificationId = variance.VerificationId,
            EventType = AuditEventType.VarianceDetected,
            FieldName = variance.FieldName,
            PreviousValue = variance.SystemValue,
            NewValue = variance.PhysicalValue,
            CreatedBy = userId,
            Remarks = variance.Message
        };
}
