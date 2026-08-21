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
using DevelApp.RuntimePluggableClassFactory.FilePlugin;
using DevelApp.RuntimePluggableClassFactory.Security;
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
                RequireDigitalSignature = false, // Can be configured
                MaxAssemblySizeBytes = 10 * 1024 * 1024, // 10MB
                AllowedExtensions = new List<string> { ".dll" }
            };
            var securityValidator = new DefaultPluginSecurityValidator(securitySettings);

            // Setup plugin loader
            services.AddSingleton<IPluginSecurityValidator>(securityValidator);
            services.AddSingleton<IPluginLoader<IJobExecutor>>(new FilePluginLoader<IJobExecutor>(
                new Uri(pluginDirectory), securityValidator));
            services.AddSingleton<PluginClassFactory<IJobExecutor>>();

            // Also register typed plugin factory for type-safe job executors
            services.AddSingleton<DevelApp.RuntimePluggableClassFactory.TypedPluginClassFactory<IJobExecutor<MergeSortInput, MergeSortOutput>, MergeSortInput, MergeSortOutput>>();

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
                .AddCheck<QuartzHealthCheck>("Quartz Scheduler");

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
}
