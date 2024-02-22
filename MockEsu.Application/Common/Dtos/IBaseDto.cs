using AutoMapper;
using AutoMapper.Internal;
using MockEsu.Application.DTOs.Roles;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Domain.Entities.Authentification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace MockEsu.Application.Common.Dtos;

public interface IBaseDto
{
    static abstract Type GetOriginType();
}