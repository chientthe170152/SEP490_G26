using Backend.Common.Models;
using Backend.DTOs.QuestionBank;

namespace Backend.Services.Interfaces;

public interface IQuestionBankService
{
    Task<Result<List<QuestionBankListItemDto>>> ListAsync(QuestionBankListQueryDto query);
    Task<Result<QuestionBankDetailDto>> GetByIdAsync(int bankId);
    Task<Result<QuestionBankDetailDto>> CreatePersonalBankAsync(CreatePersonalBankRequest request);
    Task<Result<QuestionBankDetailDto>> UpdatePersonalBankAsync(int bankId, UpdatePersonalBankRequest request);
    Task<Result> ArchivePersonalBankAsync(int bankId);
    Task<Result<int>> RepairSharedBanksAsync();
}
