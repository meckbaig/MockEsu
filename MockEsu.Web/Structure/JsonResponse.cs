using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using MockEsu.Application.Common.BaseRequests;

namespace MockEsu.Web.Structure
{
    public static class JsonResponseClass
    {
        //public static readonly CustomExceptionHandler _exceptionHandler = new CustomExceptionHandler();
        //public static HttpContext _httpContext;

        public static ContentResult ToJsonResponseAsync(this BaseResponse response)
        {
            var result = new ContentResult();
            if (response.GetException() != null)
                throw response.GetException() ?? new Exception(response.GetMessage());
                //await _exceptionHandler.TryHandleAsync(_httpContext, response.Exception ?? new Exception(), new CancellationToken());
            var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            result.Content = JsonConvert.SerializeObject(response, settings);
            result.ContentType = "application/json";
            return result;
        }

        //public static void InitCustomExceptionHandler(HttpContext httpContext)
        //{
        //    _httpContext = httpContext;
        //}
    }
}
