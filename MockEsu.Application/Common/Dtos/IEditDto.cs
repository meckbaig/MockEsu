using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.Dtos;

public interface IEditDto
{
    static abstract Type GetOriginType();
}
