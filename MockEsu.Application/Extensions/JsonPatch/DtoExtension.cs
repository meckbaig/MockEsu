﻿using AutoMapper;
using AutoMapper.Internal;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.DTOs.Roles;
using MockEsu.Application.Extensions.StringExtensions;
using MockEsu.Domain.Entities.Authentification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace MockEsu.Application.Extensions.JsonPatch;

internal static class DtoExtension
{
    /// <summary>
    /// Gets source value from DTO mapping.
    /// </summary>
    /// <param name="value">DTO value.</param>
    /// <param name="dtoType">DTO type.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <returns>Source value.</returns>
    public static object GetSourceValueJsonPatch(
        object value,
        Type dtoType,
        IConfigurationProvider provider)
    {
        if (!TryGetSourceValueJsonPatch(
            value,
            dtoType,
            provider,
            out object sourceValue,
            out string errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }
        return sourceValue;
    }

    /// <summary>
    /// Gets source value from DTO mapping.
    /// </summary>
    /// <param name="value">DTO value.</param>
    /// <param name="dtoType">DTO type.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="sourceValue">Source value.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if source value got successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetSourceValueJsonPatch(
        object value,
        Type dtoType,
        IConfigurationProvider provider,
        out object sourceValue,
        out string errorMessage)
    {
        string serialized = JsonConvert.SerializeObject(value);
        var jsonValueType = JToken.Parse(serialized).Type;

        errorMessage = null;
        sourceValue = null;
        if (typeof(BaseDto).IsAssignableFrom(dtoType))
        {
            if (jsonValueType == JTokenType.Object)
            {
                sourceValue = GetSourceValueFromJsonObject(dtoType, provider, serialized);
                return true;
            }
            else if (CanConvert(value, GetDtoOriginType(dtoType)))
            {
                sourceValue = ConvertToTargetType(value, GetDtoOriginType(dtoType));
                return true;
            }
            errorMessage = "Value is not valid.";
            return false;
        }
        if (dtoType.IsArray || dtoType.IsListType())
        {
            if (jsonValueType == JTokenType.Array)
            {
                sourceValue = GetSourceValueFromJsonArray(dtoType, provider, serialized);
                return true;
            }
            errorMessage = "Value is not an array.";
            return false;
        }
        sourceValue = value;
        return true;
    }

    /// <summary>
    /// Checks whether explicit/implicit conversion from a value to the specified type is possible.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="targetType">Type to convert to.</param>
    /// <returns><see langword="true"/> if value can be convetred; otherwise, <see langword="false"/>.</returns>
    public static bool CanConvert(object value, Type targetType)
    {
        value = NormalizeValue(value);

        Type sourceType = value.GetType();
        foreach (var item in targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(mi => (mi.Name == "op_Implicit" || mi.Name == "op_Explicit") && mi.ReturnType == targetType))
        {
            var parameters = item.GetParameters();
            if (parameters[0].ParameterType == sourceType)
                return true; 
        }
        return false;
    }

    /// <summary>
    /// Implicitly or explicitly converts a value to the specified type.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="targetType">Type to convert to.</param>
    /// <returns>Converted value.</returns>
    public static object ConvertToTargetType(object value, Type targetType)
    {
        value = NormalizeValue(value);

        MethodInfo? conversionMethod = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(mi => (mi.Name == "op_Implicit" || mi.Name == "op_Explicit")
            && mi.ReturnType == targetType
            && mi.GetParameters()[0].ParameterType == value.GetType());
        if (conversionMethod != null)
            return conversionMethod.Invoke(null, [value]);
        return value;
    }

    /// <summary>
    /// Changes long to int if value is less than max int value.
    /// </summary>
    private static object NormalizeValue(object value)
    {
        Type t = value.GetType();
        if (t == typeof(long))
            if (((long)value) <= int.MaxValue && ((long)value) >= int.MinValue)
                value = Convert.ToInt32(value);
        return value;
    }

    /// <summary>
    /// Checks if property is entity, dto or other complex object.
    /// </summary>
    /// <returns><see langword="true"/> if property is a complex object; otherwise, <see langword="false"/>.</returns>
    private static bool IsCustomObject(Type propertyType)
    {
        if (propertyType.IsPrimitive)
            return false;
        if (propertyType == typeof(string) ||
            propertyType == typeof(DateTime) ||
            propertyType == typeof(decimal) ||
            propertyType == typeof(TimeOnly))
            return false;
        if (propertyType.IsEnum)
            return false;

        return true;
    }

    /// <summary>
    /// Gets source array from DTO mapping.
    /// </summary>
    /// <param name="dtoArrayType">DTO array type.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="serialized">DTO value serialized in string.</param>
    /// <returns>Source array value.</returns>
    private static object GetSourceValueFromJsonArray(Type dtoArrayType, IConfigurationProvider provider, string serialized)
    {
        if (!IsCustomObject(dtoArrayType.GetElementType()))
            return serialized;

        List<object> sourceObjects = new();
        List<Dictionary<string, object>> dtoDictionaries
            = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(serialized);
        Type dtoType = dtoArrayType.GetGenericArguments().Single();
        foreach (var dtoDict in dtoDictionaries)
        {
            sourceObjects.Add(GetSourceValueFromJsonObject(dtoType, provider, JsonConvert.SerializeObject(dtoDict)));
        }
        return sourceObjects;
    }

    /// <summary>
    /// Gets source object from DTO mapping.
    /// </summary>
    /// <param name="dtoType">DTO object type.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="serialized">DTO value serialized in string.</param>
    /// <returns>Source object value.</returns>
    /// <exception cref="ArgumentNullException">Exception occures when unable to find property source from DTO.</exception>
    private static object GetSourceValueFromJsonObject(Type dtoType, IConfigurationProvider provider, string serialized)
    {
        Dictionary<string, object> sourceProperties = new();
        Dictionary<string, object> properties
            = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized);
        foreach (var property in properties)
        {
            Type propertyType = dtoType;
            if (!InvokeTryGetSource(property.Key.ToPascalCase(), provider, ref propertyType, out string newKey, out string errorMessage))
                throw new ArgumentNullException(errorMessage ?? $"Something went wrong while getting json patch source property path for '{property.Key}'");
            object propValue = GetSourceValueJsonPatch(property.Value, propertyType, provider);
            sourceProperties.Add(newKey.ToCamelCase(), propValue);
        }
        return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceProperties));
    }

    /// <summary>
    /// Gets source path from DTO path.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <param name="dtoPath">DTO property path.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="propertyType">Endpoint property type.</param>
    /// <returns>Source path.</returns>
    /// <exception cref="ArgumentNullException">Exception occures when unable to find property source from DTO.</exception>
    public static string GetSourceJsonPatch<TSource>(
        string dtoPath,
        IConfigurationProvider provider,
        out Type propertyType)
        where TSource : BaseDto, IEditDto
    {
        if (!TryGetSourceJsonPatch<TSource>(
            dtoPath,
            provider,
            out propertyType,
            out string sourceJsonPach,
            out string errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }
        return sourceJsonPach;
    }

    /// <summary>
    /// Gets source path from DTO path.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <param name="dtoPath">DTO property path.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="propertyType">Endpoint property type.</param>
    /// <param name="sourceJsonPatch">Source path.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if source path got successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetSourceJsonPatch<TSource>(
        string dtoPath,
        IConfigurationProvider provider,
        out Type propertyType,
        out string sourceJsonPatch,
        out string errorMessage)
        where TSource : BaseDto, IEditDto
    {
        propertyType = typeof(TSource);
        errorMessage = null;
        sourceJsonPatch = null;
        if (dtoPath.Length == 0)
        {
            sourceJsonPatch = dtoPath;
            return true;
        }

        string[] pathSegments = dtoPath.Split('.');
        List<string> sourcePathSegments = new();
        bool nextSegmentMustBeElementOfCollection = false;
        foreach (string segment in pathSegments)
        {
            if (nextSegmentMustBeElementOfCollection &&
                (int.TryParse(segment, out int _) || segment == "-"))
            {
                nextSegmentMustBeElementOfCollection = false;
                sourcePathSegments.Add(segment);
                propertyType = propertyType.GetGenericArguments().Single();
            }
            else if (!nextSegmentMustBeElementOfCollection)
            {
                if (!InvokeTryGetSource(segment, provider, ref propertyType, out string sourceSegment, out errorMessage, throwException: false))
                {
                    errorMessage ??= $"Something went wrong while getting json patch source property path for '{dtoPath}'";
                    return false;
                }
                if (propertyType.IsCollection())
                    nextSegmentMustBeElementOfCollection = true;
                sourcePathSegments.Add(sourceSegment);
            }
            else
            {
                errorMessage = $"Segment '{segment}' must be Id of entity in collection";
                return false;
            }
        }
        sourceJsonPatch = string.Join(".", sourcePathSegments);
        return true;
    }

    /// <summary>
    /// Gets map source name from DTO property name.
    /// </summary>
    /// <param name="dtoProperty">DTO property name.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="nextPropertyType">The type of the DTO containing the parameter. Replaced by the parameter type.</param>
    /// <param name="sourceProperty">Source property path.</param>
    /// <param name="throwException">Throws exception if <see langword="true"/>; otherwise, returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="sourceProperty" /> was found successfully; otherwise, <see langword="false"/>.</returns>
    public static bool InvokeTryGetSource(
        string dtoProperty,
        IConfigurationProvider provider,
        ref Type nextPropertyType,
        out string sourceProperty,
        out string errorMessage,
        bool throwException = true)
    {
        var methodInfo = typeof(DtoExtension).GetMethod(
                            nameof(TryGetSource),
                            BindingFlags.Static | BindingFlags.NonPublic);
        var genericMethod = methodInfo.MakeGenericMethod(GetDtoOriginType(nextPropertyType), nextPropertyType);
        object[] parameters = [dtoProperty, provider, null, null, null, throwException];
        object result = genericMethod.Invoke(null, parameters);
        bool boolResult = (bool)result;
        if (boolResult)
        {
            sourceProperty = (string)parameters[2];
            nextPropertyType = (Type)parameters[3];
        }
        else
        {
            sourceProperty = string.Empty;
        }
        errorMessage = (string)parameters[4];
        return boolResult;
    }

    /// <summary>
    /// Gets origin of provided DTO type.
    /// </summary>
    /// <param name="dtoType">DTO type.</param>
    /// <returns>DTO source type.</returns>
    private static Type GetDtoOriginType(Type dtoType)
    {
        if (!typeof(IEditDto).IsAssignableFrom(dtoType))
            throw new ArgumentException($"{dtoType.Name} does not implement the interface {nameof(IEditDto)}");

        MethodInfo method = dtoType.GetMethod(nameof(IEditDto.GetOriginType), BindingFlags.Static | BindingFlags.Public);
        var result = method.Invoke(null, null);
        return (Type)result;
    }

    private static Type GetDtoValidatorType(Type dtoType)
    {
        if (!typeof(IEditDto).IsAssignableFrom(dtoType))
            throw new ArgumentException($"{dtoType.Name} does not implement the interface {nameof(IEditDto)}");

        MethodInfo method = dtoType.GetMethod(nameof(IEditDto.GetValidatorType), BindingFlags.Static | BindingFlags.Public);
        var result = method.Invoke(null, null);
        return (Type)result;
    }

    /// <summary>
    /// Gets map source name from DTO property name.
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type.</typeparam>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="dtoProperty">DTO property name.</param>
    /// <returns>Source property path.</returns>
    public static string GetSource<TSource, TDto>(
        string dtoProperty,
        IConfigurationProvider provider,
        bool throwException = true)
    {
        if (TryGetSource<TSource, TDto>(
            dtoProperty,
            provider,
            out string source,
            out Type _,
            out string _,
            throwException))
        {
            return source;
        }
        return null;
    }

    /// <summary>
    /// Gets map source name from DTO property name.
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type.</typeparam>
    /// <typeparam name="TDto">DTO type.</typeparam>
    /// <param name="dtoProperty">DTO property name.</param>
    /// <param name="provider">Configuraion provider for performing maps.</param>
    /// <param name="sourceProperty">Source property name.</param>
    /// <param name="dtoPropertyType">DTO property type.</param>
    /// <param name="errorMessage">Message if error occures; otherwise, <see langword="null"/>.</param>
    /// <param name="throwException">Throws exception if <see langword="true"/>; otherwise, returns false.</param>
    /// <returns><see langword="true"/> if <paramref name="dtoPropertyType" /> was found successfully; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetSource<TSource, TDto>(
        string dtoProperty,
        IConfigurationProvider provider,
        out string sourceProperty,
        out Type dtoPropertyType,
        out string errorMessage,
        bool throwException = true)
    {
        if (typeof(IEditDto).IsAssignableFrom(typeof(TDto)))
            return TryGetEditSource<TSource, TDto>(
                dtoProperty,
                provider,
                out sourceProperty,
                out dtoPropertyType,
                out errorMessage,
                throwException);

        errorMessage = null;
        var internalApi = provider.Internal();
        var map = internalApi.FindTypeMapFor<TSource, TDto>();
        var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.DestinationMember.Name == dtoProperty);

        if (propertyMap == null)
        {
            errorMessage = $"Property '{dtoProperty.ToCamelCase()}' does not exist";
            if (!throwException)
            {
                sourceProperty = null;
                dtoPropertyType = null;
                return false;
            }
            else
            {
                throw new ArgumentException(errorMessage);
            }
        }

        dtoPropertyType = propertyMap?.DestinationType;
        if (propertyMap?.SourceMember?.Name != null)
        {
            sourceProperty = propertyMap?.SourceMember?.Name;
            return true;
        }
        sourceProperty = GetPropertyMapSource(propertyMap.CustomMapExpression.Body);
        return true;
    }

    /// <summary>
    /// Gets map source name from DTO property name but in a reverse.
    /// </summary>
    private static bool TryGetEditSource<TSource, TDto>(
        string dtoProperty,
        IConfigurationProvider provider,
        out string sourceProperty,
        out Type dtoPropertyType,
        out string errorMessage,
        bool throwException)
    {
        errorMessage = null;
        var internalApi = provider.Internal();
        var map = internalApi.FindTypeMapFor<TDto, TSource>();
        var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.SourceMember.Name == dtoProperty);

        if (propertyMap == null)
        {
            errorMessage = $"Property '{dtoProperty.ToCamelCase()}' does not exist";
            if (!throwException)
            {
                sourceProperty = null;
                dtoPropertyType = null;
                return false;
            }
            else
            {
                throw new ArgumentException(errorMessage);
            }
        }

        dtoPropertyType = propertyMap?.SourceType;
        if (propertyMap?.DestinationMember?.Name != null)
        {
            sourceProperty = propertyMap?.DestinationMember?.Name;
            return true;
        }
        sourceProperty = null;
        errorMessage = "Not supported get source action";
        return false;
    }

    /// <summary>
    /// Get map source name from AutoMapper custom lambda function
    /// </summary>
    /// <param name="body">AutoMapper custom lambda function</param>
    /// <returns>Map source string</returns>
    /// <remarks>
    /// Helps with getting DTO map source (to add filters to source)
    /// </remarks>
    private static string GetPropertyMapSource(Expression body)
    {
        Regex regex = new Regex("[^a-zA-Z0-9.]");
        return regex.Replace(body.ToString().Substring(body.ToString().IndexOf('.') + 1), "");
    }

}