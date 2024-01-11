using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.Internal;
using MockEsu.Application.Common.Exceptions;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MockEsu.Application.Common
{
    public record BaseDto
    {
        public object this[string propertyName]
        {
            get
            {
                Type myType = GetType();
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = GetType();
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }

        /// <summary>
        /// Get map source name from DTO property name
        /// </summary>
        /// <typeparam name="TSource">Source of DTO type</typeparam>
        /// <typeparam name="TDestintaion">DTO type</typeparam>
        /// <param name="provider">Configuraion provider for performing maps</param>
        /// <param name="sourceProperty">DTO property name</param>
        /// <returns>Source property path</returns>
        public static string GetSource<TSource, TDestintaion>(IConfigurationProvider provider, string sourceProperty)
        {
            var internalAPI = InternalApi.Internal(provider);
            var map = internalAPI.FindTypeMapFor<TSource, TDestintaion>();
            var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.DestinationMember.Name == sourceProperty);

            if (propertyMap == null)
                throw new ValidationException(
                    new Dictionary<string, string[]> {
                        { "filters", [$"Parameter '{JsonNamingPolicy.CamelCase.ConvertName(sourceProperty)}' does not exist"] } 
                    }
                );

            if (propertyMap.CustomMapExpression == null)
                return propertyMap?.SourceMember?.Name;
            else
                return GetPropertyMapSource(propertyMap.CustomMapExpression.Body);
        }

        /// <summary>
        /// Get map source name from AutoMapper custom lambda function
        /// </summary>
        /// <param name="body">AutoMapper custom lambda function</param>
        /// <returns>Map source string</returns>
        /// <remarks>
        /// Helps with getting DTO map source (to add filters to source)
        /// </remarks>
        private static string GetPropertyMapSource(System.Linq.Expressions.Expression body)
        {
            return body.ToString().Substring(body.ToString().IndexOf('.')+1);
        }
    }
}
