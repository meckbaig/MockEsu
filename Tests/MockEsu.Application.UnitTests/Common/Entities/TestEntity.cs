using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MockEsu.Application.UnitTests.ValidationTestsEntites;

namespace MockEsu.Application.UnitTests.Common.Entities;

public class TestEntity : BaseEntity
{
    public string Name { get; set; }

    public string Description { get; set; }

    public DateOnly Date { get; set; }

    public int SomeCount { get; set; }

    [DatabaseRelation(Domain.Enums.Relation.ManyToMany)]
    public HashSet<TestNestedEntity> TestNestedEntities { get; set; }

    [ForeignKey(nameof(InnerEntityId))]
    public TestNestedEntity InnerEntity { get; set; }

    public int InnerEntityId { get; set; }
}
