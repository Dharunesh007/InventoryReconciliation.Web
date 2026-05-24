namespace InventoryReconciliation.Application.Dashboard;

public sealed record ExecutiveDashboardDto(
    int TotalAssets,
    int VerifiedAssets,
    int PendingVerification,
    decimal ReconciliationCompletedPercent,
    int MismatchCount,
    int MissingAssets,
    int NewAssetsDiscovered,
    int DamagedAssets,
    int AssetsWithoutStickers,
    int OwnershipMismatch,
    IReadOnlyCollection<NamedMetricDto> VarianceByType,
    IReadOnlyCollection<NamedMetricDto> VerificationByDepartment,
    IReadOnlyCollection<NamedMetricDto> VerificationByBuilding,
    IReadOnlyCollection<TrendPointDto> DailyVerificationTrend);

public sealed record NamedMetricDto(string Name, int Count, decimal Percent);
public sealed record TrendPointDto(DateOnly Date, int Verified, int Variances);
