using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MTCA.Domain.Identity;
using MTCA.Infrastructure.Persistence;

namespace MTCA.Infrastructure.Identity;

public static class IdentityConfigExtensions
{
    public static IServiceCollection AddMtcaIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(opts =>
        {
            opts.Password.RequiredLength = 10;
            opts.Password.RequireNonAlphanumeric = true;
            opts.Password.RequireDigit = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireLowercase = true;

            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            opts.Lockout.AllowedForNewUsers = true;

            opts.User.RequireUniqueEmail = false;
            opts.SignIn.RequireConfirmedEmail = false;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager()
        .AddUserManager<UserManager<ApplicationUser>>();

        return services;
    }
}
