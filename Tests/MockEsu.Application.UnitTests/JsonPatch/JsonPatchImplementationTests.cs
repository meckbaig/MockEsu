using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using MockEsu.Application.Services.Tariffs;
using Newtonsoft.Json.Serialization;
using System.Transactions;
using Testcontainers.PostgreSql;
using Xunit;
using static MockEsu.Application.UnitTests.ValidationTestsEntites;

namespace MockEsu.Application.UnitTests.JsonPatch
{
    public class JsonPatchImplementationTests : TestWithContainer
    {
        ///TODO: прогуглить, как там умные люди делают фабрики, чтобы тесты диспозить после выполнения. Надо влепить транзакции, чтобы не пересоздавать несколько баз данных
        private readonly IMapper _mapper;

        public JsonPatchImplementationTests()
        {
            var config = new MapperConfiguration(c =>
            {
                c.AddProfile<TestEntityDto.Mapping>();
                c.AddProfile<TestNestedEntityDto.Mapping>();
                c.AddProfile<TestEntityEditDto.Mapping>();
                c.AddProfile<TestNestedEntityEditDto.Mapping>();
                c.AddProfile<TestEditDtoWithLongNameMapping.Mapping>();
            });
            _mapper = config.CreateMapper();
            if (Context == null)
                throw new Exception("Context is null");
        }

        [Fact]
        public async Task TestPatch_ReturnsOk_WhenDefault()
        {
            // Arrange

            var command = new TestJsonPatchCommand
            {
                Patch = new JsonPatchDocument<TestEntityEditDto>()
            };

            var handler = new TestJsonPatchCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
        }

        [Fact]
        public async Task TestPatchReplace_ReturnsOk_WhenStringProperty()
        {
            // Arrange
            string newEntityName = "NewValue1";

            List<Operation<TestEntityEditDto>> operations = new()
            {
                new Operation<TestEntityEditDto>
                {
                    op = "replace",
                    path = "/1/entityName",
                    value = newEntityName
                }
            };

            var command = new TestJsonPatchCommand
            {
                Patch = new JsonPatchDocument<TestEntityEditDto>(
                    operations,
                    new CamelCasePropertyNamesContractResolver())
            };

            var handler = new TestJsonPatchCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newEntityName, result.TestEntities.FirstOrDefault(x => x.Id == 1)?.EntityName);
        }

        [Fact]
        public async Task TestPatchReplace_ReturnsOk_WhenNestedStringPropertyInModel()
        {
            // Arrange
            string newEntityName = "NewValue1";

            List<Operation<TestEntityEditDto>> operations = new()
            {
                new Operation<TestEntityEditDto>
                {
                    op = "replace",
                    path = "/1/someInnerEntity/nestedName",
                    value = newEntityName
                }
            };

            var command = new TestJsonPatchCommand
            {
                Patch = new JsonPatchDocument<TestEntityEditDto>(
                    operations,
                    new CamelCasePropertyNamesContractResolver())
            };

            var handler = new TestJsonPatchCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newEntityName, result.TestEntities.FirstOrDefault(x => x.Id == 1)?.SomeInnerEntity.NestedName);
        }

        [Fact]
        public async Task TestPatchReplace_ReturnsOk_WhenNestedStringPropertyInCollection()
        {
            // Arrange
            string newEntityName = "NewValue1";

            List<Operation<TestEntityEditDto>> operations = new()
            {
                new Operation<TestEntityEditDto>
                {
                    op = "replace",
                    path = "/1/nestedThings/2/nestedName",
                    value = newEntityName
                }
            };

            var command = new TestJsonPatchCommand
            {
                Patch = new JsonPatchDocument<TestEntityEditDto>(
                    operations,
                    new CamelCasePropertyNamesContractResolver())
            };

            var handler = new TestJsonPatchCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                newEntityName,
                result.TestEntities
                    .FirstOrDefault(x => x.Id == 1)?
                        .NestedThings
                            .FirstOrDefault(x => x.Id == 2)
                                .NestedName);
        }

        [Fact]
        public async Task TestPatchReplace_ReturnsOk_WhenPropertyWithComplexMapping()
        {
            // Arrange
            string newDateString = "31 декабря 2020 г.";

            List<Operation<TestEntityEditDto>> operations = new()
            {
                new Operation<TestEntityEditDto>
                {
                    op = "replace",
                    path = "/1/dateString",
                    value = newDateString
                }
            };

            var command = new TestJsonPatchCommand
            {
                Patch = new JsonPatchDocument<TestEntityEditDto>(
                    operations,
                    new CamelCasePropertyNamesContractResolver())
            };

            var handler = new TestJsonPatchCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                newDateString,
                result.TestEntities.FirstOrDefault(x => x.Id == 1)?.DateString);
        }

        [Fact]
        public async Task TestPatchReplace_ReturnsOk_WhenPropertyWithModelId()
        {
            // Arrange
            List<Operation<TestEntityEditDto>> operations = new()
            {
                new Operation<TestEntityEditDto>
                {
                    op = "replace",
                    path = "/1/someInnerEntityId",
                    value = 2
                }
            };

            var command = new TestJsonPatchCommand
            {
                Patch = new JsonPatchDocument<TestEntityEditDto>(
                    operations,
                    new CamelCasePropertyNamesContractResolver())
            };

            var handler = new TestJsonPatchCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                2,
                result.TestEntities.FirstOrDefault(x => x.Id == 1)?.SomeInnerEntity.Id);
        }

        [Fact]
        public async Task TestPatchReplace_ReturnsOk_WhenPropertyWithLongMapping()
        {
            // Arrange
            string newName = "NewNestedEntityNameValue1";

            List<Operation<TestEditDtoWithLongNameMapping>> operations = new()
            {
                new Operation<TestEditDtoWithLongNameMapping>
                {
                    op = "replace",
                    path = "/1/nestedName",
                    value = newName
                }
            };

            var command = new TestJsonPatchLongMappingCommand
            {
                Patch = new JsonPatchDocument<TestEditDtoWithLongNameMapping>(
                    operations,
                    new CamelCasePropertyNamesContractResolver())
            };

            var handler = new TestJsonPatchLongMappingCommandHandler(Context, _mapper);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                newName,
                result.TestEntities.FirstOrDefault(x => x.Id == 1)?.SomeInnerEntity.NestedName);
        }
    }
}
