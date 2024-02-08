using AutoMapper;
using AutoMapper.Internal;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.StringExtencions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public static object GetSourceValueJsonPatch(
        object value,
        Type type,
        IConfigurationProvider provider)
    {
        if (value == null)
            return value;
        string serialized = JsonConvert.SerializeObject(value);
        if (JToken.Parse(serialized).Type != JTokenType.Object)
            return value;

        Dictionary<string, object> sourceProperties = new();
        Dictionary<string, object> properties
            = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized);
        foreach (var prop in properties)
        {
            Type tmpType = type;
            if (!InvokeTryGetSource(prop.Key.ToPascalCase(), provider, ref tmpType, out string newKey))
                throw new ArgumentNullException($"Something went wrong while getting json patch source property path for '{prop.Key}'");
            sourceProperties.Add(newKey.ToCamelCase(), prop.Value);
        }
        return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceProperties));
    }

    public static string GetSourceJsonPatch<TSource>(
        string dtoPath,
        IConfigurationProvider provider,
        out Type propertyType)
        where TSource : BaseDto, IEditDto
    {
        if (dtoPath.Length == 0)
        {
            propertyType = TSource.GetOriginType();
            return dtoPath;
        }
        string[] pathSegments = dtoPath.Split('.');
        List<string> sourcePathSegments = new();
        propertyType = typeof(TSource);
        foreach (string segment in pathSegments)
        {
            if (int.TryParse(segment, out int _) || segment == "-")
            {
                sourcePathSegments.Add(segment);
                propertyType = propertyType.GetGenericArguments().Single();
            }
            else
            {
                if (!InvokeTryGetSource(segment, provider, ref propertyType, out string sourceSegment))
                    throw new ArgumentNullException($"Something went wrong while getting json patch source property path for '{dtoPath}'");
                sourcePathSegments.Add(sourceSegment);
            }
        }
        return string.Join(".", sourcePathSegments);
    }

    public static bool InvokeTryGetSource(
        string pathSegment, 
        IConfigurationProvider provider, 
        ref Type nextSegmentType, 
        out string sourceSegment,
        bool throwException = true)
    {
        var methodInfo = typeof(BaseDto).GetMethod(
                            nameof(TryGetSource),
                            BindingFlags.Static | BindingFlags.NonPublic);
        var genericMethod = methodInfo.MakeGenericMethod(GetDtoOriginType(nextSegmentType), nextSegmentType);
        object[] parameters = [pathSegment, provider, null, null, throwException];
        object result = genericMethod.Invoke(null, parameters);
        bool boolResult = (bool)result;
        if (boolResult)
        {
            sourceSegment = (string)parameters[2];
            nextSegmentType = (Type)parameters[3];
        }
        else
            sourceSegment = string.Empty;
        return boolResult;
    }

    private static Type GetDtoOriginType(Type dtoType)
    {
        MethodInfo method = dtoType.GetMethod("GetOriginType", BindingFlags.Static | BindingFlags.Public);
        var result = method.Invoke(null, null);
        return (Type)result;

    }

    /// <summary>
    /// Get map source name from DTO property name
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <param name="dtoProperty">DTO property name</param>
    /// <returns>Source property path</returns>
    public static string GetSource<TSource, TDestintaion>(
        string dtoProperty,
        IConfigurationProvider provider,
        bool throwException = true)
    {
        if (TryGetSource<TSource, TDestintaion>(
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

    private static bool TryGetSource<TSource, TDestintaion>(
        string dtoProperty,
        IConfigurationProvider provider,
        out string source,
        out Type dtoPropertyType,
        bool throwException = true)
    {
        var internalApi = provider.Internal();
        var map = internalApi.FindTypeMapFor<TSource, TDestintaion>();
        var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.DestinationMember.Name == dtoProperty);

        if (propertyMap == null)
        {
            if (!throwException)
            {
                source = null;
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
            source = propertyMap?.SourceMember?.Name;
            return true;
        }
        source = GetPropertyMapSource(propertyMap.CustomMapExpression.Body);
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