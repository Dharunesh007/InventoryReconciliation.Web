using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Domain.Entities;

public sealed class ReconciliationVariance : Entity
{
    private ReconciliationVariance()
    {
    }

    public ReconciliationVariance(
        Guid? assetId,
        Guid? verificationId,
        VarianceType type,
        Severity severity,
        string fieldName,
        string? systemValue,
        string? physicalValue,
        int confidenceImpact,
        string message)
    {
        AssetId = assetId;
        VerificationId = verificationId;
        Type = type;
        Severity = severity;
        FieldName = fieldName;
        SystemValue = systemValue;
        PhysicalValue = physicalValue;
        ConfidenceImpact = confidenceImpact;
        Message = message;
    }

    public Guid? AssetId { get; private set; }
    public Guid? VerificationId { get; private set; }
    public VarianceType Type { get; private set; }
    public Severity Severity { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public string? SystemValue { get; private set; }
    public string? PhysicalValue { get; private set; }
    public int ConfidenceImpact { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Draft;
}
