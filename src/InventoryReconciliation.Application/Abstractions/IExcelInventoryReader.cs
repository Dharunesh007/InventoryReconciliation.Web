using InventoryReconciliation.Application.Imports;

namespace InventoryReconciliation.Application.Abstractions;

public interface IExcelInventoryReader
{
    Task<ImportPreviewResult> PreviewAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
