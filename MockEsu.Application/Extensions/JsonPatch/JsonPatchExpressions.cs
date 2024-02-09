using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.StringExtencions;
using MockEsu.Domain.Common;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Linq.Expressions;

namespace MockEsu.Application.Extensions.JsonPatch;

internal static class JsonPatchExpressions
{
    private static IObjectAdapter? _adapter;

    public static IObjectAdapter Adapter
    {
        get
        {
            if (_adapter == null)
            {
                IAdapterFactory factory = CustomAdapterFactory.Default;
                _adapter = new ObjectAdapter(new DefaultContractResolver(), null, factory);
            }
            return _adapter;
        }
    }

    internal static TDestination? ApplyToSource<TDto, TDestination>
        (this JsonPatchDocument<TDto> patch, TDestination? destination, IMapper mapper)
        where TDto : BaseDto
        where TDestination : BaseEntity
    {
        var tmpDestination = destination;
        var dto = mapper.Map<TDto>(destination);
        patch.ApplyTo(dto, Adapter);
        mapper.Map(dto, destination);
        return destination;
    }

    internal static void ApplyTransactionToSource<IDbSet, TDestination>
        (this JsonPatchDocument<IDbSet> patch, IDbSet dbSet, IAppDbContext context)
        where IDbSet : DbSet<TDestination>
        where TDestination : BaseEntity
    {
        using (var transaction = context.Database.BeginTransaction())
        {
            try
            {
                //Task[] tasks = new Task[patch.Operations.Count];
                //for (int i = 0; i < patch.Operations.Count; i++)
                //{
                //    tasks[i] = Task.Run(() 
                //        => patch.Operations[i].Apply(dbSet, Adapter));
                //}
                //Task.WaitAll(tasks);
                patch.ApplyTo(dbSet, Adapter);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    internal static JsonPatchDocument<DbSet<TDestination>> ConvertToSourceDbSet
        <TDestination, TDto>(this JsonPatchDocument<TDto> patch, IMapper mapper) 
        where TDto : BaseDto, IEditDto
        where TDestination : BaseEntity
    {
        var newOperations = new List<Operation<DbSet<TDestination>>>();
        foreach (var operation in patch.Operations)
        {
            string operationPathAsProperty = operation.path.ToPropetyFormat();
            string indexString = operationPathAsProperty.Split('.')[0];
            if (int.TryParse(operationPathAsProperty.Split('.')[0], out int _) || 
                indexString == "-")
            {
                if (indexString.Length < operationPathAsProperty.Length)
                    operationPathAsProperty = operationPathAsProperty[(indexString.Length + 1)..];
                else
                    operationPathAsProperty = string.Empty;
            }
            else
            {
                indexString = string.Empty;
            }

            var newOperation = new Operation<DbSet<TDestination>>()
            {
                from = operation.from,
                op = operation.op,
            };
            newOperation.path = 
                BaseDto.GetSourceJsonPatch<TDto>(
                    operationPathAsProperty, 
                    mapper.ConfigurationProvider,
                    out Type propertyType);
            newOperation.value =
                BaseDto.GetSourceValueJsonPatch(
                    operation.value,
                    propertyType,
                    mapper.ConfigurationProvider);

            if (indexString != string.Empty)
            {
                if (newOperation.path.Length > 0)
                    newOperation.path = $"{indexString}.{newOperation.path}";
                else
                    newOperation.path = indexString;
            }
            newOperations.Add(newOperation);
        }
        return new JsonPatchDocument<DbSet<TDestination>>(
            newOperations, 
            new CamelCasePropertyNamesContractResolver());
    }

    internal static string ToPathFormat(this string property)
    {
        return string.Format("/{0}",
            string.Join(
                '/', 
                property
                .Split('.')
                .Select(x => x.ToCamelCase())));
    }

    internal static string ToPropetyFormat(this string path)
    {
        return string.Join(
            '.', 
            path
            .Replace("/", " ")
            .Trim()
            .Split(' ')
            .Select(x => x.ToPascalCase()));
    }

    private static int GetLength(this int value)
    {
        int len = value >= 0 ? 1 : 2;
        while (value > 10 || value < -10)
        {
            value /= 10;
            len++;
        }
        return len;
    } 
}
