using ClosedXML.Excel;
using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Application.Imports;

namespace InventoryReconciliation.Infrastructure.Imports;

public sealed class ClosedXmlInventoryReader(InventoryImportValidator validator) : IExcelInventoryReader
{
    public Task<ImportPreviewResult> PreviewAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed() ?? throw new InvalidOperationException("The workbook does not contain any data.");

        var headers = usedRange.FirstRow().Cells().Select(cell => cell.GetFormattedString().Trim()).ToArray();
        var rows = usedRange.RowsUsed().Skip(1).Take(5000).ToArray();
        var profiles = new List<ExcelColumnProfile>();

        for (var index = 0; index < headers.Length; index++)
        {
            var values = rows
                .Select(row => ReadCell(row.Cell(index + 1))?.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            profiles.Add(new ExcelColumnProfile(
                headers[index],
                null,
                values.Length,
                values.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                values.Count(value => value!.StartsWith('#')),
                values.Take(4).Cast<string>().ToArray()));
        }

        var mapping = validator.SuggestMapping(headers);
        var issues = validator.ValidateHeaders(headers, mapping).ToList();
        issues.AddRange(DetectDuplicateAssetTags(headers, rows));

        var previewRows = rows
            .Take(25)
            .Select(row => headers
                .Select((header, index) => new { header, value = ReadCell(row.Cell(index + 1)) })
                .ToDictionary(pair => pair.header, pair => pair.value, StringComparer.OrdinalIgnoreCase) as IReadOnlyDictionary<string, object?>)
            .ToArray();

        var mappedProfiles = profiles
            .Select(profile => profile with
            {
                SuggestedTargetField = mapping.TryGetValue(profile.SourceColumn, out var target) ? target : null
            })
            .ToArray();

        return Task.FromResult(new ImportPreviewResult(
            usedRange.RowCount() - 1,
            headers.Length,
            mappedProfiles,
            issues,
            previewRows,
            mapping));
    }

    private static IEnumerable<ImportValidationIssue> DetectDuplicateAssetTags(string[] headers, IReadOnlyCollection<IXLRangeRow> rows)
    {
        var assetTagIndex = Array.FindIndex(headers, header => header.Equals("ASSET TAG", StringComparison.OrdinalIgnoreCase) || header.Equals("Asset Tag", StringComparison.OrdinalIgnoreCase));
        if (assetTagIndex < 0)
        {
            yield break;
        }

        var duplicateTags = rows
            .Select((row, index) => new { RowNumber = index + 2, AssetTag = ReadCell(row.Cell(assetTagIndex + 1))?.ToString()?.Trim() })
            .Where(value => !string.IsNullOrWhiteSpace(value.AssetTag))
            .GroupBy(value => value.AssetTag!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicateTags.Take(50))
        {
            yield return new ImportValidationIssue(
                duplicate.First().RowNumber,
                "Warning",
                "DUPLICATE_ASSET_TAG",
                $"Duplicate asset tag detected: {duplicate.Key}.",
                headers[assetTagIndex]);
        }
    }

    private static object? ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return cell.GetDouble();
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            return cell.GetDateTime();
        }

        return cell.GetFormattedString();
    }
}
