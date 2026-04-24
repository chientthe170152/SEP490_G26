namespace MTCA.Application.Common.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
