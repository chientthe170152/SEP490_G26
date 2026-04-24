using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Domain.Common;
using MTCA.Domain.Common.Exceptions;

namespace MTCA.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor(ICurrentUserService currentUser, TimeProvider time)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        FillAudit(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        FillAudit(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void FillAudit(DbContextEventData eventData)
    {
        var ctx = eventData.Context;
        if (ctx is null) return;

        var now = time.GetUtcNow().UtcDateTime;

        foreach (var entry in ctx.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedById = currentUser.UserId
                        ?? throw new InvariantViolationException(
                            "CreatedById required when inserting IAuditable entity");
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedById = currentUser.UserId;
                    break;
            }
        }
    }
}
