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
/// ById - list of single indexes or spans
/// </code>
/// </remarks>
internal enum CompareMethod
{
    Equals,
    ById
}