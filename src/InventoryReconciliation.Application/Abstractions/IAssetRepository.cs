using InventoryReconciliation.Domain.Entities;

namespace InventoryReconciliation.Application.Abstractions;

public interface IAssetRepository
{
    IQueryable<Asset> Query();
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Asset?> GetByAssetTagAsync(string assetTag, CancellationToken cancellationToken = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
}
