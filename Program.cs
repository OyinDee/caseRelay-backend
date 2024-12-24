using CaseRelayAPI.Data;
using CaseRelayAPI.Middlewares;
using CaseRelayAPI.Models;
using CaseRelayAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using BCrypt.Net;
using Microsoft.OpenApi.Models;
using DotNetEnv;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

try
{
    // Configuration: Load environment variables BEFORE appsettings.json
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    // Replace environment variables in configuration
    var port = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
    builder.Configuration["EmailSettings:Port"] = port;

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAntiforgery();  // New in .NET 8

    // Cloudinary Service
    var cloudinaryConfig = builder.Configuration.GetSection("Cloudinary");
    var cloudName = cloudinaryConfig["CloudName"] ?? throw new InvalidOperationException("Cloudinary CloudName is not configured");
    var apiKey = cloudinaryConfig["ApiKey"] ?? throw new InvalidOperationException("Cloudinary ApiKey is not configured");
    var apiSecret = cloudinaryConfig["ApiSecret"] ?? throw new InvalidOperationException("Cloudinary ApiSecret is not configured");

    builder.Services.AddSingleton<ICloudinaryService>(sp => new CloudinaryService(
        cloudName,
        apiKey,
        apiSecret
    ));

    // Email Service
    builder.Services.AddSingleton<EmailService>();

    // Database Context
    var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    // Log connection details (remove in production)
    Console.WriteLine($"Attempting to connect to: {dbServer}:{dbPort}");
    
    var connectionString = $"Server={dbServer},{dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;Encrypt=True;Connection Timeout=30;";
    
    builder.Services.AddDbContext<ApplicationDbContext>(options => {
        options.UseSqlServer(connectionString);
        options.EnableSensitiveDataLogging(); // Remove in production
        options.EnableDetailedErrors(); // Remove in production
    });

    // JWT Authentication
    var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRETKEY");
    if (string.IsNullOrEmpty(jwtKey))
    {
        throw new InvalidOperationException("JWT_SECRETKEY environment variable is not set");
    }

    // Decode the Base64 key instead of plain UTF8
    var keyBytes = Convert.FromBase64String(jwtKey);
    Console.WriteLine($"Base64-decoded JWT Key length in bits: {keyBytes.Length * 8}");

    if (keyBytes.Length * 8 < 256)
    {
        throw new InvalidOperationException("JWT secret key must be at least 256 bits (32 characters) long");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,  // Validates the 'iss' claim
                ValidateAudience = true,  // Validates the 'aud' claim
                ValidateLifetime = true,  // Checks if token is expired
                ValidateIssuerSigningKey = true,  // Validates the signature
                ValidIssuer = builder.Configuration["JWT_ISSUER"],
                ValidAudience = builder.Configuration["JWT_AUDIENCE"],
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.Zero  // Optional: removes the default 5 minute clock skew
            };

            // Optional: Add events for debugging token validation
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                }
            };
        });

    // CORS Policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllOrigins", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });

    // Swagger setup
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CaseRelayAPI", Version = "v1" });
        c.OperationFilter<FileUploadOperationFilter>();
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath); // Enable XML comments
    });

    // Other Services
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ICaseService, CaseService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during setup: {ex.Message}");
    throw;
}

var app = builder.Build();

try
{
    // Apply migrations at startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        
        // Add seed data
        await SeedData.InitializeAsync(scope.ServiceProvider);

        // Check database connection
        try
        {
            db.Database.CanConnect();
            Console.WriteLine("Database connection successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            throw;
        }
    }

    // Add .NET 8 specific middleware
    app.UseAntiforgery();  // New in .NET 8

    // Middleware pipeline configuration
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CaseRelayAPI v1");
            c.RoutePrefix = string.Empty; // Makes Swagger UI accessible at the root
        });
    }
    else
    {
        app.UseExceptionHandler(configure: options => 
        {
            options.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "An error occurred." });
            });
        });
        app.UseHsts(); // Enforce strict transport security
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>(); // Custom middleware for exception handling
    app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
    app.UseStaticFiles(); // Serve static files

    // Fix middleware order
    app.UseRouting();

    // Add this after app.UseRouting()
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { 
                error = "An error occurred", 
                detail = ex.Message 
            });
        }
    });

    app.UseCors("AllowAllOrigins"); // Move CORS here
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGet("/", () =>
        {
            try  // Add error handling
            {
                return Results.Ok(new
                {
                    Api = "CaseRelay API",
                    Version = "1.0",
                    Status = "Running"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Root endpoint error: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        endpoints.MapGet("/health", () =>
        {
            try  // Add error handling
            {
                return Results.Ok(new
                {
                    Status = "Healthy",
                    Environment = app.Environment.EnvironmentName,
                    Time = DateTime.UtcNow,
                    Assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check error: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        endpoints.MapGet("/test", () =>
        {
            return Results.Ok(new { message = "Basic endpoint working" });
        });

        endpoints.MapControllers();
    });

    // Run the app
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during runtime: {ex.Message}");
    throw;
}
