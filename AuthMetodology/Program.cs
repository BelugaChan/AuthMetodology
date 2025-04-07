using Asp.Versioning;
using AuthMetodology.API.CookieCreator;
using AuthMetodology.API.Extensions;
using AuthMetodology.API.Interfaces;
using AuthMetodology.API.Middleware;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Application.Profiles.v1;
using AuthMetodology.Application.Services;
using AuthMetodology.Infrastructure.Hashers;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Infrastructure.Providers;
using AuthMetodology.Infrastructure.Services;
using AuthMetodology.Persistence.Data;
using AuthMetodology.Persistence.Interfaces;
using AuthMetodology.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
namespace AuthMetodology.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            //add API versioning
            builder.Services.AddVersioning();


            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API - V1", Version = "v1.0" })
            );

            builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection(nameof(JWTOptions)));
            builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection(nameof(GoogleOptions)));
            builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(nameof(RabbitMqOptions)));
            //builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(nameof(EmailOptions)));

            builder.Services.AddApiAuthentication(builder.Services.BuildServiceProvider().GetRequiredService<IOptions<JWTOptions>>());

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJWTProvider, JWTProvider>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<ICookieCreator, CookieTokenCreator>();
            builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
            builder.Services.AddScoped<IRedisService, RedisService>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAutoMapper(typeof(UserProfileV1).Assembly);

            //Db
            var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
            builder.Services.AddDbContext<UserDBContext>(opt => opt.UseNpgsql(connectionString));
            //Cache
            builder.Services.AddStackExchangeRedisCache(opt =>
            {
                var connection = builder.Configuration.GetConnectionString("Redis");
                opt.Configuration = connection;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.Run();
        }
    }
}
