using MockEsu.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Domain.Common;

public class DatabaseRelationAttribute : Attribute
{
    public Relation Relation { get; set; }

    public DatabaseRelationAttribute(Relation relation)
    {
        Relation = relation;
    }
}
