using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Domain.Enums;

namespace InventoryReconciliation.Application.Dashboard;

public interface IExecutiveDashboardService
{
    Task<ExecutiveDashboardDto> BuildAsync(CancellationToken cancellationToken = default);
}

public sealed class ExecutiveDashboardService(IAssetRepository assets) : IExecutiveDashboardService
{
    public Task<ExecutiveDashboardDto> BuildAsync(CancellationToken cancellationToken = default)
    {
        var assetList = assets.Query().ToList();
        var verifications = assetList.SelectMany(asset => asset.Verifications).ToArray();
        var variances = verifications.SelectMany(verification => verification.Variances).ToArray();

        var totalAssets = assetList.Count;
        var verifiedAssets = verifications.Count(verification => verification.Status is VerificationStatus.Verified or VerificationStatus.Approved);
        var pending = Math.Max(totalAssets - verifiedAssets, 0);

        IReadOnlyCollection<NamedMetricDto> GroupByVariance() =>
            variances
                .GroupBy(variance => variance.Type.ToString())
                .Select(group => new NamedMetricDto(group.Key, group.Count(), Percent(group.Count(), Math.Max(variances.Length, 1))))
                .OrderByDescending(metric => metric.Count)
                .Take(8)
                .ToArray();

        IReadOnlyCollection<NamedMetricDto> GroupByDepartment() =>
            assetList
                .GroupBy(asset => string.IsNullOrWhiteSpace(asset.Department) ? "Unassigned" : asset.Department)
                .Select(group => new NamedMetricDto(group.Key!, group.Count(), Percent(group.Count(), Math.Max(totalAssets, 1))))
                .OrderByDescending(metric => metric.Count)
                .Take(10)
                .ToArray();

        IReadOnlyCollection<NamedMetricDto> GroupByBuilding() =>
            assetList
                .GroupBy(asset => string.IsNullOrWhiteSpace(asset.Location.Building) ? "Unknown" : asset.Location.Building)
                .Select(group => new NamedMetricDto(group.Key!, group.Count(), Percent(group.Count(), Math.Max(totalAssets, 1))))
                .OrderByDescending(metric => metric.Count)
                .Take(10)
                .ToArray();

        var trend = verifications
            .GroupBy(verification => DateOnly.FromDateTime(verification.CreatedAtUtc.UtcDateTime.Date))
            .OrderBy(group => group.Key)
            .TakeLast(14)
            .Select(group => new TrendPointDto(group.Key, group.Count(), group.Sum(verification => verification.Variances.Count)))
            .ToArray();

        var dashboard = new ExecutiveDashboardDto(
            totalAssets,
            verifiedAssets,
            pending,
            Percent(verifiedAssets, Math.Max(totalAssets, 1)),
            variances.Length,
            variances.Count(variance => variance.Type == VarianceType.AssetNotFound || variance.Type == VarianceType.ActiveButMissing),
            variances.Count(variance => variance.Type == VarianceType.NewUnidentifiedAsset || variance.Type == VarianceType.ExtraAssetFound),
            variances.Count(variance => variance.Type == VarianceType.AssetDamaged),
            variances.Count(variance => variance.Type == VarianceType.StickerMissing),
            variances.Count(variance => variance.Type == VarianceType.UserNameMismatch || variance.Type == VarianceType.CustodianChange),
            GroupByVariance(),
            GroupByDepartment(),
            GroupByBuilding(),
            trend);

        return Task.FromResult(dashboard);
    }

    private static decimal Percent(int numerator, int denominator) =>
        denominator == 0 ? 0 : Math.Round((decimal)numerator / denominator * 100, 1);
}
