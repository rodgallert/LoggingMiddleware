using LoggingMiddleware.Models;
using Serilog;
using System.Text.Json;

namespace LoggingMiddleware.Middleware
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string method = GetMethod(context);
            try
            {
                await _next(context);
            }
            catch (NotFoundException ex)
            {
                var response = context.Response;

                response.ContentType = "application/json";
                response.StatusCode = StatusCodes.Status404NotFound;

                Log.Information(ex.Message);
                await Log.CloseAndFlushAsync();

                await response.WriteAsync(JsonSerializer.Serialize(ex.Message));
            }
            catch (BadRequestException ex)
            {
                var response = context.Response;

                response.ContentType = "application/json";
                response.StatusCode = StatusCodes.Status400BadRequest;

                Log.Information(ex.Message);
                await Log.CloseAndFlushAsync();

                await response.WriteAsync(JsonSerializer.Serialize(ex.Message));
            }
            catch (Exception ex)
            {

                string userAgent = GetUserAgent(context);
                string controller = GetController(context);
                string action = GetAction(context);
                string ip = GetIp(context);
                string requestBody = GetRequestBody(context);
                string responseBody = GetResponseBody(context);

                Log.ForContext("UserAgent", userAgent)
                   .ForContext("Controller", controller)
                   .ForContext("Method", method)
                   .ForContext("Action", action)
                   .ForContext("Ip", ip)
                   .ForContext("RequestBody", requestBody)
                   .ForContext("ResponseBody", responseBody)
                   .Error(ex, ex.Message);

                await Log.CloseAndFlushAsync();

                var response = context.Response;

                response.ContentType = "application/json";
                response.StatusCode = StatusCodes.Status500InternalServerError;

                await response.WriteAsync(JsonSerializer.Serialize(ex.Message));
            }
        }

        private string GetUserAgent(HttpContext context)
        {
            return context.Request.Headers["User-Agent"].ToString();
        }

        private string GetController(HttpContext context)
        {
            return context.Request.RouteValues["controller"]?.ToString();
        }

        private string GetMethod(HttpContext context)
        {
            return context.Request.Method;
        }

        private string GetAction(HttpContext context)
        {
            return context.Request.RouteValues["action"]?.ToString();
        }

        private string GetIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress.ToString();
        }

        private string GetRequestBody(HttpContext context)
        {
            return context.Request.Body.ToString();
        }

        private string GetResponseBody(HttpContext context)
        {
            return context.Response.Body.ToString();
        }
    }
}
