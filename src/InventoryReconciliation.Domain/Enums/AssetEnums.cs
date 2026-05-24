namespace InventoryReconciliation.Domain.Enums;

public enum AssetStatus
{
    Active = 1,
    Inactive = 2,
    InRepair = 3,
    InTransit = 4,
    Retired = 5,
    Disposed = 6
}

public enum VerificationStatus
{
    Pending = 1,
    InProgress = 2,
    Verified = 3,
    ExceptionRaised = 4,
    Approved = 5,
    Rejected = 6
}

public enum ReconciliationOutcome
{
    ExactMatch = 1,
    PartialMatch = 2,
    CriticalMismatch = 3,
    MissingAsset = 4,
    UnauthorizedAsset = 5,
    DuplicateCandidate = 6
}

public enum VarianceType
{
    UserNameMismatch = 1,
    LocationChange = 2,
    StickerMissing = 3,
    AssetNotFound = 4,
    ExtraAssetFound = 5,
    SerialNumberMismatch = 6,
    WrongAssetType = 7,
    AssetDamaged = 8,
    InactiveButAvailable = 9,
    ActiveButMissing = 10,
    DepartmentChange = 11,
    CustodianChange = 12,
    DuplicateAssetTag = 13,
    UnauthorizedMovement = 14,
    WarrantyExpired = 15,
    NewUnidentifiedAsset = 16,
    BuildingOrFloorChange = 17,
    StickerMismatch = 18
}

public enum Severity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum ApprovalStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Escalated = 5
}

public enum AuditEventType
{
    InventoryImported = 1,
    AssetCreated = 2,
    AssetUpdated = 3,
    VerificationRecorded = 4,
    VarianceDetected = 5,
    ReconciliationApproved = 6,
    AttachmentAdded = 7,
    WorkflowEscalated = 8,
    ExportGenerated = 9
}

public enum EnterpriseRole
{
    SuperAdmin = 1,
    InventoryAdmin = 2,
    Auditor = 3,
    RegionalManager = 4,
    ItSupport = 5,
    ReadOnlyExecutive = 6,
    ComplianceTeam = 7
}
