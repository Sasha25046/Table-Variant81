using System.Text.RegularExpressions;

namespace TableDbEngine.Models;

public record ComplexInteger(int RealPart, int ImaginaryPart)
{
    private static readonly Regex ComplexRegex = new(
        @"^(?:(?<real>[+-]?\d+))?(?:(?<imag>[+-]?\d*)i)?$",
        RegexOptions.Compiled);

    public static bool TryParse(string input, out ComplexInteger result)
    {
        result = new ComplexInteger(0, 0);
        if (string.IsNullOrWhiteSpace(input)) return false;

        string s = input.Trim().Replace(" ", "");
        var match = ComplexRegex.Match(s);
        if (!match.Success || match.Length != s.Length) return false;

        string realStr = match.Groups["real"].Value;
        string imagStr = match.Groups["imag"].Value;

        if (string.IsNullOrEmpty(realStr) && string.IsNullOrEmpty(imagStr)) return false;

        int real = 0;
        int imag = 0;

        if (!string.IsNullOrEmpty(realStr))
        {
            if (!int.TryParse(realStr, out real)) return false;
        }

        if (!string.IsNullOrEmpty(imagStr))
        {
            if (imagStr == "+" || imagStr == "") imag = 1;
            else if (imagStr == "-") imag = -1;
            else if (!int.TryParse(imagStr, out imag)) return false;
        }

        result = new ComplexInteger(real, imag);
        return true;
    }

    public override string ToString()
    {
        if (ImaginaryPart >= 0)
            return $"{RealPart}+{ImaginaryPart}i";
        return $"{RealPart}{ImaginaryPart}i";
    }
}