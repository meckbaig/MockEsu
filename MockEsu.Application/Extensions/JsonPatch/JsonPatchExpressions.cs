using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.Common;
using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.JsonPatch;

internal static class JsonPatchExpressions
{
    internal static TDestination ApplyTo<TDto, TDestination>
        (this JsonPatchDocument<TDto> patch, TDestination? destination, IMapper mapper)
        where TDto : BaseDto
        where TDestination : BaseEntity
    {
        var dto = mapper.Map<TDto>(destination);
        patch.ApplyTo(dto);
        mapper.Map(dto, destination);
        return destination;
    }

}
