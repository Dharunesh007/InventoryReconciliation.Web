namespace InventoryReconciliation.Application.Imports;

public sealed class InventoryImportValidator
{
    public static readonly IReadOnlyCollection<string> MandatoryFields =
    [
        "AssetTag",
        "AssetType"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> Synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["AssetTag"] = ["ASSET TAG", "Asset ID", "AssetId", "Asset Tag", "Sticker Number"],
        ["SerialNumber"] = ["SERIAL NUMBER", "Serial No", "Serial"],
        ["HostName"] = ["Host Name", "Hostname", "Computer Name"],
        ["UserName"] = ["USER NAME(DONT REFER)", "USER NAME(DON'T REFER)", "User Name", "Assigned User", "Custodian"],
        ["EmployeeId"] = ["Emp. ID", "Employee ID", "Emp ID"],
        ["Department"] = ["Department", "Dept"],
        ["AssetType"] = ["Asset Type", "Type"],
        ["AssetCategory"] = ["CAT", "Asset Category", "Category"],
        ["Location"] = ["Location", "Asset Floor", "Office"],
        ["Floor"] = ["Floor", "Asset Floor"],
        ["Manufacturer"] = ["MANUFACTURER", "Manufacturer"],
        ["ModelNumber"] = ["MODEL NUMBER", "Model", "Model Number"],
        ["WarrantyStartDate"] = ["WARRANTY START", "Warranty Start"],
        ["WarrantyEndDate"] = ["WARRANTY END", "Warranty End"],
        ["PurchaseOrderNumber"] = ["PO NUMBER", "PO Number"],
        ["PurchaseDate"] = ["PO DATE", "Purchase Date"],
        ["InvoiceNumber"] = ["INVOICE NO", "Invoice No"],
        ["Remarks"] = ["REMARKS", "Remarks"]
    };

    public IReadOnlyDictionary<string, string> SuggestMapping(IEnumerable<string> sourceColumns)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sourceColumns)
        {
            var normalizedSource = Normalize(source);
            var match = Synonyms.FirstOrDefault(pair => pair.Value.Any(alias => Normalize(alias) == normalizedSource));
            if (!string.IsNullOrWhiteSpace(match.Key))
            {
                mapping[source] = match.Key;
            }
        }

        return mapping;
    }

    public IReadOnlyCollection<ImportValidationIssue> ValidateHeaders(IEnumerable<string> sourceColumns, IReadOnlyDictionary<string, string> mapping)
    {
        var mappedTargets = mapping.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var issues = new List<ImportValidationIssue>();

        foreach (var mandatory in MandatoryFields)
        {
            if (!mappedTargets.Contains(mandatory))
            {
                issues.Add(new ImportValidationIssue(null, "Error", "MANDATORY_COLUMN_MISSING", $"Missing required field mapping for {mandatory}."));
            }
        }

        var duplicates = sourceColumns
            .GroupBy(Normalize)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicate in duplicates)
        {
            issues.Add(new ImportValidationIssue(null, "Warning", "DUPLICATE_HEADER", $"Duplicate source header detected: {duplicate}."));
        }

        return issues;
    }

    private static string Normalize(string value) =>
        new(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
}
