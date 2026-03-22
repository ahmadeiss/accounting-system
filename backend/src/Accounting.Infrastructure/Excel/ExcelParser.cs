using OfficeOpenXml;
using System.Text.Json;

namespace Accounting.Infrastructure.Excel;

/// <summary>
/// Thin wrapper around EPPlus that reads an Excel worksheet and returns each
/// data row as a dictionary keyed by the header name (row 1).
///
/// Design decisions:
/// - Row 1 is always the header row; data starts at row 2.
/// - Empty rows (all cells blank) are skipped.
/// - All values are returned as strings; processors are responsible for parsing.
/// - The parser is stateless and has no knowledge of domain rules.
/// </summary>
public static class ExcelParser
{
    static ExcelParser()
    {
        // EPPlus 5+ requires a license context for non-commercial use.
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Parses the first worksheet of the given Excel bytes.
    /// Returns one <see cref="ExcelRow"/> per non-empty data row.
    /// </summary>
    public static IReadOnlyList<ExcelRow> Parse(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);
        using var package = new ExcelPackage(stream);

        var sheet = package.Workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The Excel file contains no worksheets.");

        if (sheet.Dimension is null)
            return Array.Empty<ExcelRow>();

        var headers = ReadHeaders(sheet);
        if (headers.Count == 0)
            return Array.Empty<ExcelRow>();

        var rows = new List<ExcelRow>();
        int lastRow = sheet.Dimension.End.Row;

        for (int r = 2; r <= lastRow; r++)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool hasAnyValue = false;

            for (int c = 1; c <= headers.Count; c++)
            {
                string header = headers[c - 1];
                string cellValue = sheet.Cells[r, c].Text?.Trim() ?? string.Empty;
                values[header] = cellValue;
                if (!string.IsNullOrEmpty(cellValue))
                    hasAnyValue = true;
            }

            if (!hasAnyValue) continue; // skip blank rows

            rows.Add(new ExcelRow(r, values));
        }

        return rows;
    }

    private static List<string> ReadHeaders(ExcelWorksheet sheet)
    {
        var headers = new List<string>();
        int lastCol = sheet.Dimension.End.Column;

        for (int c = 1; c <= lastCol; c++)
        {
            string header = sheet.Cells[1, c].Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(header)) break; // stop at first blank header
            headers.Add(header);
        }

        return headers;
    }
}

/// <summary>
/// Represents a single data row parsed from an Excel file.
/// </summary>
public sealed class ExcelRow
{
    public int RowNumber { get; }
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>JSON snapshot of the row values — stored in ImportJobRow.RawData.</summary>
    public string RawJson { get; }

    public ExcelRow(int rowNumber, Dictionary<string, string> values)
    {
        RowNumber = rowNumber;
        Values = values;
        RawJson = JsonSerializer.Serialize(values);
    }

    public string Get(string key) =>
        Values.TryGetValue(key, out var v) ? v : string.Empty;

    public bool TryGetDecimal(string key, out decimal result) =>
        decimal.TryParse(Get(key), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out result);

    public bool TryGetBool(string key, out bool result)
    {
        var raw = Get(key).ToLowerInvariant();
        result = raw is "true" or "yes" or "1";
        return raw is "true" or "yes" or "1" or "false" or "no" or "0";
    }

    public bool TryGetDateOnly(string key, out DateOnly result)
    {
        if (DateTime.TryParse(Get(key), out var dt))
        {
            result = DateOnly.FromDateTime(dt);
            return true;
        }
        result = default;
        return false;
    }
}

