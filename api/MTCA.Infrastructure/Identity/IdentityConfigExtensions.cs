using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MTCA.Application.Common.Constants;
using MTCA.Domain.Identity;
using MTCA.Infrastructure.Persistence;

namespace MTCA.Infrastructure.Identity;

public static class IdentityConfigExtensions
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public static IServiceCollection AddMtcaIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(opts =>
        {
            opts.Password.RequiredLength = PasswordPolicy.MinimumLength;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequireDigit = false;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireLowercase = false;

            opts.Lockout.MaxFailedAccessAttempts = MaxFailedAttempts;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(LockoutMinutes);
            opts.Lockout.AllowedForNewUsers = true;

            opts.User.RequireUniqueEmail = false;
            opts.SignIn.RequireConfirmedEmail = false;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager()
        .AddUserManager<UserManager<ApplicationUser>>()
        .AddPasswordValidator<MtcaPasswordValidator>();

        return services;
    }
}
