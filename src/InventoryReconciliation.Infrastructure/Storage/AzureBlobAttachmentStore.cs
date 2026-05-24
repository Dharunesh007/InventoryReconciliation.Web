using Azure.Storage.Blobs;
using InventoryReconciliation.Application.Abstractions;

namespace InventoryReconciliation.Infrastructure.Storage;

public sealed class AzureBlobAttachmentStore(BlobContainerClient containerClient) : IAttachmentStore
{
    public async Task<Uri> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var safeName = string.Join('-', fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var blobName = $"{DateTimeOffset.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeName}";
        var blob = containerClient.GetBlobClient(blobName);

        await blob.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType },
            Metadata = new Dictionary<string, string>
            {
                ["uploadedAtUtc"] = DateTimeOffset.UtcNow.ToString("O")
            }
        }, cancellationToken);

        return blob.Uri;
    }
}
