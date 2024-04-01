using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NotCachedAttribute : Attribute
    {

    }
}
