using MediatR;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Features.Auth.Commands.Refresh;

public record RefreshCommand(
    string RefreshToken,
    string? Ip,
    string? UserAgent) : IRequest<Result<RefreshResult>>;
