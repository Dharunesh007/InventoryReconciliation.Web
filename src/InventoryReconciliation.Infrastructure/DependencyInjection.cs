using Azure.Storage.Blobs;
using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Infrastructure.Data;
using InventoryReconciliation.Infrastructure.Imports;
using InventoryReconciliation.Infrastructure.Security;
using InventoryReconciliation.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryReconciliation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb")
            ?? "Server=(localdb)\\mssqllocaldb;Database=InventoryReconciliation;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
                sql.CommandTimeout(60);
            }));

        services.AddScoped<IAssetRepository, EfAssetRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IExcelInventoryReader, ClosedXmlInventoryReader>();
        services.AddSingleton<IAssetEditStore, FileAssetEditStore>();
        services.AddSingleton<IWorkbookAssetEditWriter, WorkbookAssetEditWriter>();
        services.AddSingleton<IUploadedInventorySource, WorkbookUploadedInventorySource>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        var blobConnection = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrWhiteSpace(blobConnection))
        {
            services.AddSingleton(new BlobContainerClient(blobConnection, configuration["Storage:AttachmentContainer"] ?? "asset-evidence"));
            services.AddScoped<IAttachmentStore, AzureBlobAttachmentStore>();
        }

        return services;
    }
}
