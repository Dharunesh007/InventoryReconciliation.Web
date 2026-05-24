namespace InventoryReconciliation.Domain.Entities;

public sealed class Attachment : Entity
{
    private Attachment()
    {
    }

    public Attachment(Guid? assetId, Guid? verificationId, string fileName, string contentType, string blobUri, string createdBy)
    {
        AssetId = assetId;
        VerificationId = verificationId;
        FileName = fileName;
        ContentType = contentType;
        BlobUri = blobUri;
        CreatedBy = createdBy;
    }

    public Guid? AssetId { get; private set; }
    public Guid? VerificationId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string BlobUri { get; private set; } = string.Empty;
    public string? Sha256Hash { get; private set; }
}
