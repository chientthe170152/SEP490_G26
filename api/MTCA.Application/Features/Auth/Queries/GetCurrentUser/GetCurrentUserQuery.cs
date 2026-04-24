using MediatR;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserResult>>;
