using InventoryReconciliation.Application.Reconciliation;
using InventoryReconciliation.Domain.Entities;
using InventoryReconciliation.Domain.Enums;
using InventoryReconciliation.Domain.ValueObjects;
using Xunit;

namespace InventoryReconciliation.Tests;

public sealed class ReconciliationEngineTests
{
    [Fact]
    public void Compare_ReturnsExactMatch_WhenPhysicalObservationMatchesMaster()
    {
        var engine = new ReconciliationEngine();
        var asset = CreateAsset();
        var observation = new PhysicalObservation(
            "QDA0649",
            "B1XD9R2",
            "AD01",
            "MADHU KUMAR B C",
            "66",
            "IT",
            new AssetLocation("South", "Bidadi Plant", "Admin Block", "Ground Floor", "Commutation Room"),
            "Desktop",
            "QDA0649",
            "Good",
            AssetStatus.Active,
            true,
            true,
            "Verified");

        var result = engine.Compare(asset, observation, new DateOnly(2022, 01, 01));

        Assert.Equal(ReconciliationOutcome.ExactMatch, result.Outcome);
        Assert.Equal(100, result.ConfidenceScore);
        Assert.Empty(result.Variances);
    }

    [Fact]
    public void Compare_ReturnsUnauthorizedAsset_WhenNoMasterRecordExists()
    {
        var engine = new ReconciliationEngine();
        var observation = new PhysicalObservation(
            "UNKNOWN-24",
            null,
            null,
            null,
            null,
            null,
            new AssetLocation(null, "Security Gate", null, null, null),
            "Monitor",
            null,
            "Good",
            AssetStatus.Active,
            false,
            true,
            "Extra asset found");

        var result = engine.Compare(null, observation, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Equal(ReconciliationOutcome.UnauthorizedAsset, result.Outcome);
        Assert.Contains(result.Variances, variance => variance.Type == VarianceType.NewUnidentifiedAsset);
    }

    [Fact]
    public void Compare_ClassifiesInactiveButFoundAsCriticalMismatch()
    {
        var engine = new ReconciliationEngine();
        var asset = CreateAsset(status: AssetStatus.Inactive);
        var observation = new PhysicalObservation(
            "QDA0649",
            "B1XD9R2",
            "AD01",
            "MADHU KUMAR B C",
            "66",
            "IT",
            new AssetLocation("South", "Bidadi Plant", "Admin Block", "Ground Floor", "Commutation Room"),
            "Desktop",
            "QDA0649",
            "Good",
            AssetStatus.Active,
            true,
            true,
            "Found physically");

        var result = engine.Compare(asset, observation, new DateOnly(2022, 01, 01));

        Assert.Equal(ReconciliationOutcome.CriticalMismatch, result.Outcome);
        Assert.Contains(result.Variances, variance => variance.Type == VarianceType.InactiveButAvailable);
    }

    private static Asset CreateAsset(AssetStatus status = AssetStatus.Active)
    {
        var location = new AssetLocation("South", "Bidadi Plant", "Admin Block", "Ground Floor", "Commutation Room");
        var asset = new Asset("QDA0649", "B1XD9R2", "Desktop", "DELL", "OPTIPLEX 3050", location, "test");
        asset.ApplyImportedFields(new ImportedAssetFields(
            "AD01",
            "B1XD9R2",
            "Desktop",
            "C",
            "DELL",
            "OPTIPLEX 3050",
            "MADHU KUMAR B C",
            "66",
            "IT",
            "CC-IT",
            "QDA0649",
            location,
            status,
            new DateOnly(2019, 01, 01),
            new DateOnly(2019, 01, 01),
            new DateOnly(2024, 01, 01),
            "PO-1",
            "INV-1",
            "Seed"),
            Guid.NewGuid(),
            "test");

        return asset;
    }
}
