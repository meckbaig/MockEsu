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

internal enum CompareMethod
{
    Equals,
    Between,
    CellContainsValue,
    ValueContainsCell,
    Date,
    DateTime,
    ById
}