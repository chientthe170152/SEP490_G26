using Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Services.Implements;
using Backend.Services.Interfaces;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Configure DbContext
            builder.Services.AddDbContext<MtcaSep490G26Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

            // Register Repositories
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IAuthRepository, Backend.Repositories.Implements.AuthRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IExamBlueprintRepository, Backend.Repositories.Implements.ExamBlueprintRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IStudentExamRepository, Backend.Repositories.Implements.StudentExamRepository>();

            // Register Services
            builder.Services.AddScoped<Backend.Services.Interfaces.IAuthService, Backend.Services.Implements.AuthService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IEmailService, Backend.Services.Implements.EmailService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IExamBlueprintService, Backend.Services.Implements.ExamBlueprintService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IStudentExamService, Backend.Services.Implements.StudentExamService>(); 
            builder.Services.AddScoped<Backend.Services.Interfaces.IAssignExamService, Backend.Services.Implements.AssignExamService>();

            // Add SignalR
            builder.Services.AddSignalR();

            // Add Memory Cache for OTP storage
            builder.Services.AddMemoryCache();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Configure JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
                };
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MTCA API", Version = "v1" });

                // Configure Swagger to use JWT Bearer
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                        },
                        new List<string>()
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<Backend.Hubs.ExamHub>("/examHub");

            app.Run();
        }
    }
}
