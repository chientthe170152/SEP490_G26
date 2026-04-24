using Microsoft.AspNetCore.Identity;

namespace MTCA.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool MustChangePassword { get; set; }

    public UserProfile? Profile { get; set; }
}
