namespace InventoryReconciliation.Application.Abstractions;

public interface IUploadedWorkbookStorage
{
    FileInfo GetWorkbookFile();
    Task<UploadedWorkbookSaveResult> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}

public sealed record UploadedWorkbookSaveResult(
    string OriginalFileName,
    string StoredPath,
    long SizeBytes,
    DateTimeOffset SavedAtUtc);
