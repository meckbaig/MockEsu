﻿using AutoMapper.Internal;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Domain.Common;
using MockEsu.Domain.Enums;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace MockEsu.Application.Extensions.JsonPatch;

public class CustomDbSetAdapter<TEntity> : IAdapter where TEntity : BaseEntity, new()
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

        IDbContext context = dbSet.GetContext();
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

        List<Type> segmentTypes = GetSegmentsTypes(segments);

        IDbContext context = dbSet.GetContext();
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
        IDbContext context,
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
        IDbContext context,
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
        IDbContext context,
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
        IDbContext context,
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

    /// <summary>
    /// Adds items into collection to create many-to-many connections through Entity Framework.
    /// </summary>
    /// <param name="collectionProperty">Collection property in parent entity.</param>
    /// <param name="parent">Parent entity itself.</param>
    /// <param name="context">Entity Framework DbContext.</param>
    /// <param name="collection">Value of collection property in parent entity, which is tracked by Entity Framework.</param>
    private static void InvokeReplaceWithManyToMany(
        PropertyInfo collectionProperty,
        object parent,
        IDbContext context,
        ref IEnumerable collection)
    {
        Type geneticOfCollection = collection.GetType().GetGenericArguments().Single();
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>).GetMethod(
            nameof(ReplaceWithManyToMany),
            BindingFlags.Static | BindingFlags.NonPublic);
        var genericMethod = methodInfo.MakeGenericMethod(geneticOfCollection);
        object[] parameters = [collectionProperty, parent, context, collection];
        object result = genericMethod.Invoke(null, parameters);
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
        IDbContext context,
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
        catch (Exception ex)
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
        IDbContext context,
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
            if (listProperty != null && list == null)
            {
                FillCollectionWithNullValue(listProperty, parent, ref list);
            }
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

    private static void FillCollectionWithNullValue
        <TEntityToAdd>(
        PropertyInfo listProperty,
        object parent,
        ref ICollection<TEntityToAdd> list)
        where TEntityToAdd : BaseEntity, new()
    {
        Type listType = listProperty.PropertyType;
        list = (ICollection<TEntityToAdd>)Activator.CreateInstance(listType);
        listProperty.SetValue(parent, list);
        list = (ICollection<TEntityToAdd>)listProperty.GetValue(parent);
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
        IDbContext context,
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
        catch (Exception ex)
        {
            errorMessage = string.Format(
                "Could not remove entity with id {0}",
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
        string entitiesInParentPropertyName,
        IDbContext context,
        out string errorMessage)
        where TParent : BaseEntity, new()
        where TEntityToDelete : BaseEntity, new()
    {
        try
        {
            TEntityToDelete entity = new TEntityToDelete { Id = entityId };
            TParent parent = new TParent { Id = parentId };
            var listProperty = typeof(TParent).GetProperty(entitiesInParentPropertyName);
            ICollection<TEntityToDelete> list = (ICollection<TEntityToDelete>)listProperty.GetValue(parent);
            if (listProperty != null && list == null)
            {
                FillCollectionWithNullValue(listProperty, parent, ref list);
            }
            list.Add(entity);
            (context as DbContext).ChangeTracker.Clear();
            context.Entry(parent).State = EntityState.Unchanged;
            context.Entry(entity).State = EntityState.Unchanged;
            list.Remove(entity);
            context.SaveChanges();
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = string.Format(
                "Could not remove entity '{0}' with id {1} from parent with id {2}",
                entitiesInParentPropertyName.ToCamelCase(),
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
        where TBaseEntity : BaseEntity, new()
    {
        Type? propertyType = typeof(TBaseEntity);
        for (int i = 0; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out int _))
                continue;

            propertyType = propertyType.GetProperty(segments[i])?.PropertyType;

            if (i + 1 < segments.Length && propertyType.IsCollection()
                && TryGetQueryFromProperty(dbSet, query, segments[i], out IQueryable newQuery))
            {
                propertyType = newQuery.ElementType;
                return InvokeTryReplaceWithNewQuery(
                    dbSet,
                    newQuery,
                    propertyType,
                    segments[++i..],
                    contractResolver,
                    value,
                    out errorMessage);
            }

            if (i + 1 == segments.Length && propertyType != null && !propertyType.IsCollection())
            {
                if (!TryConvertValue(
                    value,
                    propertyType!,
                    out var convertedValue,
                    out _))
                {
                    errorMessage = $"'{value}' is not correct value for '{propertyType.Name}' type";
                    return false;
                }

                IDbContext context = (dbSet as DbSet<TEntity>).GetContext();
                if (segments[1..].Length == 1)
                {
                    return TryReplaceParameterInEntity<TBaseEntity>(
                        Convert.ToInt32(segments[0]),
                        segments[1],
                        convertedValue,
                        context,
                        out errorMessage);
                }
                return TryReplaceNestedParameterInEntity<TBaseEntity>(
                    Convert.ToInt32(segments[0]),
                    segments[1..],
                    convertedValue,
                    context,
                    out errorMessage);
            }
        }
        errorMessage = "Nothing was done due to unknown reason";
        return false;
    }

    private static bool TryReplaceParameterInEntity
        <TBaseEntity>(
        int entityId,
        string propertyName,
        object value,
        IDbContext context,
        out string errorMessage)
        where TBaseEntity : BaseEntity, new()
    {
        try
        {
            TBaseEntity entity = new TBaseEntity { Id = entityId };
            var property = typeof(TBaseEntity).GetProperty(propertyName);
            (context as DbContext).ChangeTracker.Clear();
            context.Entry(entity).State = EntityState.Unchanged;
            property.SetMemberValue(entity, value);
            context.SaveChanges();
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = string.Format(
                "Could not replace value of property {0} from entity with id {1}",
                propertyName.ToCamelCase(),
                entityId);
            return false;
        }
    }

    /// TODO: дописать это чудище, там что-то чатгпт написал, возможно пригодится. 
    /// После чего сделать поумнее перебор в основном методе, так как если там будут в пути Id, он помрёт на данном методе
    private static bool TryReplaceNestedParameterInEntity<TBaseEntity>(
        int entityId,
        string[] pathSegments,
        object value,
        IDbContext context,
        out string errorMessage)
        where TBaseEntity : BaseEntity, new()
    {
        errorMessage = null;
        try
        {
            (context as DbContext).ChangeTracker.Clear();
            var query = (IQueryable<TBaseEntity>)context.Set<TBaseEntity>();
            if (pathSegments.Length > 1)
                query = query.Include(string.Join('.', pathSegments[..^1]));

            TBaseEntity entity = query.FirstOrDefault(e => e.Id == entityId);
            if (entity == null)
            {
                errorMessage = string.Format(
                    "Could not find entity {0} with id {1}",
                    typeof(TBaseEntity).Name.ToCamelCase(),
                    entityId);
                return false;
            }

            var property = typeof(TBaseEntity).GetProperty(pathSegments[0]);
            if (property == null)
            {
                errorMessage = string.Format(
                    "Could not find property {0} in entity {1}",
                    pathSegments[0].ToCamelCase(),
                    typeof(TBaseEntity).Name.ToCamelCase());
                return false;
            }

            if (pathSegments.Length == 1)
            {
                property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
            }
            else
            {
                SetNestedProperty(property.GetValue(entity), pathSegments.Skip(1).ToArray(), value, out errorMessage);
            }

            context.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static void SetNestedProperty(
        object obj,
        string[] pathSegments,
        object value,
        out string errorMessage)
    {
        var property = obj.GetType().GetProperty(pathSegments[0]);
        if (property == null)
        {
            errorMessage = string.Format(
                "Could not find property {0} in entity {1}",
                pathSegments[0].ToCamelCase(),
                obj.GetType().Name.ToCamelCase());
            return;
        }

        if (pathSegments.Length == 1)
        {
            property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
        }
        else
        {
            SetNestedProperty(property.GetValue(obj), pathSegments.Skip(1).ToArray(), value, out errorMessage);
        }
        errorMessage = null;
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
    private static void AddEntityAndItsChildrenToContext(object entity, IDbContext context)
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
                        if (GetRelation(property) == Relation.ManyToMany)
                        {
                            /// Only adds Id's to many-to-many connection
                            InvokeReplaceWithManyToMany(property, entity, context, ref childEntities);
                            /// Edits original values in nested entities
                            //context.Entry(childEntity).State = EntityState.Modified; 
                        }
                        else
                        {
                            AddEntityAndItsChildrenToContext(childEntity, context);
                        }
                    }
                }
            }
        }

        context.Entry(entity).State = EntityState.Added;
    }

    /// <summary>
    /// Adds items into collection to create many-to-many connections through Entity Framework.
    /// </summary>
    /// <typeparam name="TEntityToAdd">Type of item in collection.</typeparam>
    /// <param name="collectionProperty">Collection property in parent entity.</param>
    /// <param name="parent">Parent entity itself.</param>
    /// <param name="context">Entity Framework DbContext.</param>
    /// <param name="collection">Value of collection property in parent entity, which is tracked by Entity Framework.</param>
    private static void ReplaceWithManyToMany
        <TEntityToAdd>(
        PropertyInfo collectionProperty,
        object parent,
        IDbContext context,
        ref ICollection<TEntityToAdd> collection)
        where TEntityToAdd : BaseEntity, new()
    {
        var collectionItems = collection;
        FillCollectionWithNullValue(collectionProperty, parent, ref collection);
        //context.Entry(collection).State = EntityState.Unchanged;
        foreach (var item in collectionItems)
        {
            context.Entry(item).State = EntityState.Unchanged;
            collection.Add(item);
        }
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

    /// <summary>
    /// Gets types of segments from <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="segments">Path segmnents.</param>
    /// <returns>List of segments types.</returns>
    private static List<Type> GetSegmentsTypes(string[] segments)
    {
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

        return segmentTypes;
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
