namespace InventoryReconciliation.Application.Imports;

public sealed record ExcelColumnProfile(
    string SourceColumn,
    string? SuggestedTargetField,
    int NonBlankCount,
    int UniqueCount,
    int ErrorCount,
    IReadOnlyCollection<string> Samples);

public sealed record ImportValidationIssue(
    int? RowNumber,
    string Severity,
    string Code,
    string Message,
    string? SourceColumn = null);

public sealed record ImportPreviewResult(
    int TotalRows,
    int TotalColumns,
    IReadOnlyCollection<ExcelColumnProfile> Columns,
    IReadOnlyCollection<ImportValidationIssue> Issues,
    IReadOnlyCollection<IReadOnlyDictionary<string, object?>> PreviewRows,
    IReadOnlyDictionary<string, string> SuggestedMapping);

public sealed record ColumnMappingRequest(
    IReadOnlyDictionary<string, string> SourceToTargetFields,
    bool TreatFormulaErrorsAsBlank,
    bool CreateSnapshot,
    string? SnapshotName);

public sealed record ImportExecutionResult(
    Guid ImportBatchId,
    Guid? SnapshotId,
    int Inserted,
    int Updated,
    int Rejected,
    int Duplicates,
    IReadOnlyCollection<ImportValidationIssue> Issues);
