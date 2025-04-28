using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AuthMetodology.API.Extensions
{
    public static class ApiExtensions
    {
        public static void AddApiAuthentication(
            this IServiceCollection services,
            IOptions<JWTOptions> options)
        {
            var value = options.Value;
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                { //будет осуществляться проверка, есть ли в headers токен
                    options.TokenValidationParameters = new TokenValidationParameters() 
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(value.SecretKey))
                    };

                    options.Events = new JwtBearerEvents() 
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["access"];

                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins("http://mydomen.com")
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)))
                .AddPolicy("BearerOnly", policy => { policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);policy.RequireAuthenticatedUser(); });
        }
    }
}
