using Microsoft.AspNetCore.Mvc;
using TableDbEngine.Models;
using TableDbEngine.Services;
using TableDbWeb.Services;

namespace TableDbWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly DatabaseSessionService _db;

    public DatabaseController(DatabaseSessionService db) => _db = db;

    [HttpGet]
    public IActionResult GetDatabase() => Ok(_db.CurrentDatabase);

    [HttpPost("new")]
    public IActionResult NewDatabase([FromBody] string name)
    {
        _db.NewDatabase(string.IsNullOrWhiteSpace(name) ? "NewDatabase" : name);
        return Ok(_db.CurrentDatabase);
    }

    [HttpPost("export")]
    public IActionResult Export() => Content(_db.ExportJson(), "application/json");

    [HttpPost("import")]
    public IActionResult Import([FromBody] string json)
    {
        try
        {
            _db.LoadDatabase(json);
            return Ok(_db.CurrentDatabase);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Помилка формату JSON: " + ex.Message });
        }
    }

    [HttpPost("tables")]
    public IActionResult CreateTable([FromBody] Table table)
    {
        if (string.IsNullOrWhiteSpace(table.Name))
            return BadRequest(new { error = "Назва таблиці не може бути порожньою!" });

        if (_db.CurrentDatabase.Tables.Any(t => t.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { error = $"Таблиця з назвою '{table.Name}' уже існує в базі даних!" });

        if (table.Columns == null || table.Columns.Count == 0)
            return BadRequest(new { error = "Таблиця повинна містити хоча б одну колонку!" });

        if (table.Columns.Any(c => string.IsNullOrWhiteSpace(c.Name)))
            return BadRequest(new { error = "Назва колонки не може бути порожньою!" });

        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in table.Columns)
        {
            if (!columnNames.Add(col.Name.Trim()))
                return BadRequest(new { error = $"Колонка з назвою '{col.Name}' повторюється!" });
        }

        _db.CurrentDatabase.Tables.Add(table);
        return Ok(table);
    }

    [HttpDelete("tables/{tableName}")]
    public IActionResult DeleteTable(string tableName)
    {
        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });

        _db.CurrentDatabase.Tables.Remove(table);
        return Ok();
    }

    [HttpPost("tables/{tableName}/columns")]
    public IActionResult AddColumn(string tableName, [FromBody] Column col)
    {
        if (string.IsNullOrWhiteSpace(col.Name))
            return BadRequest(new { error = "Назва колонки не може бути порожньою!" });

        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });

        if (table.Columns.Any(c => c.Name.Equals(col.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { error = $"Колонка '{col.Name}' вже існує в таблиці!" });

        col.Name = col.Name.Trim();
        table.Columns.Add(col);

        string defVal = TypeValidator.GetDefaultValue(col.Type);
        foreach (var row in table.Rows)
        {
            row.Cells.Add(new Cell(defVal));
        }

        return Ok(table);
    }

    [HttpPut("tables/{tableName}/columns/{oldColName}")]
    public IActionResult EditColumn(string tableName, string oldColName, [FromBody] Column newCol)
    {
        if (string.IsNullOrWhiteSpace(newCol.Name))
            return BadRequest(new { error = "Нова назва колонки не може бути порожньою!" });

        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });

        int idx = table.Columns.FindIndex(c => c.Name.Equals(oldColName, StringComparison.OrdinalIgnoreCase));
        if (idx == -1) return NotFound(new { error = "Колонку не знайдено." });

        if (!oldColName.Equals(newCol.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
            table.Columns.Any(c => c.Name.Equals(newCol.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = $"Колонка з назвою '{newCol.Name}' уже існує!" });
        }

        for (int r = 0; r < table.Rows.Count; r++)
        {
            var cellVal = table.Rows[r].Cells[idx].RawValue;
            if (!TypeValidator.Validate(cellVal, newCol.Type))
            {
                return BadRequest(new { 
                    error = $"Неможливо змінити тип на {newCol.Type}! Значення '{cellVal}' у рядку #{r + 1} не відповідає новому типу." 
                });
            }
        }

        newCol.Name = newCol.Name.Trim();
        table.Columns[idx] = newCol;
        return Ok(table);
    }

    [HttpDelete("tables/{tableName}/columns/{colName}")]
    public IActionResult DeleteColumn(string tableName, string colName)
    {
        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });

        if (table.Columns.Count <= 1)
            return BadRequest(new { error = "Неможливо видалити єдину колонку! Таблиця повинна містити щонайменше одну колонку." });

        int idx = table.Columns.FindIndex(c => c.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));
        if (idx == -1) return NotFound(new { error = "Колонку не знайдено." });

        table.Columns.RemoveAt(idx);
        foreach (var r in table.Rows)
        {
            if (idx < r.Cells.Count) r.Cells.RemoveAt(idx);
        }

        return Ok(table);
    }

    [HttpPost("tables/{tableName}/rows")]
    public IActionResult AddRow(string tableName, [FromBody] List<string> values)
    {
        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });

        var processedValues = new List<string>();
        for (int i = 0; i < table.Columns.Count; i++)
        {
            var col = table.Columns[i];
            string val = i < values.Count && !string.IsNullOrWhiteSpace(values[i]) 
                ? values[i].Trim() 
                : TypeValidator.GetDefaultValue(col.Type);

            if (!TypeValidator.Validate(val, col.Type))
            {
                return BadRequest(new { error = $"Некоректне значення '{val}' для колонки '{col.Name}' типу {col.Type}." });
            }
            processedValues.Add(val);
        }

        table.AddRow(new Row(processedValues));
        return Ok(table);
    }

    [HttpPut("tables/{tableName}/rows/{rowIndex:int}")]
    public IActionResult EditRow(string tableName, int rowIndex, [FromBody] List<string> values)
    {
        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });
        if (rowIndex < 0 || rowIndex >= table.Rows.Count) return BadRequest(new { error = "Невірний індекс рядка." });

        var processedValues = new List<string>();
        for (int i = 0; i < table.Columns.Count; i++)
        {
            var col = table.Columns[i];
            string val = i < values.Count && !string.IsNullOrWhiteSpace(values[i]) 
                ? values[i].Trim() 
                : TypeValidator.GetDefaultValue(col.Type);

            if (!TypeValidator.Validate(val, col.Type))
            {
                return BadRequest(new { error = $"Некоректне значення '{val}' для колонки '{col.Name}' типу {col.Type}." });
            }
            processedValues.Add(val);
        }

        table.Rows[rowIndex] = new Row(processedValues);
        return Ok(table);
    }

    [HttpDelete("tables/{tableName}/rows/{rowIndex:int}")]
    public IActionResult DeleteRow(string tableName, int rowIndex)
    {
        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });
        if (rowIndex < 0 || rowIndex >= table.Rows.Count) return BadRequest(new { error = "Невірний індекс рядка." });

        table.Rows.RemoveAt(rowIndex);
        return Ok(table);
    }

    [HttpGet("tables/{tableName}/search")]
    public IActionResult Search(string tableName, [FromQuery] string? column, [FromQuery] string pattern)
    {
        var table = _db.CurrentDatabase.Tables.FirstOrDefault(t => t.Name == tableName);
        if (table == null) return NotFound(new { error = "Таблицю не знайдено." });

        var results = _db.SearchService.Search(table, column, pattern);
        return Ok(results);
    }
}