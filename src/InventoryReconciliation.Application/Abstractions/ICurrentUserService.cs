namespace InventoryReconciliation.Application.Abstractions;

public interface ICurrentUserService
{
    string UserId { get; }
    string DisplayName { get; }
    bool IsInRole(string role);
}
