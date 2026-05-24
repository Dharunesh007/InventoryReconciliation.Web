using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Application.Assets;
using InventoryReconciliation.Application.Reconciliation;
using InventoryReconciliation.Domain.Enums;
using InventoryReconciliation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InventoryReconciliation.Web.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/assets", async (
            string? search,
            int page,
            int pageSize,
            IAssetRepository assets,
            CancellationToken cancellationToken) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 10, 250);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = assets.Query();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(asset =>
                    asset.AssetTag.Contains(term) ||
                    (asset.SerialNumber != null && asset.SerialNumber.Contains(term)) ||
                    (asset.HostName != null && asset.HostName.Contains(term)) ||
                    (asset.UserName != null && asset.UserName.Contains(term)));
            }

            var pageItems = await query
                .OrderBy(asset => asset.AssetTag)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return pageItems.Select(asset => new AssetListItemDto(
                asset.Id,
                asset.AssetTag,
                asset.SerialNumber,
                asset.HostName,
                asset.AssetType,
                asset.Manufacturer,
                asset.ModelNumber,
                asset.UserName,
                asset.EmployeeId,
                asset.Department,
                asset.Location.DisplayName,
                asset.Status,
                asset.IsWarrantyExpired(today),
                asset.Verifications.SelectMany(verification => verification.Variances).Count()));
        });

        group.MapGet("/assets/{id:guid}", async (Guid id, IAssetRepository assets, CancellationToken cancellationToken) =>
        {
            var asset = await assets.GetByIdAsync(id, cancellationToken);
            if (asset is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new AssetDetailDto(
                asset.Id,
                asset.AssetTag,
                asset.SerialNumber,
                asset.HostName,
                asset.AssetType,
                asset.Manufacturer,
                asset.ModelNumber,
                asset.UserName,
                asset.EmployeeId,
                asset.Department,
                asset.CostCenter,
                asset.Location.DisplayName,
                asset.Status,
                asset.PurchaseDate,
                asset.WarrantyEndDate,
                asset.AuditLogs
                    .OrderByDescending(log => log.CreatedAtUtc)
                    .Select(log => new AssetAuditDto(log.CreatedAtUtc, log.EventType.ToString(), log.FieldName, log.PreviousValue, log.NewValue, log.CreatedBy, log.Remarks))
                    .ToArray()));
        });

        group.MapPost("/imports/preview", async (
            IFormFile file,
            IExcelInventoryReader reader,
            CancellationToken cancellationToken) =>
        {
            const long maxUploadBytes = 50 * 1024 * 1024;

            if (file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Upload a non-empty .xlsx workbook.");
            }

            if (file.Length > maxUploadBytes)
            {
                return Results.BadRequest("Workbook exceeds the 50 MB upload limit.");
            }

            await using var stream = file.OpenReadStream();
            var preview = await reader.PreviewAsync(stream, file.FileName, cancellationToken);
            return Results.Ok(preview);
        })
        .RequireAuthorization("CanImportInventory")
        .DisableAntiforgery();

        group.MapPost("/reconcile/preview", async (
            VerificationInputDto input,
            IAssetRepository assets,
            IReconciliationEngine engine,
            CancellationToken cancellationToken) =>
        {
            var asset = input.AssetId.HasValue
                ? await assets.GetByIdAsync(input.AssetId.Value, cancellationToken)
                : !string.IsNullOrWhiteSpace(input.AssetTag)
                    ? await assets.GetByAssetTagAsync(input.AssetTag, cancellationToken)
                    : null;

            var observation = new PhysicalObservation(
                input.AssetTag,
                input.SerialNumber,
                input.HostName,
                input.UserName,
                input.EmployeeId,
                input.Department,
                new AssetLocation(null, input.Location, input.Building, input.Floor, input.SeatOrCubicle),
                input.AssetType,
                input.StickerNumber,
                input.Condition,
                AssetStatus.Active,
                input.StickerPresent,
                input.IsPhysicallyFound,
                input.Remarks,
                input.Latitude,
                input.Longitude,
                input.DeviceFingerprint);

            return Results.Ok(engine.Compare(asset, observation, DateOnly.FromDateTime(DateTime.UtcNow)));
        })
        .RequireAuthorization("CanVerifyAssets");

        group.MapGet("/inventory/export", async (
            IWorkbookAssetEditWriter workbookWriter,
            CancellationToken cancellationToken) =>
        {
            var workbookBytes = await workbookWriter.ExportEditedWorkbookAsync(cancellationToken);
            var fileName = $"IT-Asset-Inventory-Edited-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return Results.File(
                workbookBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        });

        return app;
    }
}
