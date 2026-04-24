using MediatR;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? Ip,
    string? UserAgent) : IRequest<Result<LoginResult>>;
