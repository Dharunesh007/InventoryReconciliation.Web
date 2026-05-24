using ClosedXML.Excel;
using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Application.Assets;
using Microsoft.Extensions.Configuration;

namespace InventoryReconciliation.Infrastructure.Imports;

public sealed class WorkbookUploadedInventorySource(IConfiguration configuration, IAssetEditStore assetEditStore) : IUploadedInventorySource
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private UploadedInventorySnapshot? _cached;
    private DateTimeOffset? _cachedLastModified;
    private DateTimeOffset? _cachedEditRevision;

    public async Task<UploadedInventorySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var path = Environment.ExpandEnvironmentVariables(
            configuration["InventorySource:UploadedWorkbookPath"]
            ?? @"%USERPROFILE%\Downloads\IT Asset inv.xlsx");

        var file = new FileInfo(path);
        var lastModified = file.Exists ? file.LastWriteTimeUtc : (DateTime?)null;
        var editRevision = await assetEditStore.GetRevisionAsync(cancellationToken);

        if (_cached is not null && _cachedLastModified == lastModified && _cachedEditRevision == editRevision)
        {
            return _cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            editRevision = await assetEditStore.GetRevisionAsync(cancellationToken);
            if (_cached is not null && _cachedLastModified == lastModified && _cachedEditRevision == editRevision)
            {
                return _cached;
            }

            var snapshot = file.Exists
                ? LoadWorkbook(file)
                : EmptySnapshot(path);
            _cached = await assetEditStore.ApplyEditsAsync(snapshot, cancellationToken);
            _cachedLastModified = lastModified;
            _cachedEditRevision = editRevision;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static UploadedInventorySnapshot LoadWorkbook(FileInfo file)
    {
        using var source = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var workbookBytes = new MemoryStream();
        source.CopyTo(workbookBytes);
        workbookBytes.Position = 0;

        using var workbook = new XLWorkbook(workbookBytes);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return EmptySnapshot(file.FullName);
        }

        var headers = usedRange.FirstRow().Cells()
            .Select(cell => ReadText(cell) ?? string.Empty)
            .ToArray();

        var headerIndex = headers
            .Select((header, index) => new { Key = Normalize(header), Index = index + 1 })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

        var rows = usedRange.RowsUsed().Skip(1).ToArray();
        var assets = rows
            .Select(row => ToAsset(row, headerIndex))
            .Where(asset => !string.IsNullOrWhiteSpace(asset.AssetTag))
            .ToArray();

        var columns = headers.Select((header, index) =>
        {
            var values = rows
                .Select(row => ReadText(row.Cell(index + 1)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();

            return new UploadedColumnProfile(
                header,
                values.Length,
                values.Select(value => value.Trim().ToUpperInvariant()).Distinct().Count(),
                values.Count(IsFormulaErrorText),
                values.Where(value => !IsFormulaErrorText(value)).Distinct().Take(4).ToArray());
        }).ToArray();

        return new UploadedInventorySnapshot(
            file.FullName,
            file.Name,
            file.LastWriteTimeUtc,
            assets.Length,
            headers.Length,
            headers,
            assets,
            columns,
            Duplicates(assets.Select(asset => asset.AssetTag)),
            Duplicates(assets.Select(asset => asset.SerialNumber)),
            Duplicates(assets.Select(asset => asset.HostName)));
    }

    private static UploadedAssetRecord ToAsset(IXLRangeRow row, IReadOnlyDictionary<string, int> headerIndex) =>
        new(
            row.RowNumber(),
            Get(row, headerIndex, "ASSET TAG", "ASSETTAG", "ASSET ID") ?? string.Empty,
            Get(row, headerIndex, "SERIAL NUMBER", "SERIALNO", "SERIAL"),
            Get(row, headerIndex, "HOST NAME", "HOSTNAME"),
            Get(row, headerIndex, "USER NAME(DONT REFER)", "USERNAME", "CUSTODIAN"),
            Get(row, headerIndex, "EMP. ID", "EMP ID", "EMPLOYEE ID"),
            Get(row, headerIndex, "DEPARTMENT", "DEPT"),
            Get(row, headerIndex, "ASSET FLOOR", "FLOOR", "LOCATION"),
            Get(row, headerIndex, "ASSET TYPE", "TYPE"),
            Get(row, headerIndex, "CAT", "ASSET CATEGORY", "CATEGORY"),
            Get(row, headerIndex, "SINGLE/GROUP"),
            Get(row, headerIndex, "ASSET STATUS", "STATUS"),
            Get(row, headerIndex, "MANUFACTURER"),
            Get(row, headerIndex, "MODEL NUMBER", "MODEL"),
            GetDate(row, headerIndex, "WARRANTY START"),
            GetDate(row, headerIndex, "WARRANTY END"),
            Get(row, headerIndex, "PO NUMBER"),
            GetDate(row, headerIndex, "PO DATE"),
            Get(row, headerIndex, "INVOICE NO"),
            GetDate(row, headerIndex, "INVOICE DATE"),
            Get(row, headerIndex, "REMARKS"),
            Get(row, headerIndex, "WINDOWS PATCH"),
            Get(row, headerIndex, "SENTINEL STATUS"));

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

    private static string? Get(IXLRangeRow row, IReadOnlyDictionary<string, int> headerIndex, params string[] names)
    {
        foreach (var name in names)
        {
            if (headerIndex.TryGetValue(Normalize(name), out var index))
            {
                var value = ReadText(row.Cell(index));
                return IsFormulaErrorText(value) ? null : value;
            }
        }

        return null;
    }

    private static DateOnly? GetDate(IXLRangeRow row, IReadOnlyDictionary<string, int> headerIndex, params string[] names)
    {
        foreach (var name in names)
        {
            if (!headerIndex.TryGetValue(Normalize(name), out var index))
            {
                continue;
            }

            var cell = row.Cell(index);
            try
            {
                if (cell.DataType == XLDataType.DateTime)
                {
                    return DateOnly.FromDateTime(cell.GetDateTime());
                }

                if (cell.DataType == XLDataType.Number)
                {
                    return DateOnly.FromDateTime(DateTime.FromOADate(cell.GetDouble()));
                }

                var text = ReadText(cell);
                if (DateTime.TryParse(text, out var parsed))
                {
                    return DateOnly.FromDateTime(parsed);
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string? ReadText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        try
        {
            var value = cell.GetFormattedString().Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsFormulaErrorText(string? value) =>
        value?.StartsWith('#') == true;

    private static string Normalize(string value) =>
        new(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static UploadedInventorySnapshot EmptySnapshot(string path) =>
        new(
            path,
            Path.GetFileName(path),
            null,
            0,
            0,
            [],
            [],
            [],
            [],
            [],
            []);
}
