using InventoryReconciliation.Application.Assets;

namespace InventoryReconciliation.Application.Abstractions;

public interface IWorkbookAssetEditWriter
{
    Task SaveEditsAsync(IEnumerable<AssetEditRequest> requests, CancellationToken cancellationToken = default);
    Task<byte[]> ExportEditedWorkbookAsync(CancellationToken cancellationToken = default);
}
