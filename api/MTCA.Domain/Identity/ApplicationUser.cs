using Microsoft.AspNetCore.Identity;

namespace MTCA.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public UserProfile? Profile { get; set; }
}
