using InventoryReconciliation.Domain.Entities;
using InventoryReconciliation.Domain.Enums;
using InventoryReconciliation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InventoryReconciliation.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedDevelopmentAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Assets.AnyAsync(cancellationToken))
        {
            return;
        }

        var snapshotId = Guid.NewGuid();
        var assets = new[]
        {
            new Asset("QDA0649", "B1XD9R2", "Desktop", "DELL", "OPTIPLEX 3050", new AssetLocation("South", "Bidadi Plant", "Admin Block", "Ground Floor", "Commutation Room"), "seed"),
            new Asset("QDB0146", "HPH5VX3", "Laptop", "HP", "EliteBook 840", new AssetLocation("South", "Bidadi Plant", "Stores", "Ground Floor", "IS Stores"), "seed"),
            new Asset("QDB0631", "8GBSXC2", "Desktop", "DELL", "OPTIPLEX 3030", new AssetLocation("South", "Bidadi Plant", "New Office", "First Floor", "Finance Bay"), "seed")
        };

        assets[0].ApplyImportedFields(new ImportedAssetFields("AD01", "B1XD9R2", "Desktop", "C", "DELL", "OPTIPLEX 3050", "MADHU KUMAR B C", "66", "IT", "CC-IT", "QDA0649", assets[0].Location, AssetStatus.Active, null, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)), "TKAP/CG/000001266", "2110131338", "Imported from sample workbook"), snapshotId, "seed");
        assets[1].ApplyImportedFields(new ImportedAssetFields("FIN51", "HPH5VX3", "Laptop", "A", "HP", "EliteBook 840", "CHANDRASHEKAR P", "255", "Finance", "CC-FIN", "QDB0146", assets[1].Location, AssetStatus.Active, null, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), "PO-2024-044", "INV-8842", "Seed laptop"), snapshotId, "seed");
        assets[2].ApplyImportedFields(new ImportedAssetFields("QDB0631", "8GBSXC2", "Desktop", "C", "DELL", "OPTIPLEX 3030", "RAVIKUMAR PC", "70", "Operations", "CC-OPS", "QDB0631", assets[2].Location, AssetStatus.Inactive, null, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-8)), "PO-2022-012", "INV-1024", "Seed inactive asset"), snapshotId, "seed");

        await dbContext.Assets.AddRangeAsync(assets, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
