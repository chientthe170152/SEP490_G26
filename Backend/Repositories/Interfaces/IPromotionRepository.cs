using Backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories.Interfaces;

public interface IPromotionRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task InsertRequestAsync(QuestionPromotionRequest request);
    Task InsertItemAsync(QuestionPromotionRequestItem item);
    Task<bool> HasPendingItemAsync(int questionId);
    Task<QuestionPromotionRequest?> GetRequestWithItemsForUpdateAsync(int requestId);
    Task<int> SaveChangesAsync();
    
    Task<(List<QuestionPromotionRequest> items, int total)> GetRequestsAsync(
        int? requestedByUserId, 
        int? status, 
        int? subjectId, 
        int? teacherId, 
        int page, 
        int pageSize);

    Task<QuestionPromotionRequest?> GetRequestDetailAsync(int requestId);
}
