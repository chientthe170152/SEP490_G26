using MTCA.Domain.Identity.Enums;
using MTCA.Domain.MasterData;

namespace MTCA.Domain.Identity;

public class UserProfile
{
    public string UserId { get; set; } = default!;
    public string StudentCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public UserProfileStatus Status { get; set; } = UserProfileStatus.ACTIVE;
    public string? Nickname { get; set; }
    public int? NicknameChangedInSemesterId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = default!;
    public Semester? NicknameChangedInSemester { get; set; }
}
