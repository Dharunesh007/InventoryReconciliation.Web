using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryReconciliation.Infrastructure.Data;

public sealed class EfAssetRepository(AppDbContext dbContext) : IAssetRepository
{
    public IQueryable<Asset> Query() =>
        dbContext.Assets
            .AsNoTracking()
            .Include(asset => asset.Verifications)
            .ThenInclude(verification => verification.Variances);

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assets
            .Include(asset => asset.AuditLogs)
            .Include(asset => asset.Verifications)
            .ThenInclude(verification => verification.Variances)
            .FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public Task<Asset?> GetByAssetTagAsync(string assetTag, CancellationToken cancellationToken = default)
    {
        var normalized = assetTag.Trim().ToUpperInvariant();
        return dbContext.Assets
            .Include(asset => asset.AuditLogs)
            .Include(asset => asset.Verifications)
            .ThenInclude(verification => verification.Variances)
            .FirstOrDefaultAsync(asset => asset.NormalizedAssetTag == normalized, cancellationToken);
    }

    public Task AddAsync(Asset asset, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AddAsync(asset, cancellationToken).AsTask();
}
