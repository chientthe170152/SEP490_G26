using Backend.Models;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddDbContext<MtcaSep490G26Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("value")));
            builder.Services.AddScoped<IAssignExamService, AssignExamService>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendDev", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:5291",
                            "https://localhost:7109",
                            "http://localhost:39743",
                            "https://localhost:44339")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

            app.UseCors("FrontendDev");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
