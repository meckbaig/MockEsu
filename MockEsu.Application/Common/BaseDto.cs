using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.Internal;
using System.Reflection;
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

        //public string Destination<TSrc, TDst>(MapperConfiguration mapper, string sourceProperty)
        //{
        //    var internalAPI = InternalApi.Internal(mapper);
        //    var map = internalAPI.FindTypeMapFor<TSrc, TDst>();
        //    var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.SourceMember == typeof(TSrc).GetProperty(sourceProperty));

        //    return propertyMap.DestinationMember.Name;
        //}

        public static string GetSource<TSource, TDestintaion>(IConfigurationProvider provider, string sourceProperty)
        {
            var internalAPI = InternalApi.Internal(provider);
            var map = internalAPI.FindTypeMapFor<TSource, TDestintaion>();
            var propertyMap = map.PropertyMaps.FirstOrDefault(pm => pm.DestinationMember.Name == sourceProperty);

            if (propertyMap.CustomMapExpression == null)
                return propertyMap?.SourceMember?.Name;
            else
                return GetPropertyMapSource(propertyMap.CustomMapExpression.Body);
        }

        private static string GetPropertyMapSource(System.Linq.Expressions.Expression body)
        {
            return body.ToString().Substring(body.ToString().IndexOf('.')+1);
        }
    }
}
