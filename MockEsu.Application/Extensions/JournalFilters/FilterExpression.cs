﻿using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.JournalFilters
{
    /// <summary>
    /// Class representing a filtering expression
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    public record FilterExpression
    {
        /// <summary>
        /// DTO key
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Source endpoint key
        /// </summary>
        public string? EndPoint { get; set; }

        /// <summary>
        /// Type of expression
        /// </summary>
        public ExpressionType ExpressionType { get; set; }

        /// <summary>
        /// Filter value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Factory constructor
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestintaion"></typeparam>
        /// <param name="filter"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static FilterExpression Initialize<TSource, TDestintaion>(string filter, IConfigurationProvider provider)
            where TSource : BaseEntity
            where TDestintaion : BaseDto
        {
            var f = new FilterExpression();
            if (filter.Contains("!:"))
            {
                f.Key = ToPascalCase(filter.Substring(0, filter.IndexOf("!:")));
                f.EndPoint = BaseDto.GetSource<TSource, TDestintaion>(provider, f.Key);
                f.Value = filter.Substring(filter.IndexOf("!:") + 2);
                f.ExpressionType = ExpressionType.Exclude;
            }
            else if (filter.Contains(':'))
            {
                f.Key = ToPascalCase(filter.Substring(0, filter.IndexOf(':')));
                f.EndPoint = BaseDto.GetSource<TSource, TDestintaion>(provider, f.Key);
                f.Value = filter.Substring(filter.IndexOf(':') + 1);
                f.ExpressionType = ExpressionType.Include;
            }
            else
                f.ExpressionType = ExpressionType.Undefined;
            return f;
        }

        /// <summary>
        /// Converts a string to pascal case
        /// </summary>
        /// <param name="value">input string</param>
        /// <returns>String in pascal case</returns>
        private static string ToPascalCase(string value)
        {
            if (value.Length <= 1)
                return value.ToUpper();
            return $"{value[0].ToString().ToUpper()}{value.Substring(1)}";
        }
    }
    public enum ExpressionType
    {
        Include, Exclude, Undefined
    }

}