using System.Globalization;
using System.Text.RegularExpressions;

namespace TableDbEngine.Models;

public record ComplexReal(double RealPart, double ImaginaryPart)
{
    private static readonly Regex ComplexRegex = new(
        @"^(?:(?<real>[+-]?\d+(?:\.\d+)?))?(?:(?<imag>[+-]?\d*(?:\.\d+)?)i)?$",
        RegexOptions.Compiled);

    public static bool TryParse(string input, out ComplexReal result)
    {
        result = new ComplexReal(0.0, 0.0);
        if (string.IsNullOrWhiteSpace(input)) return false;

        string s = input.Trim().Replace(" ", "");
        var match = ComplexRegex.Match(s);
        if (!match.Success || match.Length != s.Length) return false;

        string realStr = match.Groups["real"].Value;
        string imagStr = match.Groups["imag"].Value;

        if (string.IsNullOrEmpty(realStr) && string.IsNullOrEmpty(imagStr)) return false;

        double real = 0.0;
        double imag = 0.0;

        if (!string.IsNullOrEmpty(realStr))
        {
            if (!double.TryParse(realStr, NumberStyles.Float, CultureInfo.InvariantCulture, out real)) return false;
        }

        if (!string.IsNullOrEmpty(imagStr))
        {
            if (imagStr == "+" || imagStr == "") imag = 1.0;
            else if (imagStr == "-") imag = -1.0;
            else if (!double.TryParse(imagStr, NumberStyles.Float, CultureInfo.InvariantCulture, out imag)) return false;
        }

        result = new ComplexReal(real, imag);
        return true;
    }

    public override string ToString()
    {
        string r = RealPart.ToString(CultureInfo.InvariantCulture);
        string im = ImaginaryPart.ToString(CultureInfo.InvariantCulture);
        if (ImaginaryPart >= 0)
            return $"{r}+{im}i";
        return $"{r}{im}i";
    }
}