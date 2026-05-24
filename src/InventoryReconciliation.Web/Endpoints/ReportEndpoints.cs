using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using InventoryReconciliation.Application.Abstractions;
using InventoryReconciliation.Application.Assets;

namespace InventoryReconciliation.Web.Endpoints;

public static class ReportEndpoints
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly ReportDefinition[] ReportDefinitions =
    [
        new("verification-summary", "Verification Summary", ["xlsx", "csv", "pdf"]),
        new("variance-summary", "Variance Summary", ["xlsx", "csv", "pdf"]),
        new("missing-assets", "Missing Assets", ["csv", "xlsx", "pdf"]),
        new("department-variance", "Department Variance", ["xlsx", "csv", "pdf"]),
        new("location-changes", "Location Changes", ["xlsx", "csv", "pdf"]),
        new("auditor-productivity", "Auditor Productivity", ["csv", "xlsx", "pdf"]),
        new("asset-aging", "Asset Aging", ["xlsx", "csv", "pdf"]),
        new("compliance-report", "Compliance Report", ["pdf", "xlsx", "csv"])
    ];

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").RequireAuthorization("CanViewExecutiveReports");

        group.MapGet("/{reportSlug}/{format}", async (
            string reportSlug,
            string format,
            IUploadedInventorySource inventorySource,
            IAssetEditStore editStore,
            CancellationToken cancellationToken) =>
        {
            var definition = FindReport(reportSlug);
            if (definition is null)
            {
                return Results.NotFound($"Unknown report: {reportSlug}");
            }

            format = NormalizeFormat(format);
            if (!definition.Formats.Contains(format, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest($"{definition.Title} does not support {format} export.");
            }

            var report = await BuildReportAsync(definition, inventorySource, editStore, cancellationToken);
            var fileStem = $"{definition.Slug}-{DateTime.Now:yyyyMMdd-HHmm}";

            return format switch
            {
                "xlsx" => Results.File(BuildExcel(report), ExcelContentType, $"{fileStem}.xlsx"),
                "csv" => Results.File(BuildCsv(report), "text/csv; charset=utf-8", $"{fileStem}.csv"),
                "pdf" => Results.File(BuildPdf(report), "application/pdf", $"{fileStem}.pdf"),
                _ => Results.BadRequest($"Unsupported format: {format}")
            };
        });

        group.MapGet("/pack", async (
            IUploadedInventorySource inventorySource,
            IAssetEditStore editStore,
            CancellationToken cancellationToken) =>
        {
            using var package = new MemoryStream();
            using (var archive = new ZipArchive(package, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var definition in ReportDefinitions)
                {
                    var report = await BuildReportAsync(definition, inventorySource, editStore, cancellationToken);
                    AddEntry(archive, $"{definition.Slug}.xlsx", BuildExcel(report));
                    AddEntry(archive, $"{definition.Slug}.csv", BuildCsv(report));
                }
            }

            return Results.File(package.ToArray(), "application/zip", $"inventory-report-pack-{DateTime.Now:yyyyMMdd-HHmm}.zip");
        });

        return app;
    }

    private static async Task<ReportDocument> BuildReportAsync(
        ReportDefinition definition,
        IUploadedInventorySource inventorySource,
        IAssetEditStore editStore,
        CancellationToken cancellationToken)
    {
        var snapshot = await inventorySource.GetSnapshotAsync(cancellationToken);
        var edits = (await editStore.GetAllAsync(cancellationToken)).Values
            .OrderByDescending(entry => entry.SavedAtUtc)
            .ToArray();

        return definition.Slug switch
        {
            "verification-summary" => BuildVerificationSummary(snapshot, edits),
            "variance-summary" => BuildVarianceSummary(snapshot, edits),
            "missing-assets" => BuildMissingAssets(snapshot),
            "department-variance" => BuildDepartmentVariance(snapshot, edits),
            "location-changes" => BuildLocationChanges(snapshot, edits),
            "auditor-productivity" => BuildAuditorProductivity(snapshot, edits),
            "asset-aging" => BuildAssetAging(snapshot),
            "compliance-report" => BuildComplianceReport(snapshot, edits),
            _ => throw new InvalidOperationException($"Unknown report: {definition.Slug}")
        };
    }

    private static ReportDocument BuildVerificationSummary(UploadedInventorySnapshot snapshot, IReadOnlyCollection<AssetEditEntry> edits)
    {
        var totalChanges = edits.Sum(entry => (entry.Changes ?? []).Length);
        var summary = new[]
        {
            Row("Source workbook", snapshot.SourceFileName),
            Row("Total uploaded assets", snapshot.TotalRows),
            Row("Active assets", snapshot.ActiveAssets),
            Row("Pending verification", Math.Max(snapshot.TotalRows - edits.Count, 0)),
            Row("Edited assets", edits.Count),
            Row("Tracked change points", totalChanges),
            Row("Duplicate asset tags", snapshot.DuplicateAssetTags.Count),
            Row("Duplicate serial numbers", snapshot.DuplicateSerialNumbers.Count),
            Row("Generated at", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
        };

        var detailRows = snapshot.Assets
            .GroupBy(asset => Clean(asset.AssetStatus, "Blank"))
            .OrderByDescending(group => group.Count())
            .Select(group => Row(group.Key, group.Count(), Percent(group.Count(), snapshot.TotalRows)))
            .ToArray();

        return new ReportDocument(
            "Verification Summary",
            summary,
            ["Status", "Asset Count", "Percent"],
            detailRows.Length == 0 ? [Row("No asset rows", 0, "0%")] : detailRows);
    }

    private static ReportDocument BuildVarianceSummary(UploadedInventorySnapshot snapshot, IReadOnlyCollection<AssetEditEntry> edits)
    {
        var changeRows = edits
            .SelectMany(entry => (entry.Changes ?? []).Select(change => Row(
                entry.AssetTag,
                change.ChangeType,
                change.FieldName,
                Clean(change.PreviousValue, "Blank"),
                Clean(change.NewValue, "Blank"),
                entry.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))))
            .ToArray();

        var byType = edits
            .SelectMany(entry => entry.Changes ?? [])
            .GroupBy(change => Clean(change.ChangeType, "Other Update"))
            .OrderByDescending(group => group.Count())
            .Select(group => Row(group.Key, group.Count()))
            .ToArray();

        var summary = byType.Length == 0
            ? [Row("Tracked variance change points", 0), Row("Note", "Save edits in Asset Explorer to populate this report.")]
            : byType;

        return new ReportDocument(
            "Variance Summary",
            summary,
            ["Asset Tag", "Variance Type", "Field", "Previous Value", "New Value", "Saved At"],
            changeRows.Length == 0 ? [Row("No saved variances", "None", "None", "None", "None", "")] : changeRows);
    }

    private static ReportDocument BuildMissingAssets(UploadedInventorySnapshot snapshot)
    {
        var rows = snapshot.Assets
            .Where(asset =>
                Contains(asset.AssetStatus, "missing") ||
                Contains(asset.AssetStatus, "not found") ||
                Contains(asset.AssetStatus, "inactive") ||
                Contains(asset.AssetStatus, "not active"))
            .OrderBy(asset => asset.AssetTag)
            .Select(asset => Row(asset.AssetTag, asset.SerialNumber, asset.UserName, asset.Department, asset.AssetFloor, asset.AssetStatus, "Status indicates exception"))
            .ToArray();

        return new ReportDocument(
            "Missing Assets",
            [Row("Potential missing / inactive assets", rows.Length), Row("Source", snapshot.SourceFileName)],
            ["Asset Tag", "Serial Number", "User", "Department", "Location", "Status", "Reason"],
            rows.Length == 0 ? [Row("No missing asset records", "", "", "", "", "", "No physical missing status has been captured yet.")] : rows);
    }

    private static ReportDocument BuildDepartmentVariance(UploadedInventorySnapshot snapshot, IReadOnlyCollection<AssetEditEntry> edits)
    {
        var rows = edits
            .SelectMany(entry => (entry.Changes ?? [])
                .Where(change => change.ChangeType is "Department Change" or "User Mismatch" or "Custodian Change")
                .Select(change => Row(entry.AssetTag, change.ChangeType, change.FieldName, Clean(change.PreviousValue, "Blank"), Clean(change.NewValue, "Blank"), entry.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))))
            .ToArray();

        var summary = snapshot.Assets
            .GroupBy(asset => Clean(asset.Department, "Unknown department"))
            .OrderByDescending(group => group.Count())
            .Take(12)
            .Select(group => Row(group.Key, group.Count()))
            .ToArray();

        return new ReportDocument(
            "Department Variance",
            summary.Length == 0 ? [Row("Departments found", 0)] : summary,
            ["Asset Tag", "Change Type", "Field", "Previous Value", "New Value", "Saved At"],
            rows.Length == 0 ? [Row("No department/custodian variance", "", "", "", "", "")] : rows);
    }

    private static ReportDocument BuildLocationChanges(UploadedInventorySnapshot snapshot, IReadOnlyCollection<AssetEditEntry> edits)
    {
        var rows = edits
            .SelectMany(entry => (entry.Changes ?? [])
                .Where(change => change.ChangeType == "Location Change")
                .Select(change => Row(entry.AssetTag, change.FieldName, Clean(change.PreviousValue, "Blank"), Clean(change.NewValue, "Blank"), entry.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))))
            .ToArray();

        var summary = snapshot.Assets
            .GroupBy(asset => Clean(asset.AssetFloor, "Unknown location"))
            .OrderByDescending(group => group.Count())
            .Take(12)
            .Select(group => Row(group.Key, group.Count()))
            .ToArray();

        return new ReportDocument(
            "Location Changes",
            summary.Length == 0 ? [Row("Locations found", 0)] : summary,
            ["Asset Tag", "Field", "Previous Location", "New Location", "Saved At"],
            rows.Length == 0 ? [Row("No location changes saved", "", "", "", "")] : rows);
    }

    private static ReportDocument BuildAuditorProductivity(UploadedInventorySnapshot snapshot, IReadOnlyCollection<AssetEditEntry> edits)
    {
        var rows = edits
            .GroupBy(entry => entry.SavedAtUtc.ToLocalTime().Date)
            .OrderByDescending(group => group.Key)
            .Select(group => Row(
                group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                group.Count(),
                group.Sum(entry => (entry.Changes ?? []).Length),
                "Asset Explorer"))
            .ToArray();

        return new ReportDocument(
            "Auditor Productivity",
            [Row("Edited assets", edits.Count), Row("Tracked change points", edits.Sum(entry => (entry.Changes ?? []).Length)), Row("Total workbook rows", snapshot.TotalRows)],
            ["Date", "Saved Asset Edits", "Change Points", "Source"],
            rows.Length == 0 ? [Row("No saved activity", 0, 0, "Asset Explorer")] : rows);
    }

    private static ReportDocument BuildAssetAging(UploadedInventorySnapshot snapshot)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = snapshot.Assets
            .OrderBy(asset => asset.WarrantyEnd ?? DateOnly.MaxValue)
            .Select(asset =>
            {
                var category = asset.WarrantyEnd is null
                    ? "Unknown"
                    : asset.WarrantyEnd < today
                        ? "Expired"
                        : asset.WarrantyEnd <= today.AddDays(90)
                            ? "Expiring in 90 days"
                            : "Valid";

                return Row(asset.AssetTag, asset.AssetType, asset.Manufacturer, asset.ModelNumber, FormatDate(asset.WarrantyStart), FormatDate(asset.WarrantyEnd), category);
            })
            .ToArray();

        var summary = rows
            .GroupBy(row => row[6])
            .Select(group => Row(group.Key, group.Count()))
            .OrderBy(row => row[0])
            .ToArray();

        return new ReportDocument(
            "Asset Aging",
            summary.Length == 0 ? [Row("Warranty records", 0)] : summary,
            ["Asset Tag", "Asset Type", "Manufacturer", "Model", "Warranty Start", "Warranty End", "Lifecycle Status"],
            rows.Length == 0 ? [Row("No asset rows", "", "", "", "", "", "")] : rows);
    }

    private static ReportDocument BuildComplianceReport(UploadedInventorySnapshot snapshot, IReadOnlyCollection<AssetEditEntry> edits)
    {
        var rows = edits
            .SelectMany(entry => (entry.Changes ?? []).Select(change => Row(
                entry.AssetTag,
                change.ChangeType,
                change.FieldName,
                Clean(change.PreviousValue, "Blank"),
                Clean(change.NewValue, "Blank"),
                entry.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))))
            .ToArray();

        return new ReportDocument(
            "Compliance Report",
            [
                Row("Source workbook", snapshot.SourceFileName),
                Row("Total assets", snapshot.TotalRows),
                Row("Duplicate asset tags", snapshot.DuplicateAssetTags.Count),
                Row("Duplicate serial numbers", snapshot.DuplicateSerialNumbers.Count),
                Row("Formula error cells", snapshot.FormulaErrorCells),
                Row("Saved edited assets", edits.Count),
                Row("Immutable change points", edits.Sum(entry => (entry.Changes ?? []).Length)),
                Row("Generated at", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
            ],
            ["Asset Tag", "Event Type", "Field", "Previous Value", "New Value", "Event Time"],
            rows.Length == 0 ? [Row("No saved audit events", "", "", "", "", "")] : rows);
    }

    private static byte[] BuildExcel(ReportDocument report)
    {
        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = report.Title;
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;
        summarySheet.Cell(2, 1).Value = "Generated";
        summarySheet.Cell(2, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        WriteTable(summarySheet, 4, ["Metric", "Value"], report.SummaryRows);

        var detailsSheet = workbook.Worksheets.Add("Details");
        WriteTable(detailsSheet, 1, report.DetailColumns, report.DetailRows);

        summarySheet.Columns().AdjustToContents();
        detailsSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildCsv(ReportDocument report)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, [report.Title]);
        AppendCsvRow(builder, ["Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)]);
        builder.AppendLine();
        AppendCsvRow(builder, ["Summary"]);
        AppendCsvRow(builder, ["Metric", "Value"]);
        foreach (var row in report.SummaryRows)
        {
            AppendCsvRow(builder, row);
        }

        builder.AppendLine();
        AppendCsvRow(builder, ["Details"]);
        AppendCsvRow(builder, report.DetailColumns);
        foreach (var row in report.DetailRows)
        {
            AppendCsvRow(builder, row);
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private static byte[] BuildPdf(ReportDocument report)
    {
        var lines = new List<string>
        {
            report.Title,
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}",
            "",
            "Summary"
        };

        lines.AddRange(report.SummaryRows.Select(row => $"{row.ElementAtOrDefault(0)}: {row.ElementAtOrDefault(1)}"));
        lines.Add("");
        lines.Add("Details");
        lines.Add(string.Join(" | ", report.DetailColumns));
        lines.AddRange(report.DetailRows.Take(32).Select(row => string.Join(" | ", row)));

        return MinimalPdf.Create(report.Title, lines);
    }

    private static void WriteTable(IXLWorksheet sheet, int startRow, IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var cell = sheet.Cell(startRow, index + 1);
            cell.Value = columns[index];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2A44");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                sheet.Cell(startRow + rowIndex + 1, columnIndex + 1).Value = rows[rowIndex].ElementAtOrDefault(columnIndex) ?? string.Empty;
            }
        }

        if (rows.Count > 0)
        {
            sheet.Range(startRow, 1, startRow + rows.Count, columns.Count).CreateTable();
        }
    }

    private static void AddEntry(ZipArchive archive, string fileName, byte[] bytes)
    {
        var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes, 0, bytes.Length);
    }

    private static ReportDefinition? FindReport(string slug) =>
        ReportDefinitions.FirstOrDefault(report => string.Equals(report.Slug, slug, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeFormat(string value)
    {
        value = value.Trim().TrimStart('.').ToLowerInvariant();
        return value switch
        {
            "excel" => "xlsx",
            "pdf" => "pdf",
            "csv" => "csv",
            _ => value
        };
    }

    private static string[] Row(params object?[] values) =>
        values.Select(value => value switch
        {
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            null => string.Empty,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        }).ToArray();

    private static void AppendCsvRow(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsv(values[index]));
        }

        builder.AppendLine();
    }

    private static string EscapeCsv(string value) =>
        value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;

    private static bool Contains(string? value, string term) =>
        value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

    private static string Clean(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string FormatDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Percent(int value, int total) =>
        total <= 0 ? "0%" : $"{Math.Round(value / (double)total * 100, 1):0.#}%";

    private sealed record ReportDefinition(string Slug, string Title, string[] Formats);

    private sealed record ReportDocument(
        string Title,
        IReadOnlyList<string[]> SummaryRows,
        IReadOnlyList<string> DetailColumns,
        IReadOnlyList<string[]> DetailRows);

    private static class MinimalPdf
    {
        public static byte[] Create(string title, IReadOnlyList<string> lines)
        {
            var safeLines = lines
                .Select(line => Sanitize(line))
                .Chunk(44)
                .ToArray();

            if (safeLines.Length == 0)
            {
                safeLines = [[]];
            }

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                string.Empty,
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            };

            var pageObjectIds = new List<int>();
            foreach (var pageLines in safeLines)
            {
                var content = BuildContentStream(title, pageLines);
                var contentObjectId = objects.Count + 2;
                var pageObject = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>";
                pageObjectIds.Add(objects.Count + 1);
                objects.Add(pageObject);
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
            writer.WriteLine("%PDF-1.4");

            var offsets = new List<long> { 0 };
            for (var index = 0; index < objects.Count; index++)
            {
                writer.Flush();
                offsets.Add(stream.Position);
                writer.WriteLine($"{index + 1} 0 obj");
                writer.WriteLine(objects[index]);
                writer.WriteLine("endobj");
            }

            writer.Flush();
            var xrefOffset = stream.Position;
            writer.WriteLine("xref");
            writer.WriteLine($"0 {objects.Count + 1}");
            writer.WriteLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
            {
                writer.WriteLine($"{offset:0000000000} 00000 n ");
            }

            writer.WriteLine("trailer");
            writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("%%EOF");
            writer.Flush();
            return stream.ToArray();
        }

        private static string BuildContentStream(string title, IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BT");
            builder.AppendLine("/F1 16 Tf");
            builder.AppendLine("18 TL");
            builder.AppendLine($"50 760 Td ({Escape(Sanitize(title))}) Tj");
            builder.AppendLine("T*");
            builder.AppendLine("/F1 9 Tf");
            builder.AppendLine("12 TL");
            foreach (var line in lines)
            {
                builder.AppendLine($"({Escape(line)}) Tj");
                builder.AppendLine("T*");
            }
            builder.AppendLine("ET");
            return builder.ToString();
        }

        private static string Sanitize(string value) =>
            new(value.Select(character => character is >= ' ' and <= '~' ? character : '?').ToArray());

        private static string Escape(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
