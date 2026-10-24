using Microsoft.EntityFrameworkCore;
using AuthorizationService.Data;
using AuthorizationService.Middleware;
using SharedAuth.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace AuthorizationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Allow cross-origin requests for development (needed so other services' Swagger UI can call the auth token endpoint)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DefaultCors", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
            // Configure Swagger/OpenAPI via extension method
            builder.Services.AddCustomSwagger(builder.Configuration);
            builder.Services.AddHttpContextAccessor();

            // Register shared authentication helpers (Auth client, token provider and handler)
            builder.Services.AddSharedAuth(builder.Configuration);
            // Register DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configure JWT authentication
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key");
            if (!string.IsNullOrWhiteSpace(key))
            {
                var keyBytes = Encoding.UTF8.GetBytes(key);
                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection.GetValue<string>("Issuer"),
                        ValidateAudience = true,
                        ValidAudience = jwtSection.GetValue<string>("Audience"),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });
            }
            var app = builder.Build();

            // Serve static files (needed for custom swagger UI script)
            app.UseStaticFiles();

            // Enable CORS early so the token endpoint can be called from other origins (e.g., microservice Swagger UI)
            app.UseCors("DefaultCors");

            // Provide a dedicated endpoint to serve the custom-auth.js in case static file routing or caching prevents the file from being delivered
            try
            {
                var customPath = System.IO.Path.Combine(app.Environment.ContentRootPath, "wwwroot", "swagger-ui", "custom-auth.js");
                if (System.IO.File.Exists(customPath))
                {
                    app.MapGet("/swagger-ui/custom-auth.js", async ctx =>
                    {
                        ctx.Response.ContentType = "application/javascript";
                        var content = await System.IO.File.ReadAllTextAsync(customPath);
                        await ctx.Response.WriteAsync(content);
                    });
                }
            }
            catch {
                // ignore; this is best-effort for dev convenience
            }

            // Use centralized swagger setup (serves static script and injects inline fallback)
            app.UseCustomSwagger();


            // Global exception handling middleware
            app.UseMiddleware<ExceptionMiddleware>();

            // Enable authentication
            app.UseAuthentication();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
