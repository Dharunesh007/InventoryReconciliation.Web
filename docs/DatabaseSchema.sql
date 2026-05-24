/*
Inventory Physical Verification & Reconciliation - SQL Server schema
Target: Azure SQL / SQL Server 2022+
*/

CREATE TABLE dbo.AssetMaster (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_AssetMaster PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    AssetTag nvarchar(64) NOT NULL,
    NormalizedAssetTag nvarchar(64) NOT NULL,
    HostName nvarchar(128) NULL,
    SerialNumber nvarchar(128) NULL,
    NormalizedSerialNumber nvarchar(128) NULL,
    AssetType nvarchar(100) NOT NULL,
    AssetCategory nvarchar(100) NULL,
    Manufacturer nvarchar(100) NULL,
    ModelNumber nvarchar(150) NULL,
    UserName nvarchar(200) NULL,
    EmployeeId nvarchar(64) NULL,
    Department nvarchar(150) NULL,
    CostCenter nvarchar(80) NULL,
    StickerNumber nvarchar(80) NULL,
    Region nvarchar(100) NULL,
    Location nvarchar(200) NULL,
    Building nvarchar(120) NULL,
    Floor nvarchar(80) NULL,
    SeatOrCubicle nvarchar(80) NULL,
    Status nvarchar(40) NOT NULL,
    IsActive bit NOT NULL,
    IsDisposed bit NOT NULL,
    PurchaseDate date NULL,
    WarrantyStartDate date NULL,
    WarrantyEndDate date NULL,
    PurchaseOrderNumber nvarchar(120) NULL,
    InvoiceNumber nvarchar(120) NULL,
    Remarks nvarchar(1000) NULL,
    CurrentSnapshotId uniqueidentifier NULL,
    RowVersion int NOT NULL
);

CREATE UNIQUE INDEX UX_AssetMaster_NormalizedAssetTag ON dbo.AssetMaster (NormalizedAssetTag);
CREATE INDEX IX_AssetMaster_NormalizedSerialNumber ON dbo.AssetMaster (NormalizedSerialNumber) WHERE NormalizedSerialNumber IS NOT NULL;
CREATE INDEX IX_AssetMaster_HostName ON dbo.AssetMaster (HostName) WHERE HostName IS NOT NULL;
CREATE INDEX IX_AssetMaster_EmployeeId ON dbo.AssetMaster (EmployeeId) WHERE EmployeeId IS NOT NULL;
CREATE INDEX IX_AssetMaster_Department_Status ON dbo.AssetMaster (Department, Status);
CREATE INDEX IX_AssetMaster_Location_Building_Floor ON dbo.AssetMaster (Location, Building, Floor);
CREATE INDEX IX_AssetMaster_WarrantyEndDate ON dbo.AssetMaster (WarrantyEndDate) WHERE WarrantyEndDate IS NOT NULL;

CREATE TABLE dbo.InventoryImportBatch (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_InventoryImportBatch PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    FileName nvarchar(260) NOT NULL,
    ContentHash nvarchar(128) NOT NULL,
    FileSizeBytes bigint NOT NULL,
    TotalRows int NOT NULL,
    ValidRows int NOT NULL,
    RejectedRows int NOT NULL,
    DuplicateRows int NOT NULL,
    Status nvarchar(40) NOT NULL,
    ValidationSummaryJson nvarchar(max) NULL
);

CREATE INDEX IX_InventoryImportBatch_ContentHash ON dbo.InventoryImportBatch (ContentHash);
CREATE INDEX IX_InventoryImportBatch_CreatedAtUtc ON dbo.InventoryImportBatch (CreatedAtUtc DESC);

CREATE TABLE dbo.InventorySnapshot (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_InventorySnapshot PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    ImportBatchId uniqueidentifier NOT NULL,
    SnapshotName nvarchar(160) NOT NULL,
    AssetCount int NOT NULL,
    SourceHash nvarchar(128) NULL,
    IsBaseline bit NOT NULL,
    CONSTRAINT FK_InventorySnapshot_InventoryImportBatch FOREIGN KEY (ImportBatchId) REFERENCES dbo.InventoryImportBatch(Id)
);

CREATE INDEX IX_InventorySnapshot_ImportBatch_Created ON dbo.InventorySnapshot (ImportBatchId, CreatedAtUtc DESC);

CREATE TABLE dbo.AssetVerification (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_AssetVerification PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    AssetId uniqueidentifier NOT NULL,
    CampaignName nvarchar(160) NULL,
    PhysicalAssetTag nvarchar(64) NULL,
    PhysicalSerialNumber nvarchar(128) NULL,
    PhysicalHostName nvarchar(128) NULL,
    PhysicalUserName nvarchar(200) NULL,
    PhysicalEmployeeId nvarchar(64) NULL,
    PhysicalDepartment nvarchar(150) NULL,
    PhysicalRegion nvarchar(100) NULL,
    PhysicalLocation nvarchar(200) NULL,
    PhysicalBuilding nvarchar(120) NULL,
    PhysicalFloor nvarchar(80) NULL,
    PhysicalSeatOrCubicle nvarchar(80) NULL,
    PhysicalAssetType nvarchar(100) NULL,
    PhysicalStickerNumber nvarchar(80) NULL,
    PhysicalCondition nvarchar(80) NULL,
    PhysicalStatus nvarchar(40) NOT NULL,
    StickerPresent bit NOT NULL,
    IsPhysicallyFound bit NOT NULL,
    VerificationRemarks nvarchar(1000) NULL,
    Latitude decimal(10,7) NULL,
    Longitude decimal(10,7) NULL,
    DeviceFingerprint nvarchar(256) NULL,
    Status nvarchar(40) NOT NULL,
    Outcome nvarchar(40) NOT NULL,
    ConfidenceScore int NOT NULL,
    ApprovalStatus nvarchar(40) NOT NULL,
    SubmittedAtUtc datetimeoffset NULL,
    ApprovedAtUtc datetimeoffset NULL,
    ApprovedBy nvarchar(200) NULL,
    CONSTRAINT FK_AssetVerification_AssetMaster FOREIGN KEY (AssetId) REFERENCES dbo.AssetMaster(Id)
);

CREATE INDEX IX_AssetVerification_Asset_Created ON dbo.AssetVerification (AssetId, CreatedAtUtc DESC);
CREATE INDEX IX_AssetVerification_ApprovalStatus ON dbo.AssetVerification (ApprovalStatus);
CREATE INDEX IX_AssetVerification_Outcome_Status ON dbo.AssetVerification (Outcome, Status);
CREATE INDEX IX_AssetVerification_CampaignName ON dbo.AssetVerification (CampaignName);

CREATE TABLE dbo.ReconciliationVariance (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_ReconciliationVariance PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    AssetId uniqueidentifier NULL,
    VerificationId uniqueidentifier NULL,
    Type nvarchar(60) NOT NULL,
    Severity nvarchar(30) NOT NULL,
    FieldName nvarchar(120) NOT NULL,
    SystemValue nvarchar(1000) NULL,
    PhysicalValue nvarchar(1000) NULL,
    ConfidenceImpact int NOT NULL,
    Message nvarchar(1200) NOT NULL,
    ApprovalStatus nvarchar(40) NOT NULL,
    CONSTRAINT FK_ReconciliationVariance_AssetMaster FOREIGN KEY (AssetId) REFERENCES dbo.AssetMaster(Id),
    CONSTRAINT FK_ReconciliationVariance_AssetVerification FOREIGN KEY (VerificationId) REFERENCES dbo.AssetVerification(Id)
);

CREATE INDEX IX_ReconciliationVariance_Type_Severity ON dbo.ReconciliationVariance (Type, Severity);
CREATE INDEX IX_ReconciliationVariance_ApprovalStatus ON dbo.ReconciliationVariance (ApprovalStatus);
CREATE INDEX IX_ReconciliationVariance_Asset ON dbo.ReconciliationVariance (AssetId, CreatedAtUtc DESC);
CREATE INDEX IX_ReconciliationVariance_Verification ON dbo.ReconciliationVariance (VerificationId);

CREATE TABLE dbo.AssetAuditLog (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_AssetAuditLog PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    AssetId uniqueidentifier NOT NULL,
    VerificationId uniqueidentifier NULL,
    EventType nvarchar(60) NOT NULL,
    FieldName nvarchar(120) NULL,
    PreviousValue nvarchar(2000) NULL,
    NewValue nvarchar(2000) NULL,
    Remarks nvarchar(1200) NOT NULL,
    DeviceFingerprint nvarchar(256) NULL,
    Latitude decimal(10,7) NULL,
    Longitude decimal(10,7) NULL,
    CONSTRAINT FK_AssetAuditLog_AssetMaster FOREIGN KEY (AssetId) REFERENCES dbo.AssetMaster(Id)
);

CREATE INDEX IX_AssetAuditLog_Asset_Created ON dbo.AssetAuditLog (AssetId, CreatedAtUtc DESC);
CREATE INDEX IX_AssetAuditLog_EventType_Created ON dbo.AssetAuditLog (EventType, CreatedAtUtc DESC);

CREATE TABLE dbo.Attachment (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Attachment PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    AssetId uniqueidentifier NULL,
    VerificationId uniqueidentifier NULL,
    FileName nvarchar(260) NOT NULL,
    ContentType nvarchar(120) NOT NULL,
    BlobUri nvarchar(1000) NOT NULL,
    Sha256Hash nvarchar(128) NULL
);

CREATE INDEX IX_Attachment_Asset_Verification ON dbo.Attachment (AssetId, VerificationId);

CREATE TABLE dbo.WorkflowApproval (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_WorkflowApproval PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    VerificationId uniqueidentifier NOT NULL,
    AssignedTo nvarchar(200) NOT NULL,
    Status nvarchar(40) NOT NULL,
    DueAtUtc datetimeoffset NOT NULL,
    CompletedAtUtc datetimeoffset NULL,
    DecisionRemarks nvarchar(1000) NULL,
    CONSTRAINT FK_WorkflowApproval_AssetVerification FOREIGN KEY (VerificationId) REFERENCES dbo.AssetVerification(Id)
);

CREATE INDEX IX_WorkflowApproval_Assigned_Status ON dbo.WorkflowApproval (AssignedTo, Status);
CREATE INDEX IX_WorkflowApproval_DueAtUtc ON dbo.WorkflowApproval (DueAtUtc);

CREATE TABLE dbo.Notification (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Notification PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    RecipientUserId nvarchar(200) NOT NULL,
    Title nvarchar(160) NOT NULL,
    Message nvarchar(1000) NOT NULL,
    DeepLink nvarchar(600) NULL,
    IsRead bit NOT NULL,
    ReadAtUtc datetimeoffset NULL
);

CREATE INDEX IX_Notification_Recipient_Read_Created ON dbo.Notification (RecipientUserId, IsRead, CreatedAtUtc DESC);

CREATE TABLE dbo.UserProfile (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_UserProfile PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    EntraObjectId nvarchar(120) NOT NULL,
    DisplayName nvarchar(200) NOT NULL,
    Email nvarchar(320) NOT NULL,
    Region nvarchar(100) NULL,
    Department nvarchar(150) NULL,
    IsActive bit NOT NULL
);

CREATE UNIQUE INDEX UX_UserProfile_EntraObjectId ON dbo.UserProfile (EntraObjectId);

CREATE TABLE dbo.UserRoleAssignment (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_UserRoleAssignment PRIMARY KEY,
    CreatedAtUtc datetimeoffset NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAtUtc datetimeoffset NULL,
    ModifiedBy nvarchar(200) NULL,
    UserProfileId uniqueidentifier NOT NULL,
    Role nvarchar(80) NOT NULL,
    CONSTRAINT FK_UserRoleAssignment_UserProfile FOREIGN KEY (UserProfileId) REFERENCES dbo.UserProfile(Id)
);

CREATE UNIQUE INDEX UX_UserRoleAssignment_User_Role ON dbo.UserRoleAssignment (UserProfileId, Role);

-- Optional row-level security pattern. Bind SESSION_CONTEXT('Region') in middleware after Entra login.
CREATE SCHEMA Security;
GO

CREATE FUNCTION Security.fn_regionPredicate(@Region nvarchar(100))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_accessResult
WHERE @Region IS NULL
   OR @Region = CAST(SESSION_CONTEXT(N'Region') AS nvarchar(100))
   OR CAST(SESSION_CONTEXT(N'IsGlobalInventoryAdmin') AS bit) = 1;
GO

CREATE SECURITY POLICY Security.AssetRegionFilter
ADD FILTER PREDICATE Security.fn_regionPredicate(Region) ON dbo.AssetMaster
WITH (STATE = OFF);
GO
