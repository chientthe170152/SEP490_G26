using Microsoft.AspNetCore.Identity;

namespace MTCA.Domain.Identity;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
