using AuthMetodology.API.CookieCreator;
using AuthMetodology.API.Interfaces;
using AuthMetodology.API.Middleware;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Application.Services;
using AuthMetodology.Infrastructure.Hashers;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Infrastructure.Providers;
using AuthMetodology.Persistence.Data;
using AuthMetodology.Persistence.Interfaces;
using AuthMetodology.Persistence.Profiles;
using AuthMetodology.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace AuthMetodology.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection(nameof(JWTOptions)));
            builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection(nameof(GoogleOptions)));

            builder.Services.AddApiAuthentication(builder.Services.BuildServiceProvider().GetRequiredService<IOptions<JWTOptions>>());

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJWTProvider, JWTProvider>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<ICookieCreator, CookieTokenCreator>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAutoMapper(typeof(UserProfile).Assembly);

            //Db
            var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
            builder.Services.AddDbContext<UserDBContext>(opt => opt.UseNpgsql(connectionString));

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
