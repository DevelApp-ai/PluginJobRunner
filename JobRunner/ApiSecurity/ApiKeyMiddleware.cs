using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JobRunner.ApiSecurity
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEYNAME = "ApiKey";
        private const string APIKEY_ENV_VAR = "JOBRUNNER_API_KEY";
        
        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get API key from header
            if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Api Key was not provided.");
                return;
            }

            // Get API key from environment variable (more secure than appsettings)
            var appSettings = context.RequestServices.GetRequiredService<IConfiguration>();
            var logger = context.RequestServices.GetRequiredService<ILogger<ApiKeyMiddleware>>();
            
            // Try environment variable first, then fall back to configuration
            var apiKey = Environment.GetEnvironmentVariable(APIKEY_ENV_VAR);
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = appSettings.GetValue<string>(APIKEYNAME);
            }
            
            // Validate API key
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("API key is not configured. Set {EnvVar} environment variable or {ConfigKey} in configuration.", 
                    APIKEY_ENV_VAR, APIKEYNAME);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Server configuration error.");
                return;
            }

            if (!apiKey.Equals(extractedApiKey, StringComparison.Ordinal))
            {
                logger.LogWarning("Unauthorized access attempt with invalid API key");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await _next(context);
        }
    }
}
