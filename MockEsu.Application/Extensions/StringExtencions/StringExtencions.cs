namespace MockEsu.Application.Extensions.StringExtencions;

internal static class StringExtencions
{
    /// <summary>
    /// Converts a string to pascal case
    /// </summary>
    /// <param name="value">input string</param>
    /// <returns>String in pascal case</returns>
    public static string ToPascalCase(this string value)
    {
        if (value.Length <= 1)
            return value.ToUpper();
        return $"{value[0].ToString().ToUpper()}{value.Substring(1)}";
    }
}
