using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.CompilerServices;

namespace MockEsu.Application.Common.Attributes;

public class FilterableAttribute : Attribute
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
/// ById - entity by id (foreign key)
/// Nested - collection of entities
/// </code>
/// </remarks>
public enum CompareMethod
{
    Equals,
    ById,
    Nested
}