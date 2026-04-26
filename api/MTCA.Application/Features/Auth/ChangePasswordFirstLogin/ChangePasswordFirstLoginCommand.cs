using MediatR;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Features.Auth.ChangePasswordFirstLogin;

public sealed record ChangePasswordFirstLoginCommand(
    string CurrentPassword,
    string NewPassword,
    string? Ip,
    string? UserAgent) : IRequest<Result<ChangePasswordFirstLoginResult>>;
