using Microsoft.AspNetCore.JsonPatch.Operations;
using MockEsu.Application.Extensions.StringExtensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.JsonPatch;

internal class JsonPatchPath
{
    public readonly string OriginalPath;
    public readonly string Index;
    public readonly string AsSingleProperty;

    public JsonPatchPath(string path)
    {
        OriginalPath = path;

        string operationPathAsProperty = path.ToPropetyFormat();
        string index = operationPathAsProperty.Split('.')[0];
        if (int.TryParse(operationPathAsProperty.Split('.')[0], out int _) ||
            index == "-")
        {
            if (index.Length < operationPathAsProperty.Length)
                operationPathAsProperty = operationPathAsProperty[(index.Length + 1)..];
            else
                operationPathAsProperty = string.Empty;
        }
        else
        {
            index = string.Empty;
        }
        AsSingleProperty = operationPathAsProperty;
        Index = index;
    }

    public string ToFullPropertyPath(string newPropertyPath)
    {
        if (Index != string.Empty)
        {
            if (newPropertyPath.Length > 0)
                newPropertyPath = $"{Index}.{newPropertyPath}";
            else
                newPropertyPath = Index;
        }
        return newPropertyPath;
    }
}
