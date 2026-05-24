using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Domain.Entities;

public sealed class InventoryImportBatch : Entity
{
    private readonly List<InventorySnapshot> _snapshots = [];

    private InventoryImportBatch()
    {
    }

    public InventoryImportBatch(string fileName, string contentHash, long fileSizeBytes, string createdBy)
    {
        FileName = fileName;
        ContentHash = contentHash;
        FileSizeBytes = fileSizeBytes;
        CreatedBy = createdBy;
        Status = ApprovalStatus.Draft;
    }

    public string FileName { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int RejectedRows { get; private set; }
    public int DuplicateRows { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string? ValidationSummaryJson { get; private set; }

    public IReadOnlyCollection<InventorySnapshot> Snapshots => _snapshots;

    public void CompleteValidation(int totalRows, int validRows, int rejectedRows, int duplicateRows, string validationSummaryJson)
    {
        TotalRows = totalRows;
        ValidRows = validRows;
        RejectedRows = rejectedRows;
        DuplicateRows = duplicateRows;
        ValidationSummaryJson = validationSummaryJson;
        Status = rejectedRows == 0 ? ApprovalStatus.Submitted : ApprovalStatus.Draft;
    }

    public InventorySnapshot CreateSnapshot(string snapshotName, string userId)
    {
        var snapshot = new InventorySnapshot(Id, snapshotName, ValidRows, userId);
        _snapshots.Add(snapshot);
        return snapshot;
    }
}
