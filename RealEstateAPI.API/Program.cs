using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealEstateAPI.API.Authorization;
using RealEstateAPI.API.Filters;
using RealEstateAPI.API.Middleware;
using RealEstateAPI.Application.Mappings;
using RealEstateAPI.Application.Validators.Auth;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using RealEstateAPI.Infrastructure.Helpers;
using RealEstateAPI.Infrastructure.Repositories;
using RealEstateAPI.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting RealEstateAPI...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine("Logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 50_000_000,
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    });

    var jwtSection = builder.Configuration.GetSection("JwtSettings");

    if (!jwtSection.Exists())
        throw new Exception("JwtSettings section not found in configuration.");

    var jwtSecret = jwtSection["SecretKey"];
    var jwtIssuer = jwtSection["Issuer"];
    var jwtAudience = jwtSection["Audience"];
    var jwtExpiration = jwtSection["ExpirationInMinutes"];

    if (string.IsNullOrWhiteSpace(jwtSecret))
        throw new Exception("JwtSettings:SecretKey is missing.");

    if (string.IsNullOrWhiteSpace(jwtIssuer))
        throw new Exception("JwtSettings:Issuer is missing.");

    if (string.IsNullOrWhiteSpace(jwtAudience))
        throw new Exception("JwtSettings:Audience is missing.");

    if (string.IsNullOrWhiteSpace(jwtExpiration))
        throw new Exception("JwtSettings:ExpirationInMinutes is missing.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Default");

        options.UseSqlServer(
            connectionString,
            sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly("RealEstateAPI.Infrastructure");
                sqlServerOptions.CommandTimeout(60);
                sqlServerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            });

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddSingleton<JwtHelper>(_ =>
        new JwtHelper(
            secretKey: jwtSecret!,
            issuer: jwtIssuer!,
            audience: jwtAudience!,
            expirationInMinutes: int.Parse(jwtExpiration!)
        )
    );

    builder.Services.AddAutoMapper(config => config.AddProfile<MappingProfile>());

    builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

    builder.Services.AddScoped<IFileService>(provider =>
    {
        var config = provider.GetRequiredService<IConfiguration>();
        var uploadSettings = config.GetSection("FileUploadSettings");

        return new FileService(
            uploadPath: uploadSettings["PropertyImagesPath"]!,
            maxFileSizeInMB: long.Parse(uploadSettings["MaxFileSizeInMB"]!),
            allowedExtensions: uploadSettings.GetSection("AllowedExtensions").Get<string[]>()!
        );
    });

    builder.Services.AddScoped<IEmailService>(provider =>
    {
        var smtp = provider.GetRequiredService<IConfiguration>()
                           .GetSection("SmtpSettings");

        return new EmailService(
            host: smtp["Host"]!,
            port: int.Parse(smtp["Port"]!),
            username: smtp["Username"]!,
            password: smtp["Password"]!,
            fromEmail: smtp["FromEmail"]!,
            fromName: smtp["FromName"]!,
            enableSsl: bool.Parse(smtp["EnableSsl"]!)
        );
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret!)
            ),

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.Name = "XSRF-REQUEST-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    builder.Services.AddControllers(options =>
    {

        options.Filters.Add<ValidationFilter>();

    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? (int)retryAfter.TotalSeconds
                : (int?)null;

            if (retryAfterSeconds.HasValue)
            {
                context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.Value.ToString();
            }

            var body = JsonSerializer.Serialize(new
            {
                success = false,
                message = "Too many requests. Please slow down and try again later.",
                statusCode = StatusCodes.Status429TooManyRequests,
                timestamp = DateTime.UtcNow
            });

            await context.HttpContext.Response.WriteAsync(body, cancellationToken);
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var key = httpContext.User.Identity?.IsAuthenticated == true
                ? $"user:{httpContext.User.Identity!.Name}"
                : $"ip:{httpContext.Connection.RemoteIpAddress}";

            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        options.AddPolicy("AuthPolicy", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                $"auth:{httpContext.Connection.RemoteIpAddress}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "application/json",
            "application/problem+json"
        });
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        options.Level = CompressionLevel.Fastest);

    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("PropertiesCache", policy =>
            policy.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("*").Tag("properties"));

        options.AddPolicy("CategoriesCache", policy =>
            policy.Expire(TimeSpan.FromMinutes(10)).SetVaryByQuery("*").Tag("categories"));
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.CustomSchemaIds(type => type.FullName);

        options.SwaggerDoc("v1", new()
        {
            Title = "Real Estate API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Bearer {token}"
        });

        var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            { securityScheme, new string[] { } }
        };

        options.AddSecurityRequirement(securityRequirement);
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, elapsed, ex) => ex != null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
    });

    app.UseGlobalExceptionHandler();

    app.UseResponseCompression();

    app.UseSecurityHeaders();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors("AllowAll");

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseOutputCache();

    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminSeeder");
        await RealEstateAPI.Infrastructure.Data.AdminSeeder.SeedAsync(context, builder.Configuration, seedLogger);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RealEstateAPI terminated unexpectedly during startup");
}
finally
{
    Log.CloseAndFlush();
}
