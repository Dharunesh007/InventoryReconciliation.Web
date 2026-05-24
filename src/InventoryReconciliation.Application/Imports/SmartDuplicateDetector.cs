namespace InventoryReconciliation.Application.Imports;

public sealed record DuplicateCandidate(
    int LeftRow,
    int RightRow,
    string Reason,
    int ConfidenceScore,
    IReadOnlyCollection<string> MatchingFields);

public sealed class SmartDuplicateDetector
{
    public IReadOnlyCollection<DuplicateCandidate> Detect(IReadOnlyCollection<IReadOnlyDictionary<string, object?>> rows)
    {
        var candidates = new List<DuplicateCandidate>();
        var indexedRows = rows.Select((row, index) => new IndexedRow(index + 2, row)).ToArray();

        AddExactMatches(candidates, indexedRows, "AssetTag", "Duplicate asset tag", 96);
        AddExactMatches(candidates, indexedRows, "SerialNumber", "Duplicate serial number", 92);

        foreach (var pair in PairRows(indexedRows))
        {
            var matched = new List<string>();
            if (Equivalent(pair.Left.Row, pair.Right.Row, "HostName")) matched.Add("HostName");
            if (Equivalent(pair.Left.Row, pair.Right.Row, "ModelNumber")) matched.Add("ModelNumber");
            if (Equivalent(pair.Left.Row, pair.Right.Row, "UserName")) matched.Add("UserName");
            if (Equivalent(pair.Left.Row, pair.Right.Row, "Department")) matched.Add("Department");

            if (matched.Count >= 3)
            {
                candidates.Add(new DuplicateCandidate(
                    pair.Left.RowNumber,
                    pair.Right.RowNumber,
                    "Likely duplicate based on host, model, custodian, and department similarity",
                    65 + (matched.Count * 6),
                    matched));
            }
        }

        return candidates
            .GroupBy(candidate => new { candidate.LeftRow, candidate.RightRow, candidate.Reason })
            .Select(group => group.OrderByDescending(candidate => candidate.ConfidenceScore).First())
            .OrderByDescending(candidate => candidate.ConfidenceScore)
            .Take(250)
            .ToArray();
    }

    private static void AddExactMatches(
        ICollection<DuplicateCandidate> candidates,
        IReadOnlyCollection<IndexedRow> indexedRows,
        string fieldName,
        string reason,
        int confidence)
    {
        var groups = indexedRows
            .Where(item => TryGetValue(item.Row, fieldName, out var value) && !string.IsNullOrWhiteSpace(value))
            .GroupBy(item => Normalize(GetValue(item.Row, fieldName)), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Key.Length > 0 && group.Count() > 1);

        foreach (var group in groups)
        {
            var rows = group.ToArray();
            for (var i = 0; i < rows.Length; i++)
            {
                for (var j = i + 1; j < rows.Length; j++)
                {
                    candidates.Add(new DuplicateCandidate(rows[i].RowNumber, rows[j].RowNumber, reason, confidence, [fieldName]));
                }
            }
        }
    }

    private static IEnumerable<(IndexedRow Left, IndexedRow Right)> PairRows(IReadOnlyList<IndexedRow> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            for (var j = i + 1; j < rows.Count; j++)
            {
                yield return (rows[i], rows[j]);
            }
        }
    }

    private static bool Equivalent(IReadOnlyDictionary<string, object?> left, IReadOnlyDictionary<string, object?> right, string fieldName) =>
        TryGetValue(left, fieldName, out var leftValue)
        && TryGetValue(right, fieldName, out var rightValue)
        && Normalize(leftValue) == Normalize(rightValue);

    private static bool TryGetValue(IReadOnlyDictionary<string, object?> row, string fieldName, out string value)
    {
        value = string.Empty;
        if (!row.TryGetValue(fieldName, out var raw) || raw is null)
        {
            return false;
        }

        value = raw.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? GetValue(IReadOnlyDictionary<string, object?> row, string fieldName) =>
        row.TryGetValue(fieldName, out var value) ? value?.ToString() : null;

    private static string Normalize(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private sealed record IndexedRow(int RowNumber, IReadOnlyDictionary<string, object?> Row);
}
