using InventoryReconciliation.Application.Assets;

namespace InventoryReconciliation.Application.Abstractions;

public interface IUploadedInventorySource
{
    Task<UploadedInventorySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
