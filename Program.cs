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

var builder = WebApplication.CreateBuilder(args);

try
{
    // Configuration - Adding support for environment variables and appsettings.json
    builder.Configuration.AddEnvironmentVariables();  // Ensures environment variables are added
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    // Adding services to the container
    builder.Services.AddControllers();

    // Cloudinary Service
    var cloudinaryConfig = builder.Configuration.GetSection("Cloudinary");
    builder.Services.AddSingleton<ICloudinaryService>(sp => new CloudinaryService(
        cloudinaryConfig["CloudName"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME"),
        cloudinaryConfig["ApiKey"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY"),
        cloudinaryConfig["ApiSecret"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
    ));

    // DbContext for SQL Server
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRING_DEFAULT_CONNECTION")));

    // JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRETKEY")
                ))
            };
        });

    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ICaseService, CaseService>();
    builder.Services.AddScoped<IUserService, UserService>(); // Add IUserService
    builder.Services.AddScoped<EmailService>(); // Register EmailService

    // CORS Policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllOrigins", builder =>
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader());
    });

    // Swagger Setup
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "CaseRelayAPI",
            Version = "v1"
        });
    });
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred during application startup: {ex.Message}");
    throw;
}

var app = builder.Build();

try
{

    // Configure the HTTP request pipeline
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

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseStaticFiles();
    app.UseHttpsRedirection();
    app.UseCors("AllowAllOrigins");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map Controllers
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred while configuring the HTTP request pipeline: {ex.Message}");
    throw;
}
