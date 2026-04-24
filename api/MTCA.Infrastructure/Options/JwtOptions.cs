using System.ComponentModel.DataAnnotations;

namespace MTCA.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Required]
    [MinLength(64)]
    public string Key { get; set; } = default!;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 120;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;
}
