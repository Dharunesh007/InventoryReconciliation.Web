using InventoryReconciliation.Domain.Entities;
using InventoryReconciliation.Domain.Enums;
using InventoryReconciliation.Domain.ValueObjects;

namespace InventoryReconciliation.Application.Reconciliation;

public interface IReconciliationEngine
{
    ReconciliationResultDto Compare(Asset? systemAsset, PhysicalObservation physicalObservation, DateOnly today);
}

public sealed class ReconciliationEngine : IReconciliationEngine
{
    public ReconciliationResultDto Compare(Asset? systemAsset, PhysicalObservation physicalObservation, DateOnly today)
    {
        if (systemAsset is null)
        {
            var variance = new VarianceDto(
                VarianceType.NewUnidentifiedAsset,
                Severity.Critical,
                "AssetTag",
                null,
                physicalObservation.AssetTag,
                "Physical asset does not exist in the master inventory.",
                45);

            return new ReconciliationResultDto(
                ReconciliationOutcome.UnauthorizedAsset,
                55,
                [variance],
                "Create exception record, capture image evidence, and route to Inventory Admin for ownership validation.");
        }

        var variances = new List<VarianceDto>();

        if (!physicalObservation.IsPhysicallyFound)
        {
            variances.Add(new VarianceDto(
                VarianceType.AssetNotFound,
                Severity.Critical,
                "PhysicalPresence",
                "Expected",
                "Missing",
                "Asset is active in the system but was not found during verification.",
                40));
        }

        AddMismatch(variances, VarianceType.SerialNumberMismatch, Severity.High, "SerialNumber", systemAsset.SerialNumber, physicalObservation.SerialNumber, 25);
        AddMismatch(variances, VarianceType.UserNameMismatch, Severity.Medium, "UserName", systemAsset.UserName, physicalObservation.UserName, 12);
        AddMismatch(variances, VarianceType.DepartmentChange, Severity.Medium, "Department", systemAsset.Department, physicalObservation.Department, 10);
        AddMismatch(variances, VarianceType.WrongAssetType, Severity.High, "AssetType", systemAsset.AssetType, physicalObservation.AssetType, 18);
        AddMismatch(variances, VarianceType.StickerMismatch, Severity.Medium, "StickerNumber", systemAsset.StickerNumber, physicalObservation.StickerNumber, 10);

        if (!physicalObservation.StickerPresent)
        {
            variances.Add(new VarianceDto(
                VarianceType.StickerMissing,
                Severity.High,
                "StickerPresent",
                "True",
                "False",
                "Asset sticker is missing and must be reissued before approval.",
                15));
        }

        if (!Equivalent(systemAsset.Location.DisplayName, physicalObservation.Location.DisplayName))
        {
            variances.Add(new VarianceDto(
                VarianceType.LocationChange,
                Severity.Medium,
                "Location",
                systemAsset.Location.DisplayName,
                physicalObservation.Location.DisplayName,
                "Physical location differs from inventory master.",
                14));
        }

        if (systemAsset.Status == AssetStatus.Inactive && physicalObservation.IsPhysicallyFound)
        {
            variances.Add(new VarianceDto(
                VarianceType.InactiveButAvailable,
                Severity.Critical,
                "AssetStatus",
                systemAsset.Status.ToString(),
                "Physically available",
                "Asset is marked inactive but is present in the office.",
                35));
        }

        if (systemAsset.Status == AssetStatus.Active && !physicalObservation.IsPhysicallyFound)
        {
            variances.Add(new VarianceDto(
                VarianceType.ActiveButMissing,
                Severity.Critical,
                "AssetStatus",
                systemAsset.Status.ToString(),
                "Missing",
                "Active asset could not be located physically.",
                35));
        }

        if (systemAsset.IsWarrantyExpired(today))
        {
            variances.Add(new VarianceDto(
                VarianceType.WarrantyExpired,
                Severity.Low,
                "WarrantyEndDate",
                systemAsset.WarrantyEndDate?.ToString("yyyy-MM-dd"),
                "Expired",
                "Warranty has expired; consider lifecycle action.",
                3));
        }

        if (physicalObservation.Condition?.Contains("damage", StringComparison.OrdinalIgnoreCase) == true)
        {
            variances.Add(new VarianceDto(
                VarianceType.AssetDamaged,
                Severity.High,
                "Condition",
                "Operational",
                physicalObservation.Condition,
                "Auditor marked the asset as damaged.",
                22));
        }

        var confidence = Math.Clamp(100 - variances.Sum(v => v.ConfidenceImpact), 0, 100);
        var outcome = SelectOutcome(variances, confidence, physicalObservation.IsPhysicallyFound);

        return new ReconciliationResultDto(outcome, confidence, variances, BuildRecommendation(outcome, variances));
    }

    public static IReadOnlyCollection<ReconciliationVariance> ToDomainVariances(
        ReconciliationResultDto result,
        Guid? assetId,
        Guid? verificationId) =>
        result.Variances
            .Select(variance => new ReconciliationVariance(
                assetId,
                verificationId,
                variance.Type,
                variance.Severity,
                variance.FieldName,
                variance.SystemValue,
                variance.PhysicalValue,
                variance.ConfidenceImpact,
                variance.Message))
            .ToArray();

    private static ReconciliationOutcome SelectOutcome(IReadOnlyCollection<VarianceDto> variances, int confidence, bool found)
    {
        if (!found)
        {
            return ReconciliationOutcome.MissingAsset;
        }

        if (variances.Count == 0)
        {
            return ReconciliationOutcome.ExactMatch;
        }

        return variances.Any(v => v.Severity == Severity.Critical) || confidence < 65
            ? ReconciliationOutcome.CriticalMismatch
            : ReconciliationOutcome.PartialMatch;
    }

    private static void AddMismatch(
        ICollection<VarianceDto> variances,
        VarianceType type,
        Severity severity,
        string fieldName,
        string? systemValue,
        string? physicalValue,
        int confidenceImpact)
    {
        if (string.IsNullOrWhiteSpace(systemValue) && string.IsNullOrWhiteSpace(physicalValue))
        {
            return;
        }

        if (Equivalent(systemValue, physicalValue))
        {
            return;
        }

        variances.Add(new VarianceDto(
            type,
            severity,
            fieldName,
            systemValue,
            physicalValue,
            $"{fieldName} differs between master inventory and physical observation.",
            confidenceImpact));
    }

    private static bool Equivalent(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string BuildRecommendation(ReconciliationOutcome outcome, IReadOnlyCollection<VarianceDto> variances) =>
        outcome switch
        {
            ReconciliationOutcome.ExactMatch => "Auto-close verification and retain immutable evidence trail.",
            ReconciliationOutcome.PartialMatch => "Route to Regional Manager for controlled master-data update approval.",
            ReconciliationOutcome.CriticalMismatch => "Hold reconciliation, require evidence attachment, and escalate to Inventory Admin.",
            ReconciliationOutcome.MissingAsset => "Start missing asset workflow and notify IT Support with SLA tracking.",
            ReconciliationOutcome.UnauthorizedAsset => "Create unauthorized asset case and quarantine from bulk updates.",
            _ when variances.Any(v => v.Type == VarianceType.DuplicateAssetTag) => "Merge duplicate candidates only after serial and custodian confirmation.",
            _ => "Review exception and capture supporting remarks."
        };
}
