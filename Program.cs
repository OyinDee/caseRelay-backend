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

try
{
    Console.WriteLine("Application starting...");
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure Kestrel for Azure App Service
    builder.WebHost.UseKestrel(options => {
        // Let Azure handle the port
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        options.Listen(System.Net.IPAddress.Any, int.Parse(port));
    })
    .UseContentRoot(Directory.GetCurrentDirectory());

    // Remove any explicit port configuration

    // Add Application Insights
    builder.Services.AddApplicationInsightsTelemetry();

    // Add environment fallback logic
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    builder.Environment.EnvironmentName = environment;
    Console.WriteLine($"Application running in {environment} environment");

    try
    {
        // Configuration priority: Azure App Settings > Environment > appsettings.json
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
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("SQL connection string not found");
        }

        // Replace password placeholder if provided in environment
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrEmpty(dbPassword))
        {
            connectionString = connectionString.Replace("{your_password}", dbPassword);
        }

        builder.Services.AddDbContext<ApplicationDbContext>(options => {
            options.UseSqlServer(connectionString);
            if (environment == "Development") {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // JWT Authentication
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRETKEY");
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT_SECRETKEY environment variable is not set");
        }

        // Use plain text key instead of Base64
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        Console.WriteLine($"JWT Key length in bits: {keyBytes.Length * 8}");

        // Update JWT configuration
        var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "https://caserelay.vercel.app";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Auth failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://caserelay.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
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

    // Add startup logging
    app.Logger.LogInformation("Application built successfully");

    try
    {
        // Apply migrations at startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check if database exists, if not, create it
            if (!await db.Database.CanConnectAsync())
            {
                Console.WriteLine("Database does not exist. Creating database...");
                await db.Database.EnsureCreatedAsync();
            }

            // Check for pending migrations
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations. Applying...");
                await db.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully.");
            }
                
            // Add seed data only after migrations
            try
            {
                await SeedData.InitializeAsync(scope.ServiceProvider);
                Console.WriteLine("Seed data applied successfully.");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"Warning - Seed data error: {seedEx.Message}");
                // Continue execution even if seeding fails
            }

            Console.WriteLine("Database setup completed successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Critical database error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // In production, you might want to log this but still allow the app to start
        if (environment == "Development")
        {
            throw; // Only throw in development
        }
    }

    // Add .NET 8 specific middleware
    app.UseAntiforgery();  // New in .NET 8

    // Middleware pipeline configuration
    if (app.Environment.IsDevelopment() || environment == "Development")
    {
        Console.WriteLine("Running in Development mode - enabling developer features");
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
        Console.WriteLine("Running in Production mode - using production error handling");
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
    app.UseDefaultFiles();  // Add this line before UseStaticFiles
    app.UseStaticFiles(new StaticFileOptions
    {
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream"
    }); // Serve static files

    // Fix middleware order - CORS must be before Authorization
    app.UseRouting();
    app.UseCors(); // Must come before Authentication
    app.UseAuthentication();
    app.UseAuthorization();

    // Keep only these endpoint definitions
    app.MapGet("/test", () =>
    {
        try
        {
            return Results.Ok(new { message = "Basic endpoint working" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test endpoint error: {ex.Message}");
            return Results.StatusCode(500);
        }
    }).AllowAnonymous();

    app.MapGet("/", () =>
    {
        try
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

    app.MapGet("/health", () =>
    {
        try
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
    }).AllowAnonymous();

    app.MapControllers();

    // Run the app without specifying port (let IIS handle it)
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during runtime: {ex.Message}");
    throw;
}
}
catch (Exception ex)
{
    Console.WriteLine($"Critical startup error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    File.WriteAllText("startup_error.log", ex.ToString());
    throw;
}
