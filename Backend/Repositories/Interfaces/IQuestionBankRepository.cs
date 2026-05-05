using Backend.DTOs.QuestionBank;
using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface IQuestionBankRepository
{
    Task<(List<QuestionBankListItemDto> Items, int Total)> ListAsync(QuestionBankListQueryDto query, int currentUserId);
    Task<QuestionBankDetailDto?> GetDetailAsync(int bankId);
    Task<QuestionBank?> GetByIdAsync(int bankId);
    Task AddAsync(QuestionBank bank);
    Task AddManyAsync(IEnumerable<QuestionBank> banks);
    Task UpdateAsync(QuestionBank bank);
    Task<int> RepairSharedBanksAsync(int adminUserId, DateTime now);
    Task SaveChangesAsync();
}
