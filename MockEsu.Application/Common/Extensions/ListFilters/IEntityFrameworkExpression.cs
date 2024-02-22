using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.ListFilters
{
    public interface IEntityFrameworkExpression<T> where T : Enum
    {
        public string? Key { get; set; }
        public string? EndPoint { get; set; }
        public T ExpressionType { get; set; }
    }  
}
