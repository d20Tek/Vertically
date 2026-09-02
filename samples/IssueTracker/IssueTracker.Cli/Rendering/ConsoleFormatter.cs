using System.Text;

namespace IssueTracker.Cli.Rendering;

/// <summary>
/// Minimal plain-text rendering helpers for CLI output — a tiny column-formatting utility (compute max
/// widths, pad columns) for list verbs and a key/value detail block for show verbs. Intentionally
/// dependency-free (no table library) to keep the sample focused on Vertically integration.
/// </summary>
internal static class ConsoleFormatter
{
    private const string ColumnSeparator = "  ";

    /// <summary>
    /// Renders <paramref name="rows"/> as a padded, left-aligned text table under <paramref name="headers"/>.
    /// Column widths are sized to the widest cell (header or value) in each column. When there are no rows,
    /// <paramref name="emptyMessage"/> is returned instead.
    /// </summary>
    public static string Table(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        string emptyMessage = "No results.")
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0) return emptyMessage;

        var widths = new int[headers.Count];
        for (var col = 0; col < headers.Count; col++)
        {
            widths[col] = headers[col].Length;
        }

        foreach (var row in rows)
        {
            for (var col = 0; col < headers.Count; col++)
            {
                var value = col < row.Count ? row[col] ?? string.Empty : string.Empty;
                widths[col] = Math.Max(widths[col], value.Length);
            }
        }

        var builder = new StringBuilder();
        AppendRow(builder, headers, widths);
        AppendRow(builder, headers.Select((h, col) => new string('-', widths[col])).ToArray(), widths);

        foreach (var row in rows)
        {
            AppendRow(builder, row, widths);
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Renders <paramref name="fields"/> as an aligned <c>Label : Value</c> detail block, padding labels to
    /// the widest label so the values line up.
    /// </summary>
    public static string Detail(IReadOnlyList<(string Label, string Value)> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Count == 0) return string.Empty;

        var labelWidth = fields.Max(f => f.Label.Length);
        var builder = new StringBuilder();
        foreach (var (label, value) in fields)
        {
            builder.AppendLine($"{label.PadRight(labelWidth)} : {value}");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> cells, int[] widths)
    {
        for (var col = 0; col < widths.Length; col++)
        {
            if (col > 0) builder.Append(ColumnSeparator);

            var value = col < cells.Count ? cells[col] ?? string.Empty : string.Empty;
            builder.Append(value.PadRight(widths[col]));
        }

        builder.AppendLine();
    }
}
