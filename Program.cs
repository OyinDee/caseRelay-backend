using CaseRelayAPI.Data;
using CaseRelayAPI.Middlewares;
using CaseRelayAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using DotNetEnv;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

try
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    var port = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
    builder.Configuration["EmailSettings:Port"] = port;

    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAntiforgery();

    var cloudinaryConfig = builder.Configuration.GetSection("Cloudinary");
    var cloudName = cloudinaryConfig["CloudName"] ?? throw new InvalidOperationException("Cloudinary CloudName is not configured");
    var apiKey = cloudinaryConfig["ApiKey"] ?? throw new InvalidOperationException("Cloudinary ApiKey is not configured");
    var apiSecret = cloudinaryConfig["ApiSecret"] ?? throw new InvalidOperationException("Cloudinary ApiSecret is not configured");

    builder.Services.AddSingleton<ICloudinaryService>(sp => new CloudinaryService(
        cloudName,
        apiKey,
        apiSecret
    ));

    builder.Services.AddSingleton<EmailService>();

    var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    var connectionString = $"Server={dbServer},{dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;Encrypt=True;Connection Timeout=30;";
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    });

    var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRETKEY");
    if (string.IsNullOrEmpty(jwtKey)) throw new InvalidOperationException("JWT_SECRETKEY environment variable is not set");

    var keyBytes = Convert.FromBase64String(jwtKey);
    if (keyBytes.Length * 8 < 256) throw new InvalidOperationException("JWT secret key must be at least 256 bits (32 characters) long");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JWT_ISSUER"],
                ValidAudience = builder.Configuration["JWT_AUDIENCE"],
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllOrigins", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CaseRelayAPI", Version = "v1" });
        c.OperationFilter<FileUploadOperationFilter>();
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

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
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        db.Database.CanConnect();
    }

    app.UseAntiforgery();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CaseRelayAPI v1");
            c.RoutePrefix = string.Empty;
        });
    }
    else
    {
        app.UseExceptionHandler(options => 
        {
            options.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "An error occurred." });
            });
        });
        app.UseHsts();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors("AllowAllOrigins");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new
    {
        Status = "Healthy",
        Environment = app.Environment.EnvironmentName,
        Time = DateTime.UtcNow,
        Assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
    }));

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during runtime: {ex.Message}");
    throw;
}
