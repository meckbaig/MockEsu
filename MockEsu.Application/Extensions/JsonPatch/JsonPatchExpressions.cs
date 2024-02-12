using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.StringExtencions;
using MockEsu.Domain.Common;
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

    /// <summary>
    /// Deprecated.
    /// </summary>
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

    /// <summary>
    /// Applies json patch to the database.
    /// </summary>
    /// <typeparam name="TDbSet">Type of DbSet to apply json patch to.</typeparam>
    /// <typeparam name="TDestination">Type of entity.</typeparam>
    /// <param name="patch">Json patch document containing operations</param>
    /// <param name="dbSet">DbSet to apply json patch to.</param>
    internal static void ApplyTransactionToSource<TDestination>
        (this JsonPatchDocument<DbSet<TDestination>> patch, DbSet<TDestination> dbSet)
        where TDestination : BaseEntity
    {
        IAppDbContext context = dbSet.GetService<IAppDbContext>();
        using (var transaction = context.Database.BeginTransaction())
        {
            try
            {
                patch.ApplyTo(dbSet, Adapter);
                transaction.Commit();
            }
            catch (JsonPatchException ex)
            {
                transaction.Rollback();
                if (ex.FailedOperation != null)
                    throw new JsonPatchException($"{(ex.FailedOperation as IDbSetOperation).dtoPath}: {ex.Message}", ex);
                throw;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <summary>
    /// Converts json patch document from <typeparamref name="TDto"/> to DbSet of <typeparamref name="TDestination"/>.
    /// </summary>
    /// <typeparam name="TDestination">Entity type.</typeparam>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="patch">Json patch document containing operations.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <returns>Json patch document of DbSet of <typeparamref name="TDestination"/></returns>
    internal static JsonPatchDocument<DbSet<TDestination>> ConvertToSourceDbSet
        <TDto, TDestination>(this JsonPatchDocument<TDto> patch, IConfigurationProvider provider)
        where TDestination : BaseEntity
        where TDto : BaseDto, IEditDto
    {
        var newOperations = new List<Operation<DbSet<TDestination>>>();
        foreach (var operation in patch.Operations)
        {
            var jsonPatchPath = new JsonPatchPath(operation.path);

            var newOperation = new DbSetOperation<TDestination>()
            {
                dtoPath = operation.path,
                from = operation.from,
                op = operation.op,
            };

            try
            {
                newOperation.path =
                    BaseDto.GetSourceJsonPatch<TDto>(
                        jsonPatchPath.AsSingleProperty,
                        provider,
                        out Type propertyType);
                newOperation.path = jsonPatchPath.ToFullPropertyPath(newOperation.path);

                newOperation.value =
                    BaseDto.GetSourceValueJsonPatch(
                        operation.value,
                        propertyType,
                        provider);
            }
            catch (Exception ex)
            {
                throw new JsonPatchException($"{newOperation.dtoPath}: {ex.Message}", ex);
            }

            newOperations.Add(newOperation);
        }
        return new JsonPatchDocument<DbSet<TDestination>>(
            newOperations,
            new CamelCasePropertyNamesContractResolver());
    }

    /// <summary>
    /// Converts operations from <typeparamref name="TDto"/> to DbSet of <typeparamref name="TDestination"/> and applies them to database.
    /// </summary>
    /// <typeparam name="TDestination">Type of database entity.</typeparam>
    /// <typeparam name="TDto">Type of DTO.</typeparam>
    /// <param name="patch">Json patch document containing operations.</param>
    /// <param name="dbSet">DbSet to apply json patch to.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    internal static void ApplyDtoTransactionToSource
        <TDestination, TDto>(
        this JsonPatchDocument<TDto> patch,
        DbSet<TDestination> dbSet,
        IConfigurationProvider provider)
        where TDestination : BaseEntity
        where TDto : BaseDto, IEditDto
    {
        var convertedPatch = patch.ConvertToSourceDbSet<TDto, TDestination>(provider);
        convertedPatch.ApplyTransactionToSource(dbSet);
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
