using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Domain.Entities;

public sealed class WorkflowApproval : Entity
{
    private WorkflowApproval()
    {
    }

    public WorkflowApproval(Guid verificationId, string assignedTo, string createdBy, DateTimeOffset dueAtUtc)
    {
        VerificationId = verificationId;
        AssignedTo = assignedTo;
        CreatedBy = createdBy;
        DueAtUtc = dueAtUtc;
        Status = ApprovalStatus.Submitted;
    }

    public Guid VerificationId { get; private set; }
    public string AssignedTo { get; private set; } = string.Empty;
    public ApprovalStatus Status { get; private set; }
    public DateTimeOffset DueAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? DecisionRemarks { get; private set; }
}
