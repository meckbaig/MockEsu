using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.CompilerServices;

namespace MockEsu.Application.Common.Attributes;

internal class FilterableAttribute : Attribute
{
    public CompareMethod CompareMethod { get; set; }
    public string Path { get; set; }

    public FilterableAttribute(CompareMethod compareMethod, [CallerMemberName] string path = "")
    {
        CompareMethod = compareMethod;
        Path = path;
    }
}

/// <summary>
/// 
/// </summary>
/// <remarks>
/// <code>
/// Equals - 100% equality
/// CellContainsValue - single 'cell contains value' statement
/// ValueContainsCell - single 'value contains cell' statement
/// Between - single 'between' statement
/// Date - list of single dates or spans
/// DateTime - list of single date-times or spans
/// ById - list of single indexes or spans
/// </code>
/// </remarks>
internal enum CompareMethod
{
    Equals,
    CellContainsValue,
    ValueContainsCell,
    Between, 
    Date,
    DateTime,
    ById
}