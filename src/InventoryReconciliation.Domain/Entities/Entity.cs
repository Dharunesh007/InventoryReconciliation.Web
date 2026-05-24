namespace InventoryReconciliation.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; protected set; } = "system";
    public DateTimeOffset? ModifiedAtUtc { get; protected set; }
    public string? ModifiedBy { get; protected set; }

    public void Touch(string userId)
    {
        ModifiedAtUtc = DateTimeOffset.UtcNow;
        ModifiedBy = userId;
    }
}
