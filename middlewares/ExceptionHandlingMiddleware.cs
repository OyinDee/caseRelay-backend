using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CaseRelayAPI.Middlewares
{
  public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access. Path: {Path}, QueryString: {QueryString}", httpContext.Request.Path, httpContext.Request.QueryString);
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsync("Unauthorized access.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Bad request. Path: {Path}, QueryString: {QueryString}", httpContext.Request.Path, httpContext.Request.QueryString);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsync("Bad request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request. Path: {Path}, QueryString: {QueryString}", httpContext.Request.Path, httpContext.Request.QueryString);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync("An unexpected error occurred.");
        }
    }
}

}
