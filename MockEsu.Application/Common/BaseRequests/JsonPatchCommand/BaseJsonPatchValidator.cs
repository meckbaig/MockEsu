using AutoMapper;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Application.Extensions.StringExtensions;
using MockEsu.Application.Services.Tariffs;
using Newtonsoft.Json.Bson;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using static StackExchange.Redis.Role;

namespace MockEsu.Application.Common.BaseRequests.JsonPatchCommand;

public abstract class BaseJsonPatchValidator<TCommand, TResponse, TDto> : AbstractValidator<TCommand>
    where TCommand : BaseJsonPatchCommand<TResponse, TDto>
    where TResponse : BaseResponse
    where TDto : BaseDto, IEditDto, new()
{
    public BaseJsonPatchValidator(IMapper mapper)
    {
        RuleForEach(x => x.Patch.Operations).NotNull()
            .ValidateOperations<TCommand, TResponse, TDto>(mapper);
    }
}

public static class BaseJsonPatchValidatorExtension
{
    public static IRuleBuilderOptions<TCommand, Operation<TDto>>ValidateOperations
        <TCommand, TResponse, TDto>(
            this IRuleBuilderOptions<TCommand, Operation<TDto>> ruleBuilder,
            IMapper mapper)
            where TCommand : BaseJsonPatchCommand<TResponse, TDto>
            where TResponse : BaseResponse
            where TDto : BaseDto, IEditDto, new()
    {
        string canParsePathErrorMessage = null!;
        Type propertyType = null!;
        ruleBuilder = ruleBuilder
            .Must((c, o) => CanParsePath(o, mapper, out propertyType, out canParsePathErrorMessage))
            .WithMessage(x => canParsePathErrorMessage)
            .WithErrorCode(JsonPatchValidationErrorCode.CanParsePathValidator.ToString());

        string canParseValueErrorMessage = null!;
        ruleBuilder = ruleBuilder
            .Must((c, o) => CanParseValue(o, mapper, propertyType, out canParseValueErrorMessage))
            .WithMessage(x => canParseValueErrorMessage)
            .WithErrorCode(JsonPatchValidationErrorCode.CanNotParseValueValidator.ToString());
        
        ruleBuilder.Custom((o, context) => ValidateValue(o, mapper, context, propertyType));

        return ruleBuilder;
    }

    private static bool CanParsePath<TDto>(
        Operation<TDto> operation,
        IMapper mapper,
        out Type propertyType,
        out string errorMessage)
        where TDto : BaseDto, IEditDto, new()
    {
        try
        {
            var jsonPatchPath = new JsonPatchPath(operation.path);
            BaseDto.GetSourceJsonPatch<TDto>(
                jsonPatchPath.AsSingleProperty,
                mapper.ConfigurationProvider,
                out propertyType);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"{operation.path}: {ex.Message}";
            propertyType = null;
            return false;
        }
    }

    private static bool CanParseValue<TDto>(
        Operation<TDto> operation,
        IMapper mapper,
        Type propertyType,
        out string errorMessage)
        where TDto : BaseDto, IEditDto, new()
    {
        try
        {
            BaseDto.GetSourceValueJsonPatch(
                operation.value,
                propertyType,
                mapper.ConfigurationProvider);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"{operation.path}: {ex.Message}";
            return false;
        }
    }

    private static void ValidateValue<TCommand, TDto>(
        Operation<TDto> operation,
        IMapper mapper,
        ValidationContext<TCommand> context,
        Type propertyType)
        where TDto : BaseDto, IEditDto, new()
    {
        switch (operation.OperationType)
        {
            case OperationType.Add:
                ValidateAddition(operation, context, propertyType);
                break;
            case OperationType.Remove:
                break;
            case OperationType.Replace:
                ValidateReplace(operation, mapper, context);
                break;
            default:
                throw new ArgumentException($"Operation {operation.op} is not supported.");
        }        
    }

    private static void ValidateAddition<TCommand, TDto>(
        Operation<TDto> operation,
        ValidationContext<TCommand> context, 
        Type propertyType) 
        where TDto : BaseDto, IEditDto, new()
    {
        operation.path = '/' + operation.path.Split('/').Last();
        object dtosList = Activator.CreateInstance(typeof(List<>).MakeGenericType(propertyType));
        operation.Apply(dtosList, JsonPatchExpressions.Adapter);
        if (typeof(IEditDto).IsAssignableFrom(propertyType))
        {
            MethodInfo getValidatorMethod = propertyType
                .GetMethod(nameof(IEditDto.GetValidatorType),
                    BindingFlags.Static | BindingFlags.Public)!;
            Type validatorType = (Type)getValidatorMethod.Invoke(null, null);
            if (validatorType != null)
            {
                object validator = Activator.CreateInstance(validatorType);
                MethodInfo validateMethod = validatorType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                        m.Name == nameof(IValidator.Validate) &&
                        m.GetParameters().Length == 1)!;
                object[] parameters = [((IList)dtosList)[0]];
                ValidationResult result = (ValidationResult)validateMethod.Invoke(validator, parameters);
                foreach (var error in result.Errors)
                {
                    var failure = error;
                    failure.PropertyName = context.PropertyName;
                    //failure.PropertyName = string.Format("{0}.{1}", context.PropertyName, error.PropertyName);
                    context.AddFailure(failure);
                }
            }
        }
    }

    private static void ValidateReplace<TCommand, TDto>(
        Operation<TDto> operation,
        IMapper mapper,
        ValidationContext<TCommand> context)
        where TDto : BaseDto, IEditDto, new()
    {
        string pathWithoutLastSegment = string.Join('/', operation.path.Split('/').SkipLast(1));
        var jsonPatchPath = new JsonPatchPath(pathWithoutLastSegment);
        BaseDto.GetSourceJsonPatch<TDto>(
            jsonPatchPath.AsSingleProperty,
                mapper.ConfigurationProvider,
                out Type propertyType);
        operation.path = '/' + operation.path.Split('/').Last();
        object dto = Activator.CreateInstance(propertyType);
        operation.Apply(dto, JsonPatchExpressions.Adapter);
        if (typeof(IEditDto).IsAssignableFrom(propertyType))
        {
            MethodInfo getValidatorMethod = propertyType
                .GetMethod(nameof(IEditDto.GetValidatorType),
                    BindingFlags.Static | BindingFlags.Public)!;
            Type validatorType = (Type)getValidatorMethod.Invoke(null, null);
            if (validatorType != null)
            {
                object validator = Activator.CreateInstance(validatorType);

                MethodInfo validateMethod = typeof(DefaultValidatorExtensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == nameof(IValidator.Validate));
                var genericValidateMethod = validateMethod.MakeGenericMethod(propertyType);

                var optionsAction = GetValidationOptionsAction(propertyType, operation.path.Trim('/').ToPascalCase());
                object[] parameters = [validator, dto, optionsAction];
                ValidationResult result = (ValidationResult)genericValidateMethod.Invoke(null, parameters);
                foreach (var error in result.Errors)
                {
                    var failure = error;
                    failure.PropertyName = context.PropertyName;
                    //failure.PropertyName = string.Format("{0}.{1}", context.PropertyName, error.PropertyName);
                    context.AddFailure(failure);
                }
            }
        }
    }

    private static object GetValidationOptionsAction(Type propertyType, params string[] propertiesToValidate)
    {
        MethodInfo methodInfo = typeof(BaseJsonPatchValidatorExtension)
            .GetMethod(nameof(GetAction), BindingFlags.Static | BindingFlags.NonPublic)!;

        var genericMethod = methodInfo.MakeGenericMethod(propertyType);

        object[] parameters = [propertiesToValidate];

        return genericMethod.Invoke(null, parameters);
    }

    private static Action<ValidationStrategy<T>> GetAction<T>(string[] strings)
    {
        return op => op.IncludeProperties(strings);
    }
}

internal enum JsonPatchValidationErrorCode
{
    CanParsePathValidator,
    CanNotParseValueValidator,

}
