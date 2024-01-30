using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.Validation;

internal static class ValidationExpressions
{
    public static IRuleBuilderOptions<T, string> MustBeExistingRole
        <T>(this IRuleBuilder<T, string> ruleBuilder, IAppDbContext context)
    {
        return ruleBuilder.Must((q, p) => BeExistingRole(p, context))
            .WithMessage((q, p) => $"'{p} is not existing role'")
            .WithErrorCode("NotExistingRoleValidator");
    }

    public static IRuleBuilderOptions<T, int?> MustBeExistingRoleOrNull
        <T>(this IRuleBuilder<T, int?> ruleBuilder, IAppDbContext context)
    {
        return ruleBuilder.Must((q, p) => BeExistingRoleOrNull(p, context))
            .WithMessage((q, p) => $"Role with id '{p}' does not exists")
            .WithErrorCode("NotExistingRoleValidator");
    }

    private static bool BeExistingRole(string role, IAppDbContext context)
    {
        return context.Roles.FirstOrDefault(r => r.Name.ToLower() == role.ToLower()) != null;
    }

    private static bool BeExistingRoleOrNull(int? roleId, IAppDbContext context)
    {
        if (roleId == null)
            return true;
        return context.Roles.FirstOrDefault(r => r.Id == roleId) != null;
    }

    //private static bool BeValidEmail(string email)
    //{
    //    Regex regex = new Regex(@"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$");
    //    return regex.IsMatch(email);
    //}

}
