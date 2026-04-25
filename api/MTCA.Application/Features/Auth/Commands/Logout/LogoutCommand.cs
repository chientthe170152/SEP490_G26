using MediatR;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string? RefreshToken) : IRequest<Result>;
