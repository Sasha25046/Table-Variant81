using System.Text.RegularExpressions;
using TableDbEngine.Models;

namespace TableDbEngine.Services;

public class PatternSearchService
{
    public List<Row> Search(Table table, string? targetColumnName, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return table.Rows;

        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        if (string.IsNullOrWhiteSpace(targetColumnName) || targetColumnName == "(Всі колонки)")
        {
            return table.Rows
                .Where(row => row.Cells.Any(cell => regex.IsMatch(cell.RawValue)))
                .ToList();
        }

        int colIndex = table.Columns.FindIndex(c => c.Name.Equals(targetColumnName, StringComparison.OrdinalIgnoreCase));
        if (colIndex == -1)
            return new List<Row>();

        return table.Rows
            .Where(row => colIndex < row.Cells.Count && regex.IsMatch(row.Cells[colIndex].RawValue))
            .ToList();
    }
}