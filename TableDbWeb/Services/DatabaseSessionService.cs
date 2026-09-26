using TableDbEngine.Models;
using TableDbEngine.Services;

namespace TableDbWeb.Services;

public class DatabaseSessionService
{
    public Database CurrentDatabase { get; private set; }
    public JsonDbStorage Storage { get; } = new();
    public PatternSearchService SearchService { get; } = new();

    public DatabaseSessionService()
    {
        CurrentDatabase = new Database("MyDatabase");
        
        // Ініціалізуємо демонстраційною таблицею
        var defaultTable = new Table("Sensors");
        defaultTable.Columns.Add(new Column("ID", DataType.Integer));
        defaultTable.Columns.Add(new Column("Impedance", DataType.ComplexInteger));
        defaultTable.Columns.Add(new Column("Voltage", DataType.ComplexReal));
        defaultTable.Columns.Add(new Column("Tag", DataType.String));

        defaultTable.AddRow(new Row(new[] { "1", "3+4i", "12.5+0.5i", "Alpha" }));
        defaultTable.AddRow(new Row(new[] { "2", "-3+4i", "10.0-1.2i", "Beta" }));
        defaultTable.AddRow(new Row(new[] { "3", "7-2i", "5.0+0.0i", "Gamma" }));
        defaultTable.AddRow(new Row(new[] { "4", "15+4i", "220.0+15.3i", "Delta" }));

        CurrentDatabase.Tables.Add(defaultTable);
    }

    public void NewDatabase(string name) => CurrentDatabase = new Database(name);
    public void LoadDatabase(string json) => CurrentDatabase = Storage.Deserialize(json);
    public string ExportJson() => Storage.Serialize(CurrentDatabase);
}