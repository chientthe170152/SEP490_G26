using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.QuestionBank;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class QuestionBankService(
    IQuestionBankRepository _repo,
    ISubjectRepository      _subjectRepo,
    ICurrentUserService     _currentUser,
    TimeProvider            _timeProvider) : IQuestionBankService
{
    public async Task<Result<List<QuestionBankListItemDto>>> ListAsync(QuestionBankListQueryDto query)
    {
        var (items, _) = await _repo.ListAsync(query, _currentUser.UserId);
        return items;
    }

    public async Task<Result<QuestionBankDetailDto>> GetByIdAsync(int bankId)
    {
        var bank = await _repo.GetByIdAsync(bankId);
        if (bank == null)
            return QuestionBankErrors.NotFound;

        // Visibility: Personal → only owner; Shared → any Teacher/Admin
        if (bank.OwnerType == BankOwnerType.Personal && bank.OwnerUserId != _currentUser.UserId)
            return QuestionBankErrors.NotFound; // do not leak existence

        var detail = await _repo.GetDetailAsync(bankId);
        return detail!;
    }

    public async Task<Result<QuestionBankDetailDto>> CreatePersonalBankAsync(CreatePersonalBankRequest request)
    {
        var subject = await _subjectRepo.GetByIdAsync(request.SubjectId);
        if (subject == null)
            return QuestionBankErrors.SubjectNotFound;

        if (subject.Status == SubjectStatus.Closed)
            return QuestionBankErrors.SubjectClosed;

        var now     = _timeProvider.GetUtcNow().UtcDateTime;
        var userId  = _currentUser.UserId;

        var bank = new QuestionBank
        {
            SubjectId       = request.SubjectId,
            Name            = request.Name.Trim(),
            Description     = request.Description?.Trim(),
            Purpose         = request.Purpose,
            OwnerType       = BankOwnerType.Personal,
            OwnerUserId     = userId,
            Status          = BankStatus.Active,
            CreatedByUserId = userId,
            CreatedAtUtc    = now,
            UpdatedByUserId = userId,
            UpdatedAtUtc    = now,
        };

        await _repo.AddAsync(bank);
        await _repo.SaveChangesAsync();

        var detail = await _repo.GetDetailAsync(bank.QuestionBankId);
        return detail!;
    }

    public async Task<Result<QuestionBankDetailDto>> UpdatePersonalBankAsync(int bankId, UpdatePersonalBankRequest request)
    {
        var bank = await _repo.GetByIdAsync(bankId);

        // Hide Shared Banks and non-existent ones behind the same 404
        if (bank == null || bank.OwnerType != BankOwnerType.Personal)
            return QuestionBankErrors.NotFound;

        if (bank.OwnerUserId != _currentUser.UserId)
            return QuestionBankErrors.NotOwned;

        if (bank.Status == BankStatus.Archived)
            return QuestionBankErrors.AlreadyArchived;

        var currentStamp = Convert.ToBase64String(bank.ConcurrencyStamp);
        if (currentStamp != request.ConcurrencyStamp)
            return QuestionBankErrors.ConcurrentUpdate;

        bank.Name            = request.Name.Trim();
        bank.Description     = request.Description?.Trim();
        bank.UpdatedByUserId = _currentUser.UserId;
        bank.UpdatedAtUtc    = _timeProvider.GetUtcNow().UtcDateTime;

        await _repo.UpdateAsync(bank);
        await _repo.SaveChangesAsync();

        var detail = await _repo.GetDetailAsync(bank.QuestionBankId);
        return detail!;
    }

    public async Task<Result> ArchivePersonalBankAsync(int bankId)
    {
        var bank = await _repo.GetByIdAsync(bankId);

        if (bank == null || bank.OwnerType != BankOwnerType.Personal)
            return QuestionBankErrors.NotFound;

        if (bank.OwnerUserId != _currentUser.UserId)
            return QuestionBankErrors.NotOwned;

        if (bank.Status == BankStatus.Archived)
            return QuestionBankErrors.AlreadyArchived;

        bank.Status          = BankStatus.Archived;
        bank.UpdatedByUserId = _currentUser.UserId;
        bank.UpdatedAtUtc    = _timeProvider.GetUtcNow().UtcDateTime;

        await _repo.UpdateAsync(bank);
        await _repo.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<int>> RepairSharedBanksAsync()
    {
        var now   = _timeProvider.GetUtcNow().UtcDateTime;
        var count = await _repo.RepairSharedBanksAsync(_currentUser.UserId, now);
        return count;
    }
}
