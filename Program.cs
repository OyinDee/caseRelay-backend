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
        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
        
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

        // Replace password from environment variable
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrEmpty(dbPassword))
        {
            connectionString = connectionString.Replace("{your_password}", dbPassword);
        }

        Console.WriteLine("Attempting database connection...");
        builder.Services.AddDbContext<ApplicationDbContext>(options => 
            options.UseSqlServer(connectionString)
        );

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

void SeedDatabase(ApplicationDbContext context)
{
    var users = new[]
    {
        new User
        {
            PoliceId = "P12345",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "123-456-7890",
            PasscodeHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "Officer",
            Department = "Homicide",
            BadgeNumber = "B123",
            Rank = "Sergeant",
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            PoliceId = "P67890",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Phone = "098-765-4321",
            PasscodeHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "Officer",
            Department = "Fraud",
            BadgeNumber = "B456",
            Rank = "Lieutenant",
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        }
    };

    foreach (var user in users)
    {
        if (!context.Users.Any(u => u.PoliceId == user.PoliceId))
        {
            context.Users.Add(user);
        }
    }

    var cases = new[]
    {
        new Case { Title = "Burglary", Description = "A burglary case", Status = "Open", AssignedOfficerId = "P12345", Severity = "High", CreatedBy = 1 },
        new Case { Title = "Fraud", Description = "A fraud case", Status = "Closed", AssignedOfficerId = "P67890", Severity = "Medium", CreatedBy = 2, IsClosed = true, ResolvedAt = DateTime.UtcNow },
        new Case { Title = "Assault", Description = "An assault case", Status = "Open", AssignedOfficerId = "P12345", Severity = "High", CreatedBy = 1 },
        new Case { Title = "Theft", Description = "A theft case", Status = "Closed", AssignedOfficerId = "P67890", Severity = "Low", CreatedBy = 2, IsClosed = true, ResolvedAt = DateTime.UtcNow },
        new Case { Title = "Vandalism", Description = "A vandalism case", Status = "Open", AssignedOfficerId = "P12345", Severity = "Medium", CreatedBy = 1 },
        new Case { Title = "Cybercrime", Description = "A cybercrime case", Status = "Closed", AssignedOfficerId = "P67890", Severity = "High", CreatedBy = 2, IsClosed = true, ResolvedAt = DateTime.UtcNow },
        new Case { Title = "Domestic Violence", Description = "A domestic violence case", Status = "Open", AssignedOfficerId = "P12345", Severity = "High", CreatedBy = 1 },
        new Case { Title = "Drug Trafficking", Description = "A drug trafficking case", Status = "Closed", AssignedOfficerId = "P67890", Severity = "High", CreatedBy = 2, IsClosed = true, ResolvedAt = DateTime.UtcNow },
        new Case { Title = "Kidnapping", Description = "A kidnapping case", Status = "Open", AssignedOfficerId = "P12345", Severity = "High", CreatedBy = 1 },
        new Case { Title = "Arson", Description = "An arson case", Status = "Closed", AssignedOfficerId = "P67890", Severity = "High", CreatedBy = 2, IsClosed = true, ResolvedAt = DateTime.UtcNow }
    };

    foreach (var caseItem in cases)
    {
        if (!context.Cases.Any(c => c.Title == caseItem.Title && c.Description == caseItem.Description))
        {
            context.Cases.Add(caseItem);
        }
    }

    context.SaveChanges();
}
