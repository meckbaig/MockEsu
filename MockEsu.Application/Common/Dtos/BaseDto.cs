using AutoMapper;
using AutoMapper.Internal;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.StringExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MockEsu.Application.Common.Dtos;

public abstract record BaseDto
{
    // public object this[string propertyName]
    // {
    //     get
    //     {
    //         Type myType = GetType();
    //         PropertyInfo myPropInfo = myType.GetProperty(propertyName);
    //         return myPropInfo.GetValue(this, null);
    //     }
    //     set
    //     {
    //         Type myType = GetType();
    //         PropertyInfo myPropInfo = myType.GetProperty(propertyName);
    //         myPropInfo.SetValue(this, value, null);
    //     }
    // }

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
        string serialized = JsonConvert.SerializeObject(value);
        var jsonValueType = JToken.Parse(serialized).Type;

        if (typeof(BaseDto).IsAssignableFrom(dtoType))
        {
            if (jsonValueType == JTokenType.Object)
                return GetSourceValueFromJsonObject(dtoType, provider, serialized);
            throw new ArgumentException("Value is not an object.");
        }
        if (dtoType.IsArray)
        {
            if (jsonValueType == JTokenType.Array)
                return GetSourceValueFromJsonArray(dtoType, provider, serialized);
            throw new ArgumentException("Value is not an array.");
        }
                return value;
        }
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
            if (!InvokeTryGetSource(property.Key.ToPascalCase(), provider, ref propertyType, out string newKey))
                throw new ArgumentNullException($"Something went wrong while getting json patch source property path for '{property.Key}'");
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
        propertyType = typeof(TSource);
        if (dtoPath.Length == 0)
            return dtoPath;

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
                if (!InvokeTryGetSource(segment, provider, ref propertyType, out string sourceSegment))
                    throw new ArgumentNullException($"Something went wrong while getting json patch source property path for '{dtoPath}'");
                if (typeof(IList).IsAssignableFrom(propertyType))
                    nextSegmentMustBeElementOfCollection = true;
                sourcePathSegments.Add(sourceSegment);
            }
            else
                throw new ArgumentException($"Segment '{segment}' must be Id of entity in collection");
        }
        return string.Join(".", sourcePathSegments);
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
        bool throwException = true)
    {
        var methodInfo = typeof(BaseDto).GetMethod(
                            nameof(TryGetSource),
                            BindingFlags.Static | BindingFlags.NonPublic);
        var genericMethod = methodInfo.MakeGenericMethod(GetDtoOriginType(nextPropertyType), nextPropertyType);
        object[] parameters = [dtoProperty, provider, null, null, throwException];
        object result = genericMethod.Invoke(null, parameters);
        bool boolResult = (bool)result;
        if (boolResult)
        {
            sourceProperty = (string)parameters[2];
            nextPropertyType = (Type)parameters[3];
        }
        else
            sourceProperty = string.Empty;
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
    /// <param name="throwException">Throws exception if <see langword="true"/>; otherwise, returns false.</param>
    /// <returns><see langword="true"/> if <paramref name="dtoPropertyType" /> was found successfully; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetSource<TSource, TDto>(
        string dtoProperty,
        IConfigurationProvider provider,
        out string sourceProperty,
        out Type dtoPropertyType,
        bool throwException = true)
    {
        var internalApi = provider.Internal();
        var map = internalApi.FindTypeMapFor<TSource, TDto>();
        var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.DestinationMember.Name == dtoProperty);

        if (propertyMap == null)
        {
            if (!throwException)
            {
                sourceProperty = null;
                dtoPropertyType = null;
                return false;
            }
            else
            {
                throw new ArgumentException($"Property '{dtoProperty.ToCamelCase()}' does not exist");
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

    private static ValidationException PropertyNotExistsValidationException(string sourceProperty)
    {
        return new ValidationException(new Dictionary<string, ErrorItem[]>
                    {
                        {
                            "filters",
                            [
                                new ErrorItem(
                                    $"Property '{sourceProperty.ToCamelCase()}' does not exist",
                                    ValidationErrorCode.PropertyDoesNotExistValidator
                                )
                            ]
                        }
                    });
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