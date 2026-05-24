namespace InventoryReconciliation.Application.Abstractions;

public interface IAttachmentStore
{
    Task<Uri> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}
