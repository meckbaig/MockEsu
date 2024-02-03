﻿using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.StringExtencions;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities.Traiffs;
using Newtonsoft.Json.Serialization;

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
        var dto = mapper.Map<TDto>(destination);
        patch.ApplyTo(dto, Adapter);
        mapper.Map(dto, destination);
        return destination;
    }

    internal static void ApplyTransactionToSource<IDbSet, TDestination>
        (this JsonPatchDocument<IDbSet> patch, IDbSet dbSet, IAppDbContext context)
        where IDbSet : DbSet<TDestination>
        where TDestination : class
    {
        // понять, как передавать сюда DTO

        // понять, как мапать DTO на модель без
        // экземпляра (получать эндпоинты через рефлексию)

        // плакать
        using (var transaction = context.Database.BeginTransaction())
        {
            try
            {
                patch.ApplyTo(dbSet, Adapter);
                //transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
            }
        }
    }

    //internal static IList<TDestination>? ApplyToSource<TDto, TDestination>
    //    (this JsonPatchDocument<IList<TDto>> patch, IList<TDestination>? destinations, IMapper mapper)
    //    where TDto : BaseDto
    //    where TDestination : BaseEntity
    //{
    //    var dtos = mapper.Map<IList<TDto>>(destinations);
    //    patch.ApplyTo(dtos, Adapter);
    //    mapper.Map(dtos, destinations);
    //    return destinations;
    //}

    internal static JsonPatchDocument<DbSet<TDestination>> ConvertToSourceDbSet
        <TDestination, TDto>(this JsonPatchDocument<TDto> patch, IMapper mapper) 
        where TDto : BaseDto
        where TDestination : BaseEntity
    {
        ///TODO:
        ///Ремапать, если внутри value содержится объект
        var newOperations = new List<Operation<DbSet<TDestination>>>();
        foreach (var operation in patch.Operations)
        {
            string operationPathAsProperty = operation.path.ToPropetyFormat();
            if (int.TryParse(operationPathAsProperty.Split('.')[0], out int index))
                operationPathAsProperty = operationPathAsProperty[(index.GetLength() + 1)..];
            else
                index = -1;

            var newOperation = new Operation<DbSet<TDestination>>()
            {
                from = operation.from,
                op = operation.op,
            };
            newOperation.path = 
                BaseDto.GetSourceJsonPatch<TDto>(
                    operationPathAsProperty, 
                    mapper.ConfigurationProvider,
                    out Type propertyType)
                .ToPathFormat();
            newOperation.value =
                BaseDto.GetSourceValueJsonPatch(
                    operation.value,
                    propertyType,
                    mapper.ConfigurationProvider);

            if (index != -1)
                newOperation.path = $"/{index}{newOperation.path}";
            newOperations.Add(newOperation);
        }
        return new JsonPatchDocument<DbSet<TDestination>>(
            newOperations, 
            new CamelCasePropertyNamesContractResolver());
    }

    private static string ToPathFormat(this string property)
    {
        return string.Format("/{0}",
            string.Join(
                '/', 
                property
                .Split('.')
                .Select(x => x.ToCamelCase())));
    }

    private static string ToPropetyFormat(this string path)
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