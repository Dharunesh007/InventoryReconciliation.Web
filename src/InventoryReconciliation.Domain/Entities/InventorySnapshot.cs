namespace InventoryReconciliation.Domain.Entities;

public sealed class InventorySnapshot : Entity
{
    private InventorySnapshot()
    {
    }

    public InventorySnapshot(Guid importBatchId, string snapshotName, int assetCount, string createdBy)
    {
        ImportBatchId = importBatchId;
        SnapshotName = snapshotName;
        AssetCount = assetCount;
        CreatedBy = createdBy;
    }

    public Guid ImportBatchId { get; private set; }
    public string SnapshotName { get; private set; } = string.Empty;
    public int AssetCount { get; private set; }
    public string? SourceHash { get; private set; }
    public bool IsBaseline { get; private set; }
}
