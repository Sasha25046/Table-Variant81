using System.Text.Json;
using TableDbEngine.Models;

namespace TableDbEngine.Services;

public class JsonDbStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(Database db) => JsonSerializer.Serialize(db, Options);

    public Database Deserialize(string json)
    {
        return JsonSerializer.Deserialize<Database>(json, Options) ?? new Database("ImportedDb");
    }
}