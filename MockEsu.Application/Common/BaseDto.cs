﻿using AutoMapper;
using AutoMapper.Internal;
using MockEsu.Application.Common.Exceptions;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MockEsu.Application.Common;

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
    /// Get map source name from DTO property name
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <param name="sourceProperty">DTO property name</param>
    /// <returns>Source property path</returns>
    public static string GetSource<TSource, TDestintaion>(string sourceProperty, IConfigurationProvider provider,
        bool throwException = true)
    {
        var internalApi = InternalApi.Internal(provider);
        var map = internalApi.FindTypeMapFor<TSource, TDestintaion>();
        var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.DestinationMember.Name == sourceProperty);

        if (propertyMap == null)
            if (throwException)
                throw PropertyNotExistsValidationException(sourceProperty);
            else
                return null;

        if (propertyMap.CustomMapExpression == null)
            return propertyMap?.SourceMember?.Name;
        return GetPropertyMapSource(propertyMap.CustomMapExpression.Body);
    }

    private static ValidationException PropertyNotExistsValidationException(string sourceProperty)
    {
        return new ValidationException(new Dictionary<string, ErrorItem[]>
                    {
                        {
                            "filters",
                            [
                                new ErrorItem(
                                    $"Property '{JsonNamingPolicy.CamelCase.ConvertName(sourceProperty)}' does not exist",
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

    //internal abstract Type GetOriginType();
}