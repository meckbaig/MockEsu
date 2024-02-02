using System.Text.Json;

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

    /// <summary>
    /// Converts a string to camel case
    /// </summary>
    /// <param name="value">input string</param>
    /// <returns>String in camel case</returns>
    public static string ToCamelCase(this string value)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(value);
    }
}
