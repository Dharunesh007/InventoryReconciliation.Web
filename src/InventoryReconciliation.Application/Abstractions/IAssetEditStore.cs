using InventoryReconciliation.Application.Assets;

namespace InventoryReconciliation.Application.Abstractions;

public interface IAssetEditStore
{
    Task<DateTimeOffset?> GetRevisionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, AssetEditEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AssetEditRequest request, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<AssetEditRequest> requests, CancellationToken cancellationToken = default);
    Task<UploadedInventorySnapshot> ApplyEditsAsync(UploadedInventorySnapshot snapshot, CancellationToken cancellationToken = default);
}
