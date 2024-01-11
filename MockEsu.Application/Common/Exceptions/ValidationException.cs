using FluentValidation.Results;
using MockEsu.Application.Common.BaseRequests;

namespace MockEsu.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(IDictionary<string, string[]> failures)
        : this()
    {
        Errors = failures;
    }

    public IDictionary<string, string[]> Errors { get; }
}
