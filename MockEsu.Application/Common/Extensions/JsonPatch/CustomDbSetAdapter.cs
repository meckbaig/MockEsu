using AutoMapper.Internal;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Domain.Common;
using MockEsu.Domain.Enums;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace MockEsu.Application.Extensions.JsonPatch;

public class CustomDbSetAdapter<TEntity> : IAdapter where TEntity : BaseEntity
{
    public bool TryAdd(
        object target,
        string segmentsString,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        string[] segments = segmentsString.Split('.');
        DbSet<TEntity> dbSet = (DbSet<TEntity>)target;

        int parentId = 0;
        int? parentIndex = null;
        for (int i = segments.Length - 2; i >= 0; i--)
        {
            if (parentId == 0)
            {
                if (int.TryParse(segments[i], out parentId))
                    parentIndex = i;
            }
            else break;
        }

        List<Type> segmentTypes = [typeof(TEntity)];
        for (int i = 1; i < segments.Length; i++)
        {
            if ((int.TryParse(segments[i], out int _) || segments[i] == "-")
                && segmentTypes.Last().IsCollection())
            {
                segmentTypes.Add(segmentTypes.Last().GetGenericArguments().Single());
            }
            else
            {
                segmentTypes.Add(segmentTypes.Last().GetProperty(segments[i]).PropertyType);
            }
        }

        Type entityType = segmentTypes.Last();
        if (!TryConvertValue(value, entityType!, out var convertedValue, out errorMessage))
        {
            return false;
        }

        IAppDbContext context = dbSet.GetService<IAppDbContext>();
        if (parentId == 0)
        {
            return InvokeTryAddEntityToDb(
                entityType,
                convertedValue,
                context,
                out errorMessage);
        }
        else
        {
            int entityNameIndex = segments.Length - 2;
            string entityName = segments[entityNameIndex];
            Type parentType = segmentTypes[(int)parentIndex];
            return InvokeTryAddEntityToParent(
                parentType,
                entityType,
                parentId,
                entityName,
                convertedValue,
                context,
                out errorMessage);
        }
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
        string segmentsString,
        IContractResolver contractResolver,
        out string errorMessage)
    {
        string[] segments = segmentsString.Split('.');
        DbSet<TEntity> dbSet = (DbSet<TEntity>)target;

        int entityId = 0;
        int parentId = 0;
        int? entityIndex = null;
        int? parentIndex = null;
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            if (entityId == 0)
            {
                if (int.TryParse(segments[i], out entityId))
                    entityIndex = i;
            }
            else if (parentId == 0)
            {
                if (int.TryParse(segments[i], out parentId))
                    parentIndex = i;
            }
            else break;
        }

        if (entityId == 0)
        {
            errorMessage = "Could not recognize entity id";
            return false;
        }

        List<Type> segmentTypes = [typeof(TEntity)];
        for (int i = 1; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out int _) && segmentTypes.Last().IsCollection())
            {
                segmentTypes.Add(segmentTypes.Last().GetGenericArguments().Single());
            }
            else
            {
                segmentTypes.Add(segmentTypes.Last().GetProperty(segments[i]).PropertyType);
            }
        }

        IAppDbContext context = dbSet.GetService<IAppDbContext>();
        if (parentId == 0)
        {
            Type entityType = segmentTypes[(int)entityIndex];
            return InvokeTryRemoveEntityFromDb(
                entityType,
                entityId,
                context,
                out errorMessage);
        }
        else
        {
            int entityNameIndex = (int)entityIndex - 1;
            string entityName = segments[entityNameIndex];
            Type parentType = segmentTypes[(int)parentIndex];
            Type entityType = segmentTypes[(int)entityIndex];
            return InvokeTryRemoveEntityFromParent(
                parentType,
                entityType,
                parentId,
                entityId,
                entityName,
                context,
                out errorMessage);
        }
    }

    public bool TryReplace(
        object target,
        string segmentsString,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        string[] segments = segmentsString.Split('.');
        DbSet<TEntity> dbSet = (DbSet<TEntity>)target;

        return TryReplaceWithNewQuery(target, dbSet, segments, contractResolver, value, out errorMessage);
    }

    #region ReflectionCalls

    private bool InvokeTryAddEntityToDb(
        Type entityType,
        object convertedValue,
        IAppDbContext context,
        out string errorMessage)
    {
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m =>
            m.Name == nameof(TryAddEntityToDb) &&
            m.GetParameters().Length == 3);
        var genericMethod = methodInfo.MakeGenericMethod(entityType);
        object[] parameters = [convertedValue, context, null];
        object result = genericMethod.Invoke(this, parameters);

        errorMessage = (string)parameters.Last();
        return (bool)result;
    }

    private bool InvokeTryAddEntityToParent(
        Type parentType,
        Type entityType,
        int parentId,
        string entitiesInParentFieldName,
        object convertedValue,
        IAppDbContext context,
        out string errorMessage)
    {
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m =>
            m.Name == nameof(TryAddEntityToParent) &&
            m.GetParameters().Length == 5);
        var genericMethod = methodInfo.MakeGenericMethod(parentType, entityType);
        object[] parameters = [parentId, entitiesInParentFieldName, convertedValue, context, null];
        object result = genericMethod.Invoke(this, parameters);

        errorMessage = (string)parameters.Last();
        return (bool)result;
    }

    private bool InvokeTryRemoveEntityFromDb(
        Type entityType,
        int entityId,
        IAppDbContext context,
        out string errorMessage)
    {
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m =>
            m.Name == nameof(TryRemoveEntityFromDb) &&
            m.GetParameters().Length == 3);
        var genericMethod = methodInfo.MakeGenericMethod(entityType);
        object[] parameters = [entityId, context, null];
        object result = genericMethod.Invoke(this, parameters);

        errorMessage = (string)parameters.Last();
        return (bool)result;
    }

    private bool InvokeTryRemoveEntityFromParent(
        Type parentType,
        Type entityType,
        int parentId,
        int entityId,
        string entitiesInParentFieldName,
        IAppDbContext context,
        out string errorMessage)
    {
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m =>
            m.Name == nameof(TryRemoveEntityFromParent) &&
            m.GetParameters().Length == 5);
        var genericMethod = methodInfo.MakeGenericMethod(parentType, entityType);
        object[] parameters = [parentId, entityId, entitiesInParentFieldName, context, null];
        object result = genericMethod.Invoke(this, parameters);

        errorMessage = (string)parameters.Last();
        return (bool)result;
    }

    private bool InvokeTryReplaceWithNewQuery(
        object dbSet,
        IQueryable query,
        Type genericType,
        string[] segments,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m =>
            m.Name == nameof(TryReplaceWithNewQuery) &&
            m.GetParameters().Length == 6);
        var genericMethod = methodInfo.MakeGenericMethod(genericType);
        object[] parameters = [dbSet, query, segments, contractResolver, value, null];
        object result = genericMethod.Invoke(this, parameters);

        errorMessage = (string)parameters.LastOrDefault();
        return (bool)result;
    }

    #endregion

    #region PrivateMethods

    /// <summary>
    /// Adds entity and it's children into database.
    /// </summary>
    /// <typeparam name="TEntityToAdd">Type of entity to add.</typeparam>
    /// <param name="value">Entity value.</param>
    /// <param name="context">DbContext for performing actions.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <typeparamref name="TEntityToAdd"/> was successfully added; otherwise, <see langword="false"/>.</returns>
    private static bool TryAddEntityToDb
        <TEntityToAdd>(
        object value,
        IAppDbContext context,
        out string errorMessage)
        where TEntityToAdd : BaseEntity, new()
    {
        try
        {
            TEntityToAdd entity = (TEntityToAdd)value;
            (context as DbContext).ChangeTracker.Clear();
            AddEntityAndItsChildrenToContext(entity, context);
            context.SaveChanges();
            errorMessage = null;
            return true;
        }
        catch (Exception)
        {
            errorMessage = string.Format(
                "Could not add entity {0}",
                typeof(TEntityToAdd).Name.ToCamelCase());
            return false;
        }
    }

    /// <summary>
    /// Adds entity and it's children into parent entity.
    /// </summary>
    /// <typeparam name="TParent">Type of parent to which entity will be added.</typeparam>
    /// <typeparam name="TEntityToAdd">Type of entity to add.</typeparam>
    /// <param name="parentId">Id of parent entity.</param>
    /// <param name="entitiesInParentPropertyName">Name of property, in which entity will be added.</param>
    /// <param name="value">Entity value.</param>
    /// <param name="context">DbContext for performing actions.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <typeparamref name="TEntityToAdd"/> was successfully added; otherwise, <see langword="false"/>.</returns>
    private static bool TryAddEntityToParent
        <TParent, TEntityToAdd>(
        int parentId,
        string entitiesInParentPropertyName,
        object value,
        IAppDbContext context,
        out string errorMessage)
        where TParent : BaseEntity, new()
        where TEntityToAdd : BaseEntity, new()
    {
        try
        {
            TEntityToAdd entity = (TEntityToAdd)value;
            TParent parent = new TParent { Id = parentId };
            var listProperty = typeof(TParent).GetProperty(entitiesInParentPropertyName);
            ICollection<TEntityToAdd> list = (ICollection<TEntityToAdd>)listProperty.GetValue(parent);
            (context as DbContext).ChangeTracker.Clear();
            context.Entry(parent).State = EntityState.Unchanged;
            context.Entry(entity).State = EntityState.Unchanged;
            if (GetRelation(listProperty) != Relation.ManyToMany)
            {
                AddEntityAndItsChildrenToContext(entity, context);
            }
            list.Add(entity);
            context.SaveChanges();
            errorMessage = null;
            return true;
        }
        catch (Exception)
        {
            errorMessage = string.Format(
                "Could not add entity {0} to {1} field",
                typeof(TEntityToAdd).Name.ToCamelCase(),
                entitiesInParentPropertyName.ToCamelCase());
            return false;
        }
    }

    private static Relation GetRelation(PropertyInfo property)
    {
        var relationAttribute = (DatabaseRelationAttribute)property
            .GetCustomAttribute(typeof(DatabaseRelationAttribute));
        return relationAttribute?.Relation ?? Relation.None;
    }

    /// <summary>
    /// Removes entity from database.
    /// </summary>
    /// <typeparam name="TEntityToDelete">Type of entity to delete.</typeparam>
    /// <param name="entityId">Id of entity.</param>
    /// <param name="context">DbContext for performing actions.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>    
    /// <returns><see langword="true"/> if <typeparamref name="TEntityToDelete"/> was successfully deleted; otherwise, <see langword="false"/>.</returns>
    private static bool TryRemoveEntityFromDb
        <TEntityToDelete>(
        int entityId,
        IAppDbContext context,
        out string errorMessage)
        where TEntityToDelete : BaseEntity, new()
    {
        try
        {
            TEntityToDelete entity = new TEntityToDelete { Id = entityId };
            (context as DbContext).ChangeTracker.Clear();
            context.Entry(entity).State = EntityState.Deleted;
            context.SaveChanges();
            errorMessage = null;
            return true;
        }
        catch (Exception)
        {
            errorMessage = string.Format(
                "Could not delete entity with id {0}",
                entityId);
            return false;
        }
    }

    /// <summary>
    /// Removes entity from parent entity.
    /// </summary>
    /// <typeparam name="TParent">Type of parent from which entity will be deleted.</typeparam>
    /// <typeparam name="TEntityToDelete">Type of entity to delete.</typeparam>
    /// <param name="parentId">Id of parent entity.</param>
    /// <param name="entityId">Id of entity.</param>
    /// <param name="entitiesInParentPropertyName">Name of property, from which entity will be deleted.</param>
    /// <param name="context">DbContext for performing actions.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>    
    /// <returns><see langword="true"/> if <typeparamref name="TEntityToDelete"/> was successfully deleted; otherwise, <see langword="false"/>.</returns>
    private static bool TryRemoveEntityFromParent
        <TParent, TEntityToDelete>(
        int parentId,
        int entityId,
        string entitiesInParentFieldName,
        IAppDbContext context,
        out string errorMessage)
        where TParent : BaseEntity, new()
        where TEntityToDelete : BaseEntity, new()
    {
        try
        {
            TEntityToDelete entity = new TEntityToDelete { Id = entityId };
            TParent parent = new TParent { Id = parentId };
            var listProperty = typeof(TParent).GetProperty(entitiesInParentFieldName);
            ICollection<TEntityToDelete> list = (ICollection<TEntityToDelete>)listProperty.GetValue(parent);
            list.Add(entity);
            (context as DbContext).ChangeTracker.Clear();
            context.Entry(parent).State = EntityState.Unchanged;
            context.Entry(entity).State = EntityState.Unchanged;
            list.Remove(entity);
            context.SaveChanges();
            errorMessage = null;
            return true;
        }
        catch (Exception)
        {
            errorMessage = string.Format(
                "Could not delete entity '{0}' with id {1} from parent with id {2}",
                entitiesInParentFieldName.ToCamelCase(),
                entityId,
                parentId);
            return false;
        }
    }

    /// <summary>
    /// Creates new query to perform replace action.
    /// </summary>
    /// <typeparam name="TBaseEntity">Type of generic in <paramref name="query"/>.</typeparam>
    /// <param name="dbSet">The DbSet from which the context will be taken.</param>
    /// <param name="query">Request query.</param>
    /// <param name="segments">Property path segments.</param>
    /// <param name="contractResolver">Needs to be in API, idk.</param>
    /// <param name="value">New property value.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>    
    /// <returns><see langword="true"/> if update was successfully performed; otherwise, <see langword="false"/>.</returns>
    private bool TryReplaceWithNewQuery<TBaseEntity>(
        object dbSet,
        IQueryable<TBaseEntity> query,
        string[] segments,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
        where TBaseEntity : BaseEntity
    {
        for (int i = 0; i < segments.Length; i++)
        {
            Type? propertyType = typeof(TBaseEntity).GetProperty(segments[i])?.PropertyType;

            if (propertyType == null && int.TryParse(segments[i], out int entityId))
            {
                query = query.Where(e => e.Id == entityId);
                continue;
            }

            else if (i + 1 < segments.Length && propertyType.IsCollection()
                && TryGetQueryFromProperty(dbSet, query, segments[i], out var newQuery))
            {
                Type entityType = newQuery.ElementType;
                return InvokeTryReplaceWithNewQuery(
                    newQuery,
                    newQuery,
                    entityType,
                    segments[++i..],
                    contractResolver,
                    value,
                    out errorMessage);
            }

            if (propertyType != null && !propertyType.IsCollection())
            {
                if (!TryConvertValue(
                    value,
                    propertyType!,
                    out var convertedValue,
                    out errorMessage))
                {
                    errorMessage = $"'{value}' is not correct value for '{propertyType.Name}' type";
                    return false;
                }

                if (!TryGetExecuteUpdateLambda<TBaseEntity>(
                    segments[i],
                    convertedValue,
                    out var expression,
                    out errorMessage))
                {
                    return false;
                }
                query.ExecuteUpdate(expression);
            }

            else if (i == segments.Length - 1)
            {
                // Реализовать логику многих ко многим
            }

            else
            {
                errorMessage = "Could not define expression while executing 'replace' method";
                return false;
            }
        }
        errorMessage = null;
        return true;
    }



    /// <summary>
    /// Gets lambda of property itself.
    /// </summary>
    /// <typeparam name="TBaseEntity">Type of entity containing property.</typeparam>
    /// <param name="propertyName">Name of property.</param>
    /// <param name="expression">Result expression.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>    
    /// <returns><see langword="true"/> if expression was successfully created; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetPropertyLambda<TBaseEntity>(
        string propertyName,
        out Expression<Func<TBaseEntity, object>> expression,
        out string errorMessage)
    {
        try
        {
            var parameter = Expression.Parameter(typeof(TBaseEntity), "x");
            var property = Expression.Property(parameter, propertyName);

            expression = Expression.Lambda<Func<TBaseEntity, object>>(property, parameter);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            expression = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Gets expression for <c>ExecuteUpdate()</c> method.
    /// </summary>
    /// <typeparam name="TBaseEntity">Type of entity containing property.</typeparam>
    /// <param name="propertyName">Name of property.</param>
    /// <param name="value">New value for property.</param>
    /// <param name="expression">Result expression.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>    
    /// <returns><see langword="true"/> if expression was successfully created; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetExecuteUpdateLambda<TBaseEntity>(
        string propertyName,
        object? value,
        out Expression<Func<SetPropertyCalls<TBaseEntity>, SetPropertyCalls<TBaseEntity>>> expression,
        out string errorMessage)
        where TBaseEntity : BaseEntity
    {
        if (!TryGetPropertyLambda<TBaseEntity>(propertyName, out var propExpr, out errorMessage))
        {
            expression = null;
            return false;
        }
        var param = Expression.Parameter(typeof(SetPropertyCalls<TBaseEntity>));

        // find method SetProperty(Func<T, TProp>, TProp):
        var method = typeof(SetPropertyCalls<TBaseEntity>)
            .GetMethods()
            .Where(info => info.Name == nameof(SetPropertyCalls<TBaseEntity>.SetProperty))
            // filter out SetProperty(Func<T, TProp>, Func<T, TProp>) overload
            .Where(info => info.GetParameters() is [_, { ParameterType.IsConstructedGenericType: false }])
            .Single();

        // construct appropriately typed generic SetProperty
        var makeGenericMethod = method.MakeGenericMethod(propExpr.Type.GetGenericArguments()[1]);

        var methodCallExpression = Expression.Call(param, makeGenericMethod, propExpr, Expression.Constant(value, value.GetType()));

        // construct final expression
        var lambdaExpression = Expression.Lambda<Func<SetPropertyCalls<TBaseEntity>, SetPropertyCalls<TBaseEntity>>>(methodCallExpression, param);
        lambdaExpression.Compile();
        expression = lambdaExpression;
        return true;
    }

    /// <summary>
    /// Creates new DbSet query from property type.
    /// </summary>
    /// <typeparam name="TBaseEntity">Type of entity containing property.</typeparam>
    /// <param name="dbSet">The DbSet from which the context will be taken.</param>
    /// <param name="query">Query to get type of entity</param>
    /// <param name="propertyName">Name of property.</param>
    /// <param name="newQuery">Result DbSet query.</param>
    /// <returns></returns>
    private static bool TryGetQueryFromProperty<TBaseEntity>(
        object dbSet,
        IQueryable<TBaseEntity> query,
        string propertyName,
        out IQueryable newQuery)
        where TBaseEntity : BaseEntity
    {
        newQuery = null;
        var propertyInfo = typeof(TBaseEntity).GetProperty(propertyName);
        if (propertyInfo == null || propertyInfo.PropertyType.GetGenericArguments().Length == 0)
            return false;
        Type genericOfSet = propertyInfo.PropertyType.GetGenericArguments()[0];
        return (dbSet as DbSet<TBaseEntity>).TryGetDbSetFromAnotherDbSet(genericOfSet, out newQuery);
    }

    /// <summary>
    /// Labels entity and it's children as added.
    /// </summary>
    /// <param name="entity">Entity instance.</param>
    /// <param name="context">DbContext for performing actions.</param>
    private static void AddEntityAndItsChildrenToContext(object entity, IAppDbContext context)
    {
        foreach (var property in entity.GetType().GetProperties())
        {
            if (property.PropertyType.IsSubclassOf(typeof(BaseEntity)))
            {
                var childEntity = property.GetValue(entity);
                if (childEntity != null)
                {
                    AddEntityAndItsChildrenToContext(childEntity, context);
                }
            }
            else if (property.PropertyType.IsGenericType &&
                     property.PropertyType.IsCollection())
            {
                var childEntities = (IEnumerable)property.GetValue(entity);
                if (childEntities != null)
                {
                    foreach (var childEntity in childEntities)
                    {
                        AddEntityAndItsChildrenToContext(childEntity, context);
                    }
                }
            }
        }

        context.Entry(entity).State = EntityState.Added;
    }

    /// <summary>
    /// Uses convert method from AspNerCore.JsonPatch library and/or implicit/explicit type casting.
    /// </summary>
    protected virtual bool TryConvertValue(
        object originalValue,
        Type listTypeArgument,
        out object convertedValue,
        out string errorMessage)
    {
        convertedValue = null;
        errorMessage = null;
        var conversionResult = ConversionResultProvider.ConvertTo(originalValue, listTypeArgument);
        if (conversionResult.CanBeConverted)
        {
            convertedValue = conversionResult.ConvertedInstance;
            return true;
        }
        else if (DtoExtension.CanConvert(originalValue, listTypeArgument))
        {
            convertedValue = DtoExtension.ConvertToTargetType(originalValue, listTypeArgument);
            return true;
        }
        errorMessage = AdapterError.FormatInvalidValueForProperty(originalValue);
        return false;
    }

    #endregion

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
        out object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }
}
