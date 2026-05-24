using InventoryReconciliation.Domain.Enums;
using InventoryReconciliation.Domain.ValueObjects;

namespace InventoryReconciliation.Domain.Entities;

public sealed class AssetVerification : Entity
{
    private readonly List<ReconciliationVariance> _variances = [];
    private readonly List<Attachment> _attachments = [];

    private AssetVerification()
    {
    }

    public AssetVerification(Guid assetId, PhysicalObservation observation, string createdBy, string? campaignName)
    {
        AssetId = assetId;
        Observation = observation;
        CreatedBy = createdBy;
        CampaignName = campaignName;
        Status = VerificationStatus.InProgress;
    }

    public Guid AssetId { get; private set; }
    public string? CampaignName { get; private set; }
    public PhysicalObservation Observation { get; private set; } = new(null, null, null, null, null, null, new(null, null, null, null, null), null, null, null, AssetStatus.Active, false, false, null);
    public VerificationStatus Status { get; private set; }
    public ReconciliationOutcome Outcome { get; private set; }
    public int ConfidenceScore { get; private set; }
    public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Draft;
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }

    public IReadOnlyCollection<ReconciliationVariance> Variances => _variances;
    public IReadOnlyCollection<Attachment> Attachments => _attachments;

    public void ApplyReconciliation(ReconciliationOutcome outcome, int confidenceScore, IEnumerable<ReconciliationVariance> variances)
    {
        Outcome = outcome;
        ConfidenceScore = Math.Clamp(confidenceScore, 0, 100);
        _variances.Clear();
        _variances.AddRange(variances);
        Status = _variances.Count == 0 ? VerificationStatus.Verified : VerificationStatus.ExceptionRaised;
    }

    public void Submit(string userId)
    {
        ApprovalStatus = ApprovalStatus.Submitted;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
        Touch(userId);
    }

    public void Approve(string userId)
    {
        ApprovalStatus = ApprovalStatus.Approved;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        ApprovedBy = userId;
        Status = VerificationStatus.Approved;
        Touch(userId);
    }

    public void AddAttachment(Attachment attachment)
    {
        _attachments.Add(attachment);
    }
}
