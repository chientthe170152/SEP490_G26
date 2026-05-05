using Backend.Common.Models;
using Backend.DTOs.Promotion;

namespace Backend.Services.Interfaces;

public interface IPromotionService
{
    Task<Result<PromotionRequestDetailDto>> CreateAsync(CreatePromotionRequest req);
    Task<Result> FinalizeAsync(int requestId, FinalizePromotionRequest req);
    Task<Result> WithdrawAsync(int requestId);
    Task<Result<PromotionRequestPage>> ListMineAsync(int? status, int page, int pageSize);
    Task<Result<PromotionRequestPage>> ListAdminAsync(int? status, int? subjectId, int? teacherId, int page, int pageSize);
    Task<Result<PromotionRequestDetailDto>> GetDetailAsync(int requestId, bool isAdmin);
}
