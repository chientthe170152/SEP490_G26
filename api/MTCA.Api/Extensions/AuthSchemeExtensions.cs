using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MTCA.Api.Authorization;
using MTCA.Api.Options;
using MTCA.Application.Common.Constants;
using MTCA.Infrastructure.Options;

namespace MTCA.Api.Extensions;

public static class AuthSchemeExtensions
{
    private const int JwtClockSkewSeconds = 30;

    public static IServiceCollection AddMtcaAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuthCookieOptions>()
            .Bind(configuration.GetSection(AuthCookieOptions.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromSeconds(JwtClockSkewSeconds)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrEmpty(context.Request.Headers.Authorization))
                        {
                            return Task.CompletedTask;
                        }

                        var cookieOpts = context.HttpContext.RequestServices
                            .GetRequiredService<IOptions<AuthCookieOptions>>().Value;

                        var token = context.Request.Cookies[cookieOpts.Name];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AllowPasswordChange,
                p => p.RequireAuthenticatedUser());

            var passwordFresh = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(AuthClaims.MustChangePassword, AuthClaims.False)
                .Build();

            options.DefaultPolicy = passwordFresh;
            options.FallbackPolicy = passwordFresh;
        });

        return services;
    }
}
