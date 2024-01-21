using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using MockEsu.Application.Common.BaseRequests;

namespace MockEsu.Web.Structure
{
    /// <summary>
    /// Class for returning result as Json
    /// </summary>
    public static class JsonResponseClass
    {
        /// <summary>
        /// Returns response as Json or throws exception from response
        /// </summary>
        /// <param name="response">Response model</param>
        /// <returns>Json content result</returns>
        /// <exception cref="Exception">Exception from response</exception>
        public static ContentResult ToJsonResponse(this BaseResponse response)
        {
            var result = new ContentResult();
            if (response.GetException() != null)
                throw response.GetException() ?? new Exception(response.GetMessage());
            var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            result.Content = JsonConvert.SerializeObject(response, settings);
            result.ContentType = "application/json";
            return result;
        }
    }
}
