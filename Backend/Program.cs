using Backend.Models;
using Backend.Repositories.Implements;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DbContext
            builder.Services.AddDbContext<MtcaSep490G26Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

            // Repos / services
            builder.Services.AddScoped<ICourseRepo, CourseRepo>();
            builder.Services.AddScoped<ICourseService, CourseService>();

            // Chapter repo/service registration
            builder.Services.AddScoped<IChapterRepo, ChapterRepo>();
            builder.Services.AddScoped<IChapterService, ChapterService>();

            // Authentication - example JWT (configure Authority/Audience as needed)
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // configure according to your identity provider
                options.Authority = builder.Configuration["Auth:Authority"]; // optional
                options.Audience = builder.Configuration["Auth:Audience"];
                options.RequireHttpsMetadata = true;
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            app.Run();
        }
    }
}
