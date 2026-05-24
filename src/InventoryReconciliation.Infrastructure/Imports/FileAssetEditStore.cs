using System.Text.Json;
using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Application.Assets;
using Microsoft.Extensions.Hosting;

namespace InventoryReconciliation.Infrastructure.Imports;

public sealed class FileAssetEditStore(IHostEnvironment environment) : IAssetEditStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _filePath = Path.Combine(environment.ContentRootPath, "App_Data", "asset-edits.json");

    public Task<DateTimeOffset?> GetRevisionAsync(CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(_filePath);
        return Task.FromResult<DateTimeOffset?>(file.Exists ? file.LastWriteTimeUtc : null);
    }

    public async Task<IReadOnlyDictionary<string, AssetEditEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadEntriesAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task SaveAsync(AssetEditRequest request, CancellationToken cancellationToken = default) =>
        SaveManyAsync([request], cancellationToken);

    public async Task SaveManyAsync(IEnumerable<AssetEditRequest> requests, CancellationToken cancellationToken = default)
    {
        var requestArray = requests
            .Where(request => !string.IsNullOrWhiteSpace(request.SourceAssetTag))
            .ToArray();

        if (requestArray.Length == 0)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadEntriesAsync(cancellationToken);
            var savedAt = DateTimeOffset.UtcNow;

            foreach (var request in requestArray)
            {
                entries[request.SourceAssetTag.Trim()] = ToEntry(request, savedAt);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, entries.Values.OrderBy(entry => entry.SourceAssetTag), SerializerOptions, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<UploadedInventorySnapshot> ApplyEditsAsync(UploadedInventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var edits = await GetAllAsync(cancellationToken);
        if (edits.Count == 0)
        {
            return snapshot;
        }

        var assets = snapshot.Assets
            .Select(asset => edits.TryGetValue(asset.AssetTag, out var edit) ? Apply(asset, edit) : asset)
            .ToArray();

        return snapshot with
        {
            Assets = assets,
            TotalRows = assets.Length,
            DuplicateAssetTags = Duplicates(assets.Select(asset => asset.AssetTag)),
            DuplicateSerialNumbers = Duplicates(assets.Select(asset => asset.SerialNumber)),
            DuplicateHostNames = Duplicates(assets.Select(asset => asset.HostName))
        };
    }

    private async Task<Dictionary<string, AssetEditEntry>> ReadEntriesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, AssetEditEntry>(StringComparer.OrdinalIgnoreCase);
        }

        await using var stream = File.OpenRead(_filePath);
        var entries = await JsonSerializer.DeserializeAsync<AssetEditEntry[]>(stream, SerializerOptions, cancellationToken) ?? [];
        return entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.SourceAssetTag))
            .GroupBy(entry => entry.SourceAssetTag.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
    }

    private static AssetEditEntry ToEntry(AssetEditRequest request, DateTimeOffset savedAt) =>
        new AssetEditEntry(
            request.SourceAssetTag.Trim(),
            Clean(request.AssetTag) ?? request.SourceAssetTag.Trim(),
            Clean(request.SerialNumber),
            Clean(request.HostName),
            Clean(request.UserName),
            Clean(request.EmployeeId),
            Clean(request.Department),
            Clean(request.AssetFloor),
            Clean(request.AssetType),
            Clean(request.AssetCategory),
            Clean(request.SingleOrGroup),
            Clean(request.AssetStatus),
            Clean(request.Manufacturer),
            Clean(request.ModelNumber),
            request.WarrantyStart,
            request.WarrantyEnd,
            Clean(request.PurchaseOrderNumber),
            request.PurchaseOrderDate,
            Clean(request.InvoiceNumber),
            request.InvoiceDate,
            Clean(request.Remarks),
            Clean(request.WindowsPatch),
            Clean(request.SentinelStatus),
            savedAt)
        {
            Changes = request.Changes
                .Where(change => !string.IsNullOrWhiteSpace(change.ChangeType))
                .Select(change => change with
                {
                    FieldName = Clean(change.FieldName) ?? "Field",
                    ChangeType = Clean(change.ChangeType) ?? "Other Update",
                    PreviousValue = Clean(change.PreviousValue),
                    NewValue = Clean(change.NewValue)
                })
                .ToArray()
        };

    private static UploadedAssetRecord Apply(UploadedAssetRecord asset, AssetEditEntry edit) =>
        asset with
        {
            AssetTag = edit.AssetTag,
            SerialNumber = edit.SerialNumber,
            HostName = edit.HostName,
            UserName = edit.UserName,
            EmployeeId = edit.EmployeeId,
            Department = edit.Department,
            AssetFloor = edit.AssetFloor,
            AssetType = edit.AssetType,
            AssetCategory = edit.AssetCategory,
            SingleOrGroup = edit.SingleOrGroup,
            AssetStatus = edit.AssetStatus,
            Manufacturer = edit.Manufacturer,
            ModelNumber = edit.ModelNumber,
            WarrantyStart = edit.WarrantyStart,
            WarrantyEnd = edit.WarrantyEnd,
            PurchaseOrderNumber = edit.PurchaseOrderNumber,
            PurchaseOrderDate = edit.PurchaseOrderDate,
            InvoiceNumber = edit.InvoiceNumber,
            InvoiceDate = edit.InvoiceDate,
            Remarks = edit.Remarks,
            WindowsPatch = edit.WindowsPatch,
            SentinelStatus = edit.SentinelStatus
        };

    private static IReadOnlyList<DuplicateValueProfile> Duplicates(IEnumerable<string?> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim().ToUpperInvariant())
            .GroupBy(value => value)
            .Where(group => group.Count() > 1)
            .Select(group => new DuplicateValueProfile(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Value)
            .ToArray();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
