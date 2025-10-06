using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace ABCRetailers.Functions.Helpers
{
    public static class HttpJson
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task<T> ReadJsonAsync<T>(HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }

        public static async Task<HttpResponseData> WriteJsonAsync<T>(HttpRequestData req, T data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await response.WriteStringAsync(json);
            return response;
        }

        public static async Task<HttpResponseData> WriteErrorAsync(HttpRequestData req, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            var json = JsonSerializer.Serialize(new { error = message }, JsonOptions);
            await response.WriteStringAsync(json);
            return response;
        }
    }
}

