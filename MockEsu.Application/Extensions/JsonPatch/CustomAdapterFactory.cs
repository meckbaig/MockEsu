using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using MockEsu.Domain.Common;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.JsonPatch;

public class CustomAdapterFactory : IAdapterFactory
{
    internal static CustomAdapterFactory Default { get; } = new CustomAdapterFactory();

    public IAdapter Create(object target, IContractResolver contractResolver)
    {
        ArgumentNullException.ThrowIfNull(target, "target");
        ArgumentNullException.ThrowIfNull(contractResolver, "contractResolver");
        JsonContract jsonContract = contractResolver.ResolveContract(target.GetType());
        if (target is JObject)
        {
            return new JObjectAdapter();
        }

        if (target is IList)
        {
            return new CustomListAdapter();
        }

        if (target is DbSet<BaseEntity>)
        {
            return new CustomDbSetAdapter();
        }

        if (jsonContract is JsonDictionaryContract jsonDictionaryContract)
        {
            return (IAdapter)Activator
                .CreateInstance(typeof(DictionaryAdapter<,>)
                .MakeGenericType(
                    jsonDictionaryContract.DictionaryKeyType, 
                    jsonDictionaryContract.DictionaryValueType));
        }

        if (jsonContract is JsonDynamicContract)
        {
            return new DynamicObjectAdapter();
        }

        return new PocoAdapter();
    }
}

public class CustomDbSetAdapter : IAdapter
{
    public bool TryAdd(
        object target, 
        string segment, 
        IContractResolver contractResolver, 
        object value, 
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryGet(
        object target, 
        string segment, 
        IContractResolver 
        contractResolver, 
        out object value, 
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    
    public bool TryRemove(
        object target,
        string segment,
        IContractResolver contractResolver,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryReplace(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryTest(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryTraverse(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object nextTarget,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }
}

public class CustomListAdapter : ListAdapter
{
    private ResourceManager rm = new ResourceManager(typeof(CustomListAdapter));

    protected override bool TryGetPositionInfo(
        IList list, 
        string segment, 
        OperationType operationType, 
        out PositionInfo positionInfo, 
        out string errorMessage)
    {
        if (segment == "-")
        {
            positionInfo = new PositionInfo(PositionType.EndOfList, -1);
            errorMessage = null;
            return true;
        }

        if (int.TryParse(segment, out var entityId))
        {
            var entities = list.Cast<IEntityWithId>().ToList();
            if (entities == null)
            {
                positionInfo = new PositionInfo(PositionType.Invalid, -1);
                errorMessage = FormatInvalidListType();
                return false;
            }
            int entityPosition = entities.IndexOf(entities.FirstOrDefault(e => e.Id == entityId));
            if (entityPosition >= 0)
            {
                positionInfo = new PositionInfo(PositionType.Index, entityPosition);
                errorMessage = null;
                return true;
            }
            // As per JSON Patch spec, for Add operation the index value representing the number of elements is valid,
            // where as for other operations like Remove, Replace, Move and Copy the target index MUST exist.
            else if (operationType == OperationType.Add)
            {
                positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                errorMessage = null;
                return true;
            }
            else
            {
                positionInfo = new PositionInfo(PositionType.OutOfBounds, -1);
                errorMessage = FormatIndexOutOfBounds(segment);
                return false;
            }
        }
        else
        {
            positionInfo = new PositionInfo(PositionType.Invalid, -1);
            errorMessage = FormatInvalidIndexValue(segment);
            return false;
        }
    }

    public override bool TryTraverse(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage)
    {
        var entities = (target as IList).Cast<IEntityWithId>().ToList();
        if (entities == null)
        {
            value = null;
            errorMessage = null;
            return false;
        }

        if (!int.TryParse(segment, out var entityId))
        {
            value = null;
            errorMessage = FormatInvalidIndexValue(segment);
            return false;
        }

        int entityPosition = entities.IndexOf(entities.FirstOrDefault(e => e.Id == entityId));
        if (entityPosition < 0)
        {
            value = null;
            errorMessage = FormatIndexOutOfBounds(segment);
            return false;
        }

        value = entities[entityPosition];
        errorMessage = null;
        return true;
    }

    private string FormatIndexOutOfBounds(object p0)
            => $"The index value provided by path segment '{p0}' is out of bounds of the array size.";

    private string FormatInvalidIndexValue(object p0)
           => $"The path segment '{p0}' is invalid for an item Id.";

    private string FormatInvalidListType()
           => $"List items do not inherit interface {nameof(IEntityWithId)}";
}
