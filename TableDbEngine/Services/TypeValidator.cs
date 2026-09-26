using System.Globalization;
using TableDbEngine.Models;

namespace TableDbEngine.Services;

public static class TypeValidator
{
    public static bool Validate(string? value, DataType type)
    {
        if (value == null) return false;
        string trimmed = value.Trim();

        return type switch
        {
            DataType.Integer => int.TryParse(trimmed, out _),
            DataType.Real => double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            DataType.Char => trimmed.Length == 1,
            DataType.String => true,
            DataType.ComplexInteger => ComplexInteger.TryParse(trimmed, out _),
            DataType.ComplexReal => ComplexReal.TryParse(trimmed, out _),
            _ => false
        };
    }

    public static string GetDefaultValue(DataType type)
    {
        return type switch
        {
            DataType.Integer => "0",
            DataType.Real => "0.0",
            DataType.Char => "-",
            DataType.String => "",
            DataType.ComplexInteger => "0+0i",
            DataType.ComplexReal => "0.0+0.0i",
            _ => ""
        };
    }
}