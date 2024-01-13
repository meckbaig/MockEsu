using FluentValidation.Results;
using MockEsu.Application.Common.Exceptions;
using System.Text.Json.Serialization;

namespace MockEsu.Web.Structure.CustomProblemDetails
{
    public class CustomValidationProblemDetails : ValidationProblemDetails
    {
        public CustomValidationProblemDetails() { }
    
        public CustomValidationProblemDetails(IDictionary<string, ErrorItem[]> errors)
        {
            Errors = errors;
        }

        [JsonPropertyName("errors")]
        public new IDictionary<string, ErrorItem[]> Errors { get; }
    }
}
