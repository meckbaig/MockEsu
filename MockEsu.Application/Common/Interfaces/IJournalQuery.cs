using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.Interfaces
{
    internal interface IJournalQuery<T> where T : class
    {
        public int skip { get; }
        public int take { get; }
        public Dictionary<string, string> filters { get; }
    }
}
