using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryReconciliation.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetVerification> AssetVerifications => Set<AssetVerification>();
    public DbSet<ReconciliationVariance> ReconciliationVariances => Set<ReconciliationVariance>();
    public DbSet<AssetAuditLog> AssetAuditLogs => Set<AssetAuditLog>();
    public DbSet<InventoryImportBatch> InventoryImportBatches => Set<InventoryImportBatch>();
    public DbSet<InventorySnapshot> InventorySnapshots => Set<InventorySnapshot>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<WorkflowApproval> WorkflowApprovals => Set<WorkflowApproval>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AssetConfiguration());
        modelBuilder.ApplyConfiguration(new AssetVerificationConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationVarianceConfiguration());
        modelBuilder.ApplyConfiguration(new AssetAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ImportBatchConfiguration());
        modelBuilder.ApplyConfiguration(new InventorySnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new AttachmentConfiguration());
        modelBuilder.ApplyConfiguration(new WorkflowApprovalConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleAssignmentConfiguration());
    }
}

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("AssetMaster");
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.AssetTag).HasMaxLength(64).IsRequired();
        builder.Property(asset => asset.NormalizedAssetTag).HasMaxLength(64).IsRequired();
        builder.HasIndex(asset => asset.NormalizedAssetTag).IsUnique();
        builder.Property(asset => asset.SerialNumber).HasMaxLength(128);
        builder.Property(asset => asset.NormalizedSerialNumber).HasMaxLength(128);
        builder.HasIndex(asset => asset.NormalizedSerialNumber);
        builder.Property(asset => asset.HostName).HasMaxLength(128);
        builder.HasIndex(asset => asset.HostName);
        builder.Property(asset => asset.AssetType).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.AssetCategory).HasMaxLength(100);
        builder.Property(asset => asset.Manufacturer).HasMaxLength(100);
        builder.Property(asset => asset.ModelNumber).HasMaxLength(150);
        builder.Property(asset => asset.UserName).HasMaxLength(200);
        builder.Property(asset => asset.EmployeeId).HasMaxLength(64);
        builder.HasIndex(asset => asset.EmployeeId);
        builder.Property(asset => asset.Department).HasMaxLength(150);
        builder.HasIndex(asset => asset.Department);
        builder.Property(asset => asset.CostCenter).HasMaxLength(80);
        builder.Property(asset => asset.StickerNumber).HasMaxLength(80);
        builder.Property(asset => asset.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(asset => asset.PurchaseOrderNumber).HasMaxLength(120);
        builder.Property(asset => asset.InvoiceNumber).HasMaxLength(120);
        builder.Property(asset => asset.Remarks).HasMaxLength(1000);
        builder.Property(asset => asset.RowVersion).IsConcurrencyToken();

        builder.OwnsOne(asset => asset.Location, location =>
        {
            location.Property(value => value.Region).HasColumnName("Region").HasMaxLength(100);
            location.Property(value => value.Location).HasColumnName("Location").HasMaxLength(200);
            location.Property(value => value.Building).HasColumnName("Building").HasMaxLength(120);
            location.Property(value => value.Floor).HasColumnName("Floor").HasMaxLength(80);
            location.Property(value => value.SeatOrCubicle).HasColumnName("SeatOrCubicle").HasMaxLength(80);
            location.HasIndex(value => new { value.Location, value.Building, value.Floor });
        });

        builder.HasMany(asset => asset.AuditLogs)
            .WithOne()
            .HasForeignKey(log => log.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(asset => asset.Verifications)
            .WithOne()
            .HasForeignKey(verification => verification.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(asset => asset.AuditLogs).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(asset => asset.Verifications).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AssetVerificationConfiguration : IEntityTypeConfiguration<AssetVerification>
{
    public void Configure(EntityTypeBuilder<AssetVerification> builder)
    {
        builder.ToTable("AssetVerification");
        builder.HasKey(verification => verification.Id);
        builder.Property(verification => verification.CampaignName).HasMaxLength(160);
        builder.Property(verification => verification.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(verification => verification.Outcome).HasConversion<string>().HasMaxLength(40);
        builder.Property(verification => verification.ApprovalStatus).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(verification => new { verification.AssetId, verification.CreatedAtUtc });
        builder.HasIndex(verification => verification.ApprovalStatus);

        builder.OwnsOne(verification => verification.Observation, observation =>
        {
            observation.Property(value => value.AssetTag).HasColumnName("PhysicalAssetTag").HasMaxLength(64);
            observation.Property(value => value.SerialNumber).HasColumnName("PhysicalSerialNumber").HasMaxLength(128);
            observation.Property(value => value.HostName).HasColumnName("PhysicalHostName").HasMaxLength(128);
            observation.Property(value => value.UserName).HasColumnName("PhysicalUserName").HasMaxLength(200);
            observation.Property(value => value.EmployeeId).HasColumnName("PhysicalEmployeeId").HasMaxLength(64);
            observation.Property(value => value.Department).HasColumnName("PhysicalDepartment").HasMaxLength(150);
            observation.Property(value => value.AssetType).HasColumnName("PhysicalAssetType").HasMaxLength(100);
            observation.Property(value => value.StickerNumber).HasColumnName("PhysicalStickerNumber").HasMaxLength(80);
            observation.Property(value => value.Condition).HasColumnName("PhysicalCondition").HasMaxLength(80);
            observation.Property(value => value.PhysicalStatus).HasColumnName("PhysicalStatus").HasConversion<string>().HasMaxLength(40);
            observation.Property(value => value.Remarks).HasColumnName("VerificationRemarks").HasMaxLength(1000);
            observation.Property(value => value.DeviceFingerprint).HasColumnName("DeviceFingerprint").HasMaxLength(256);
            observation.Property(value => value.Latitude).HasPrecision(10, 7);
            observation.Property(value => value.Longitude).HasPrecision(10, 7);

            observation.OwnsOne(value => value.Location, location =>
            {
                location.Property(value => value.Region).HasColumnName("PhysicalRegion").HasMaxLength(100);
                location.Property(value => value.Location).HasColumnName("PhysicalLocation").HasMaxLength(200);
                location.Property(value => value.Building).HasColumnName("PhysicalBuilding").HasMaxLength(120);
                location.Property(value => value.Floor).HasColumnName("PhysicalFloor").HasMaxLength(80);
                location.Property(value => value.SeatOrCubicle).HasColumnName("PhysicalSeatOrCubicle").HasMaxLength(80);
            });
        });

        builder.HasMany(verification => verification.Variances)
            .WithOne()
            .HasForeignKey(variance => variance.VerificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(verification => verification.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.VerificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(verification => verification.Variances).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(verification => verification.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ReconciliationVarianceConfiguration : IEntityTypeConfiguration<ReconciliationVariance>
{
    public void Configure(EntityTypeBuilder<ReconciliationVariance> builder)
    {
        builder.ToTable("ReconciliationVariance");
        builder.HasKey(variance => variance.Id);
        builder.Property(variance => variance.Type).HasConversion<string>().HasMaxLength(60);
        builder.Property(variance => variance.Severity).HasConversion<string>().HasMaxLength(30);
        builder.Property(variance => variance.ApprovalStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(variance => variance.FieldName).HasMaxLength(120);
        builder.Property(variance => variance.SystemValue).HasMaxLength(1000);
        builder.Property(variance => variance.PhysicalValue).HasMaxLength(1000);
        builder.Property(variance => variance.Message).HasMaxLength(1200);
        builder.HasIndex(variance => new { variance.Type, variance.Severity });
        builder.HasIndex(variance => variance.ApprovalStatus);
    }
}

internal sealed class AssetAuditLogConfiguration : IEntityTypeConfiguration<AssetAuditLog>
{
    public void Configure(EntityTypeBuilder<AssetAuditLog> builder)
    {
        builder.ToTable("AssetAuditLog");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.EventType).HasConversion<string>().HasMaxLength(60);
        builder.Property(log => log.FieldName).HasMaxLength(120);
        builder.Property(log => log.PreviousValue).HasMaxLength(2000);
        builder.Property(log => log.NewValue).HasMaxLength(2000);
        builder.Property(log => log.Remarks).HasMaxLength(1200);
        builder.Property(log => log.DeviceFingerprint).HasMaxLength(256);
        builder.Property(log => log.Latitude).HasPrecision(10, 7);
        builder.Property(log => log.Longitude).HasPrecision(10, 7);
        builder.HasIndex(log => new { log.AssetId, log.CreatedAtUtc });
    }
}

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<InventoryImportBatch>
{
    public void Configure(EntityTypeBuilder<InventoryImportBatch> builder)
    {
        builder.ToTable("InventoryImportBatch");
        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.FileName).HasMaxLength(260);
        builder.Property(batch => batch.ContentHash).HasMaxLength(128);
        builder.Property(batch => batch.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(batch => batch.ValidationSummaryJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(batch => batch.ContentHash);
        builder.HasMany(batch => batch.Snapshots)
            .WithOne()
            .HasForeignKey(snapshot => snapshot.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(batch => batch.Snapshots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InventorySnapshotConfiguration : IEntityTypeConfiguration<InventorySnapshot>
{
    public void Configure(EntityTypeBuilder<InventorySnapshot> builder)
    {
        builder.ToTable("InventorySnapshot");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.SnapshotName).HasMaxLength(160);
        builder.Property(snapshot => snapshot.SourceHash).HasMaxLength(128);
        builder.HasIndex(snapshot => new { snapshot.ImportBatchId, snapshot.CreatedAtUtc });
    }
}

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachment");
        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.FileName).HasMaxLength(260);
        builder.Property(attachment => attachment.ContentType).HasMaxLength(120);
        builder.Property(attachment => attachment.BlobUri).HasMaxLength(1000);
        builder.Property(attachment => attachment.Sha256Hash).HasMaxLength(128);
        builder.HasIndex(attachment => new { attachment.AssetId, attachment.VerificationId });
    }
}

internal sealed class WorkflowApprovalConfiguration : IEntityTypeConfiguration<WorkflowApproval>
{
    public void Configure(EntityTypeBuilder<WorkflowApproval> builder)
    {
        builder.ToTable("WorkflowApproval");
        builder.HasKey(approval => approval.Id);
        builder.Property(approval => approval.AssignedTo).HasMaxLength(200);
        builder.Property(approval => approval.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(approval => approval.DecisionRemarks).HasMaxLength(1000);
        builder.HasIndex(approval => new { approval.AssignedTo, approval.Status });
        builder.HasIndex(approval => approval.DueAtUtc);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notification");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.RecipientUserId).HasMaxLength(200);
        builder.Property(notification => notification.Title).HasMaxLength(160);
        builder.Property(notification => notification.Message).HasMaxLength(1000);
        builder.Property(notification => notification.DeepLink).HasMaxLength(600);
        builder.HasIndex(notification => new { notification.RecipientUserId, notification.IsRead, notification.CreatedAtUtc });
    }
}

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfile");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.EntraObjectId).HasMaxLength(120);
        builder.Property(user => user.DisplayName).HasMaxLength(200);
        builder.Property(user => user.Email).HasMaxLength(320);
        builder.Property(user => user.Region).HasMaxLength(100);
        builder.Property(user => user.Department).HasMaxLength(150);
        builder.HasIndex(user => user.EntraObjectId).IsUnique();
        builder.HasMany(user => user.Roles)
            .WithOne()
            .HasForeignKey(role => role.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(user => user.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("UserRoleAssignment");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Role).HasConversion<string>().HasMaxLength(80);
        builder.HasIndex(role => new { role.UserProfileId, role.Role }).IsUnique();
    }
}
