using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Domain.Entities;

public sealed class UserProfile : Entity
{
    private readonly List<UserRoleAssignment> _roles = [];

    private UserProfile()
    {
    }

    public UserProfile(string entraObjectId, string displayName, string email, string createdBy)
    {
        EntraObjectId = entraObjectId;
        DisplayName = displayName;
        Email = email;
        CreatedBy = createdBy;
    }

    public string EntraObjectId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public string? Department { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<UserRoleAssignment> Roles => _roles;

    public void AssignRole(EnterpriseRole role, string assignedBy)
    {
        if (_roles.Any(existing => existing.Role == role))
        {
            return;
        }

        _roles.Add(new UserRoleAssignment(Id, role, assignedBy));
    }
}

public sealed class UserRoleAssignment : Entity
{
    private UserRoleAssignment()
    {
    }

    public UserRoleAssignment(Guid userProfileId, EnterpriseRole role, string createdBy)
    {
        UserProfileId = userProfileId;
        Role = role;
        CreatedBy = createdBy;
    }

    public Guid UserProfileId { get; private set; }
    public EnterpriseRole Role { get; private set; }
}
