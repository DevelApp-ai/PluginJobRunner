using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.Async;
using DevelApp.RuntimePluggableClassFactory.Caching;
using DevelApp.RuntimePluggableClassFactory.FilePlugin;
using DevelApp.RuntimePluggableClassFactory.Resilience;
using DevelApp.RuntimePluggableClassFactory.Security;
using DevelApp.RuntimePluggableClassFactory.SemanticVersioning;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JobExecutorModel;
using Serilog;
using Microsoft.Data.SqlClient;
using System.Text.Json.Serialization;
using JobRunner.ApiSecurity;
using System.IO;
using Quartz;
using JobRunner.HealthChecks;
using JobRunner.Validation;
using Microsoft.AspNetCore.RateLimiting;
using Quartz.Impl;
using Quartz.Spi;
using JobRunner.Quartz;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobRunner
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            // Add API versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-API-Version"));
            });

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddValidatorsFromAssemblyContaining<PostJobRequestValidator>();

            #region Rate Limiting

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                });
            });

            #endregion

            #region Plugin Integration

            // Configure plugin directory from configuration or environment
            var pluginDirectory = Configuration.GetValue<string>("PluginDirectory") 
                ?? Environment.GetEnvironmentVariable("JOBRUNNER_PLUGIN_DIRECTORY")
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            // Ensure plugin directory exists
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            // Setup plugin security validator
            var securitySettings = new PluginSecuritySettings
            {
                RequireDigitalSignature = Configuration.GetValue<bool>("PluginSecurity:RequireDigitalSignature") || false,
                MaxAssemblySizeBytes = Configuration.GetValue<int>("PluginSecurity:MaxAssemblySizeBytes") || (10 * 1024 * 1024),
                AllowedExtensions = Configuration.GetValue<List<string>>("PluginSecurity:AllowedExtensions") 
                    ?? new List<string> { ".dll" }
            };
            var securityValidator = new DefaultPluginSecurityValidator(securitySettings);

            // ========================================================================
            // Basic Plugin Loader and Factory
            // ========================================================================
            
            // File plugin loader - the foundation
            var filePluginLoader = new FilePluginLoader<IJobExecutor>(
                new Uri(pluginDirectory), 
                securityValidator);
            
            services.AddSingleton<IPluginLoader<IJobExecutor>>(filePluginLoader);
            services.AddSingleton<IPluginSecurityValidator>(securityValidator);

            // ========================================================================
            // Async Plugin Loader - for non-blocking plugin loading
            // ========================================================================
            
            var asyncPluginLoader = new AsyncPluginLoader<IJobExecutor>(filePluginLoader);
            services.AddSingleton<IAsyncPluginLoader<IJobExecutor>>(asyncPluginLoader);

            // Async factory - wraps the async loader
            services.AddSingleton<AsyncPluginClassFactory<IJobExecutor>>(provider =>
                new AsyncPluginClassFactory<IJobExecutor>(asyncPluginLoader));

            // ========================================================================
            // Cached Plugin Factory - for performance optimization
            // ========================================================================
            
            var cacheSettings = Configuration.GetSection("PluginCache");
            var poolSize = cacheSettings.GetValue<int>("PoolSize") || 10;
            var enableCaching = cacheSettings.GetValue<bool>("EnableInstanceCaching") || true;
            
            services.AddSingleton<CachedPluginClassFactory<IJobExecutor>>(provider =>
                new CachedPluginClassFactory<IJobExecutor>(
                    filePluginLoader,
                    retainOldVersions: 2,
                    poolSize: poolSize,
                    enableInstanceCaching: enableCaching));

            // ========================================================================
            // Resilient Plugin Factory - for fault tolerance
            // ========================================================================
            
            var resilienceSettings = Configuration.GetSection("PluginResilience");
            var circuitBreakerOptions = new CircuitBreakerOptions
            {
                FailureThreshold = resilienceSettings.GetValue<int>("CircuitBreaker:FailureThreshold") || 5,
                ResetTimeout = TimeSpan.FromSeconds(resilienceSettings.GetValue<int>("CircuitBreaker:ResetTimeoutSeconds") || 30),
                HalfOpenTestTimeout = TimeSpan.FromSeconds(resilienceSettings.GetValue<int>("CircuitBreaker:HalfOpenTestTimeoutSeconds") || 10)
            };
            
            var retryPolicyOptions = new RetryPolicyOptions
            {
                MaxRetries = resilienceSettings.GetValue<int>("RetryPolicy:MaxRetries") || 3,
                InitialDelay = TimeSpan.FromMilliseconds(resilienceSettings.GetValue<int>("RetryPolicy:InitialDelayMs") || 100),
                MaxDelay = TimeSpan.FromSeconds(resilienceSettings.GetValue<int>("RetryPolicy:MaxDelaySeconds") || 30),
                BackoffMultiplier = resilienceSettings.GetValue<double>("RetryPolicy:BackoffMultiplier") || 2.0
            };
            
            services.AddSingleton<ResilientPluginClassFactory<IJobExecutor>>(provider =>
                new ResilientPluginClassFactory<IJobExecutor>(
                    filePluginLoader,
                    retainOldVersions: 2,
                    circuitBreakerOptions: circuitBreakerOptions,
                    retryPolicyOptions: retryPolicyOptions));

            // ========================================================================
            // Typed Plugin Factories - for type-safe execution
            // ========================================================================
            
            // Register typed factory for type-safe job executors
            services.AddSingleton<TypedPluginClassFactory<IJobExecutor<MergeSortInput, MergeSortOutput>, MergeSortInput, MergeSortOutput>>();
            
            // Also register the basic plugin factory for backward compatibility
            services.AddSingleton<PluginClassFactory<IJobExecutor>>(provider =>
                provider.GetRequiredService<ResilientPluginClassFactory<IJobExecutor>>());

            #endregion

            #region Swagger

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobRunner API", Version = "v1" });
                c.SwaggerDoc("v2", new OpenApiInfo { Title = "JobRunner API", Version = "v2" });

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "JobRunner.xml");
                c.IncludeXmlComments(filePath);

                c.AddSecurityDefinition("ApiKey",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Enter the ApiKey for access",
                        Name = "ApiKey",
                        Type = SecuritySchemeType.ApiKey
                    });
                c.OperationFilter<ApiKeyRequirementsOperationFilter>();

                // Add API versioning support
                c.OperationFilter<SwaggerDefaultValues>();
            });

            #endregion

            #region Database

            var connectionString = Configuration.GetConnectionString("JobDatabase");
            services.AddDbContext<JobDbContext>(options =>
                    options.UseSqlServer(connectionString));

            #endregion

            #region Quartz

            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

            // Debug start
            services.AddSingleton<HelloWorldJob>();
            services.AddSingleton(new JobSchedule(
                jobType: typeof(HelloWorldJob),
                cronExpression: "0/5 * * * * ?")); // run every 5 seconds

            #region Health Checks

            services.AddHealthChecks()
                .AddDbContextCheck<JobDbContext>("Database")
                .AddCheck<DatabaseHealthCheck>("Database Extended")
                .AddCheck<QuartzHealthCheck>("Quartz Scheduler")
                .AddCheck<PluginLoaderHealthCheck>("Plugin Loader");

            #endregion
            // Debug end

            services.AddHostedService<QuartzHostedService>();

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, JobDbContext jobContext, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                // Make sure the database is created and up to date. 
                if (jobContext.Database.CanConnect())
                {
                    jobContext.Database.Migrate();
                }
                else
                {
                    // Create database and apply migrations on it
                    jobContext.Database.EnsureCreated();
                }

                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", 
                            $"JobRunner API {description.GroupName}");
                    }
                });
            }
            else
            {
                // In test and prod the DevOps pipeline should handle migration
                jobContext.Database.CanConnect();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<ApiKeyMiddleware>();
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }

    /// <summary>
    /// Operation filter to add API version information to Swagger
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            operation.Deprecated |= apiDescription.IsDeprecated();

            foreach (var responseType in apiDescription.SupportedResponseTypes)
            {
                operation.Responses.TryAdd(responseType.StatusCode.ToString(), 
                    new OpenApiResponse { Description = responseType.ApiResponseFormatter?.Description });
            }

            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // Add API version parameter
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "api-version",
                In = ParameterLocation.Query,
                Description = "API version",
                Required = false,
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }

    /// <summary>
    /// Interface for async plugin loader (for DI)
    /// </summary>
    public interface IAsyncPluginLoader<T> : IPluginLoader<T> where T : IPluginClass
    {
        bool IsInitialLoadComplete { get; }
        Task InitialLoadTask { get; }
    }
}
