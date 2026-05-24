namespace InventoryReconciliation.Domain.Entities;

public sealed class Notification : Entity
{
    private Notification()
    {
    }

    public Notification(string recipientUserId, string title, string message, string? deepLink, string createdBy)
    {
        RecipientUserId = recipientUserId;
        Title = title;
        Message = message;
        DeepLink = deepLink;
        CreatedBy = createdBy;
    }

    public string RecipientUserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? DeepLink { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
}
