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

var builder = WebApplication.CreateBuilder(args);

try
{
    // Configuration: Load environment variables and appsettings.json
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    // Add services to the container
    builder.Services.AddControllers();

    // Cloudinary Service
    var cloudinaryConfig = builder.Configuration.GetSection("Cloudinary");
    builder.Services.AddSingleton<ICloudinaryService>(sp => new CloudinaryService(
        cloudinaryConfig["CloudName"],
        cloudinaryConfig["ApiKey"],
        cloudinaryConfig["ApiSecret"]
    ));

    // Database Context
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
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
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts(); // Enforce strict transport security
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>(); // Custom middleware for exception handling
    app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
    app.UseStaticFiles(); // Serve static files
    app.UseCors("AllowAllOrigins"); // Enable CORS
    app.UseAuthentication(); // Enable JWT Authentication
    app.UseAuthorization(); // Enable Authorization

    // Map Controllers
    app.MapControllers();

    // Run the app
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during runtime: {ex.Message}");
    throw;
}
