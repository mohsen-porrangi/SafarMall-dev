using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace BuildingBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurrentUserService<TCurrentUserService>(this IServiceCollection services)
        where TCurrentUserService : class, ICurrentUserService
    {
        services.AddScoped<ICurrentUserService, TCurrentUserService>();
        return services;
    }
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthenticationOptions>(
                configuration.GetSection(AuthenticationOptions.Name));

        var jwtSettings = configuration
            .GetSection(AuthenticationOptions.Name)
            .Get<AuthenticationOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings!.Issuer,
                    ValidAudience = jwtSettings!.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings!.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(
                            "{\"error\": \"دسترسی غیرمجاز: توکن معتبر نیست یا وجود ندارد.\"}");
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(
                            "{\"error\": \"شما اجازه دسترسی به این منبع را ندارید.\"}");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
                policy.RequireAssertion(_ => true));
        });

        return services;
    }
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services, string title = "API", string version = "v1", string description = "", bool enableAuth = false)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description,
            });
            c.UseAllOfToExtendReferenceSchemas();
            c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
            //c.DocInclusionPredicate((docName, apiDesc) =>
            //{
            //    // بررسی اینکه response type مشکل نداشته باشه
            //    var returnType = apiDesc.SupportedResponseTypes.FirstOrDefault()?.Type;
            //    if (returnType == typeof(void) || returnType == null)
            //    {
            //        return false; 
            //    }
            //    return true;
            //});
            if (enableAuth)
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

            c.MapType<Guid?>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Nullable = true
            });
            c.SupportNonNullableReferenceTypes();
            c.UseAllOfForInheritance();
            c.UseOneOfForPolymorphism();
            c.SelectDiscriminatorNameUsing(type => type.Name);
            c.CustomSchemaIds(type =>
            {
                if (type.FullName == null)
                    return type.Name;
                return type.FullName.Replace("+", "_").Replace("`", "_");
            });
            c.EnableAnnotations();
            //c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "UserManagement.API.xml"));
        });

        return services;
    }
    public static IServiceCollection AddAllowAllCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
    public static IServiceCollection AddOpenApiWithJwt(this IServiceCollection services, string title = "API", string version = "v1", string description = "")
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }

    public static IServiceCollection AddExternalService<TService, TImplementation, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TService : class
        where TImplementation : class, TService
        where TOptions : class, IExternalServiceOptions
    {
        services.Configure<TOptions>(
            configuration.GetSection(sectionName));

        services.AddHttpClient<TService, TImplementation>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Timeout);
        });

        return services;
    }
    internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new();
            document.Components.SecuritySchemes = requirements;

            document.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new() { Reference = new() { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
            }
        };

            return Task.CompletedTask;
        }
    }


}
