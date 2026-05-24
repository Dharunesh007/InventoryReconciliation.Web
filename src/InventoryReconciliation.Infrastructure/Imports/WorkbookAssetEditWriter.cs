using ClosedXML.Excel;
using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Application.Assets;
using Microsoft.Extensions.Configuration;

namespace InventoryReconciliation.Infrastructure.Imports;

public sealed class WorkbookAssetEditWriter(IConfiguration configuration, IAssetEditStore assetEditStore) : IWorkbookAssetEditWriter
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task SaveEditsAsync(IEnumerable<AssetEditRequest> requests, CancellationToken cancellationToken = default)
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
            var file = GetWorkbookFile();
            using var workbook = LoadWorkbook(file);
            ApplyEdits(workbook, requestArray);
            SaveWorkbookToSource(workbook, file);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<byte[]> ExportEditedWorkbookAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var file = GetWorkbookFile();
            using var workbook = LoadWorkbook(file);
            var edits = await assetEditStore.GetAllAsync(cancellationToken);
            ApplyEdits(workbook, edits.Values.Select(ToRequest));

            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return output.ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    private FileInfo GetWorkbookFile()
    {
        var path = Environment.ExpandEnvironmentVariables(
            configuration["InventorySource:UploadedWorkbookPath"]
            ?? @"%USERPROFILE%\Downloads\IT Asset inv.xlsx");
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException($"Uploaded workbook was not found: {file.FullName}", file.FullName);
        }

        return file;
    }

    private static XLWorkbook LoadWorkbook(FileInfo file)
    {
        using var source = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var workbookBytes = new MemoryStream();
        source.CopyTo(workbookBytes);
        workbookBytes.Position = 0;
        return new XLWorkbook(workbookBytes);
    }

    private static void SaveWorkbookToSource(XLWorkbook workbook, FileInfo file)
    {
        var tempPath = Path.Combine(file.DirectoryName!, $"{Path.GetFileNameWithoutExtension(file.Name)}.tmp-{Guid.NewGuid():N}.xlsx");
        try
        {
            workbook.SaveAs(tempPath);
            File.Copy(tempPath, file.FullName, overwrite: true);
        }
        catch (IOException exception)
        {
            throw new IOException($"Could not write changes to {file.FullName}. Close the Excel workbook if it is open, then save again.", exception);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void ApplyEdits(XLWorkbook workbook, IEnumerable<AssetEditRequest> requests)
    {
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return;
        }

        var headers = usedRange.FirstRow().Cells()
            .Select(cell => cell.GetFormattedString())
            .ToArray();
        var headerIndex = BuildHeaderIndex(headers);
        var assetTagColumn = FindColumn(headerIndex, "ASSET TAG", "ASSETTAG", "ASSET ID");
        if (assetTagColumn is null)
        {
            throw new InvalidOperationException("The workbook does not contain an Asset Tag column.");
        }

        var rowsByAssetTag = usedRange.RowsUsed()
            .Skip(1)
            .Select(row => new { Key = ReadText(row.Cell(assetTagColumn.Value)), Row = row })
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key.Trim(), group => group.First().Row, StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            if (!rowsByAssetTag.TryGetValue(request.SourceAssetTag.Trim(), out var row) &&
                !rowsByAssetTag.TryGetValue(request.AssetTag.Trim(), out row))
            {
                continue;
            }

            WriteText(row, headerIndex, request.AssetTag, "ASSET TAG", "ASSETTAG", "ASSET ID");
            WriteText(row, headerIndex, request.SerialNumber, "SERIAL NUMBER", "SERIALNO", "SERIAL");
            WriteText(row, headerIndex, request.HostName, "HOST NAME", "HOSTNAME");
            WriteText(row, headerIndex, request.UserName, "USER NAME(DONT REFER)", "USERNAME", "CUSTODIAN");
            WriteText(row, headerIndex, request.EmployeeId, "EMP. ID", "EMP ID", "EMPLOYEE ID");
            WriteText(row, headerIndex, request.Department, "DEPARTMENT", "DEPT");
            WriteText(row, headerIndex, request.AssetFloor, "ASSET FLOOR", "FLOOR", "LOCATION");
            WriteText(row, headerIndex, request.AssetType, "ASSET TYPE", "TYPE");
            WriteText(row, headerIndex, request.AssetCategory, "CAT", "ASSET CATEGORY", "CATEGORY");
            WriteText(row, headerIndex, request.SingleOrGroup, "SINGLE/GROUP");
            WriteText(row, headerIndex, request.AssetStatus, "ASSET STATUS", "STATUS");
            WriteText(row, headerIndex, request.Manufacturer, "MANUFACTURER");
            WriteText(row, headerIndex, request.ModelNumber, "MODEL NUMBER", "MODEL");
            WriteDate(row, headerIndex, request.WarrantyStart, "WARRANTY START");
            WriteDate(row, headerIndex, request.WarrantyEnd, "WARRANTY END");
            WriteText(row, headerIndex, request.PurchaseOrderNumber, "PO NUMBER");
            WriteDate(row, headerIndex, request.PurchaseOrderDate, "PO DATE");
            WriteText(row, headerIndex, request.InvoiceNumber, "INVOICE NO");
            WriteDate(row, headerIndex, request.InvoiceDate, "INVOICE DATE");
            WriteText(row, headerIndex, request.Remarks, "REMARKS");
            WriteText(row, headerIndex, request.WindowsPatch, "WINDOWS PATCH");
            WriteText(row, headerIndex, request.SentinelStatus, "SENTINEL STATUS");
        }
    }

    private static Dictionary<string, int> BuildHeaderIndex(IEnumerable<string> headers) =>
        headers
            .Select((header, index) => new { Key = Normalize(header), Index = index + 1 })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

    private static int? FindColumn(IReadOnlyDictionary<string, int> headerIndex, params string[] names)
    {
        foreach (var name in names)
        {
            if (headerIndex.TryGetValue(Normalize(name), out var index))
            {
                return index;
            }
        }

        return null;
    }

    private static void WriteText(IXLRangeRow row, IReadOnlyDictionary<string, int> headerIndex, string? value, params string[] names)
    {
        var column = FindColumn(headerIndex, names);
        if (column is null)
        {
            return;
        }

        var cell = row.Cell(column.Value);
        if (string.IsNullOrWhiteSpace(value))
        {
            cell.Clear(XLClearOptions.Contents);
            return;
        }

        cell.Value = value.Trim();
    }

    private static void WriteDate(IXLRangeRow row, IReadOnlyDictionary<string, int> headerIndex, DateOnly? value, params string[] names)
    {
        var column = FindColumn(headerIndex, names);
        if (column is null)
        {
            return;
        }

        var cell = row.Cell(column.Value);
        if (value is null)
        {
            cell.Clear(XLClearOptions.Contents);
            return;
        }

        cell.Value = value.Value.ToDateTime(TimeOnly.MinValue);
        cell.Style.DateFormat.Format = "yyyy-mm-dd";
    }

    private static AssetEditRequest ToRequest(AssetEditEntry entry) =>
        new(
            entry.SourceAssetTag,
            entry.AssetTag,
            entry.SerialNumber,
            entry.HostName,
            entry.UserName,
            entry.EmployeeId,
            entry.Department,
            entry.AssetFloor,
            entry.AssetType,
            entry.AssetCategory,
            entry.SingleOrGroup,
            entry.AssetStatus,
            entry.Manufacturer,
            entry.ModelNumber,
            entry.WarrantyStart,
            entry.WarrantyEnd,
            entry.PurchaseOrderNumber,
            entry.PurchaseOrderDate,
            entry.InvoiceNumber,
            entry.InvoiceDate,
            entry.Remarks,
            entry.WindowsPatch,
            entry.SentinelStatus);

    private static string? ReadText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        var value = cell.GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Normalize(string value) =>
        new(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
}
