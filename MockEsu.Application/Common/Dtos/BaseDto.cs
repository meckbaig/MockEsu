using AutoMapper;
using AutoMapper.Internal;
using MockEsu.Application.DTOs.Roles;
using MockEsu.Application.Extensions.StringExtensions;
using MockEsu.Domain.Entities.Authentification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.AccessControl;
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
}