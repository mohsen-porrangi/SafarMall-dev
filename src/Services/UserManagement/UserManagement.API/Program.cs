using BuildingBlocks.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using UserManagement.API.Infrastructure.Configuration;
using UserManagement.API.Infrastructure.Data;
using UserManagement.API.Infrastructure.Middleware;

namespace UserManagement.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var assembly = typeof(Program).Assembly;

            // Configure Services
            // JWT Authentication
            builder.Services.AddJwtAuthentication(builder.Configuration);
            ConfigureApplicationServices(builder, assembly);
            // CORS
            builder.Services.AddAllowAllCors();
            ConfigureLogging(builder);

            builder.Services.AddEndpointsApiExplorer();
            // Swagger
          //   builder.Services.AddSwaggerWithJwt(title: "User Management API", version: "v1", description: "API برای مدیریت کاربران سیستم", enableAuth: true);
            builder.Services.AddOpenApiWithJwt();

            var app = builder.Build();

            // Configure Pipeline
            ConfigurePipeline(app);

            // Initialize Database
            InitializeDatabase(app);

            app.Run();
        }   

        private static void ConfigureApplicationServices(WebApplicationBuilder builder, System.Reflection.Assembly assembly)
        {
            // Health Checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<UserDbContext>("userManagement");

            // Custom Services
            builder.Services.ConfigureSqlServer(builder.Configuration);
            builder.Services.ConfigureMediatR(builder.Configuration);
            builder.Services.ConfigureService(builder.Configuration);

            // Validation and API
            builder.Services.AddValidatorsFromAssembly(assembly);
            builder.Services.AddCarter();

            // Token Service
            builder.Services.AddScoped<ITokenService, JwtTokenService>();
            builder.Services.AddHttpContextAccessor();

            // Exception Handling
            builder.Services.AddExceptionHandler<ErrorHandlerMiddleware>();
            builder.Services.AddProblemDetails();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // CORS 
            app.UseCors("AllowAllOrigins");

            // Exception Handling
            app.UseExceptionHandler();
            app.UseStatusCodePages();

    
            //if (app.Environment.IsDevelopment())
            //{
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "User Management API";
                    options.Theme = ScalarTheme.Kepler;
                    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
                });
           // }



            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Custom Middleware
            app.UseMiddleware<PermissionMiddleware>();

            // API Routes
            app.MapCarter();

            // Health Checks
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }

        private static void InitializeDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            DbInitializer.Seed(db);
        }
    }
}