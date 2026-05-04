using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Backend.Helpers;

namespace Backend.Services.Implements;

public class SubmissionService(
    ISubmissionRepository submissionRepo,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider,
    IMathGradingService mathGrading) : ISubmissionService
{
    public async Task<Result<SubmitExamResponse>> SubmitExamAsync(
        SubmitExamRequest request,
        CancellationToken ct = default)
    {
        var studentId = currentUserService.UserId;

        // ── 1. Tìm Submission đang InProgress ──────────────────────────
        var submission = await submissionRepo.GetActiveSubmissionAsync(
            request.ExamId!.Value, studentId, ct);

        if (submission == null)
            return SubmissionErrors.NotFound;

        if (submission.Status != SubmissionStatus.InProgress)
            return SubmissionErrors.AlreadySubmitted;

        var exam = submission.Paper?.Exam;
        if (exam == null)
            return SubmissionErrors.NotFound;

        // ── 2. Kiểm tra thời gian (TimeProvider) ──────────
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var startTime = submission.CreatedAtUtc;             // thời gian bắt đầu làm bài
        var deadline = startTime.AddMinutes(exam.Duration);  // hết giờ theo duration

        bool isLate = now > deadline || (exam.CloseAt.HasValue && now > exam.CloseAt.Value);

        if (isLate)
            return SubmissionErrors.Late;

        // ── 3. Validate QuestionAnswerIds thuộc Paper ────────────────────
        var validIds = await submissionRepo.GetValidQuestionAnswerIdsAsync(
            submission.PaperId, ct);

        if (request.StudentAnswers != null)
        {
            foreach (var sa in request.StudentAnswers)
            {
                if (sa.QuestionAnswerId.HasValue && !validIds.Contains(sa.QuestionAnswerId.Value))
                    return SubmissionErrors.InvalidAnswer;
            }
        }

        // ── 4. Xử lý StudentAnswers ────────────────────────────────────
        var incomingData = (request.StudentAnswers ?? [])
            .Where(sa => sa.QuestionAnswerId.HasValue)
            .Select(sa => (sa.QuestionAnswerId!.Value, sa.Response));

        StudentAnswerSyncHelper.SyncAnswers(
            submission.StudentAnswers, 
            incomingData, 
            submission.SubmissionId);

        // ── 5. Cập nhật Submission ──────────────────────────────────────
        submission.Status = request.Submit == true
            ? SubmissionStatus.Submitted
            : SubmissionStatus.InProgress;
        submission.UpdatedAtUtc = now;

        await submissionRepo.SaveChangesAsync(ct);

        // ── 6. Chấm điểm nếu đã nộp bài ────────────────────────────────
        decimal? finalTotalPoints = null;
        int? finalCorrectCount = null;
        int? finalTotalQuestions = null;

        if (submission.Status == SubmissionStatus.Submitted)
        {
            // Load đầy đủ graph để chấm (Questions → QuestionAnswers → BlankInputs)
            var fullSubmission = await submissionRepo.GetSubmissionForGradingAsync(
                submission.SubmissionId, ct);
            if (fullSubmission != null)
            {
                var (correctCount, totalQuestions, totalPoints) = await AnalyticsHelper.GradeSubmissionAsync(
                    fullSubmission, mathGrading);
                submission.TotalPoints = totalPoints;
                await submissionRepo.SaveChangesAsync(ct);

                // Gán lại để trả về response nếu rules cho phép
                finalTotalPoints = totalPoints;
                finalCorrectCount = correctCount;
                finalTotalQuestions = totalQuestions;
            }
        }

        // Kiểm tra nguyên tắc xem điểm
        // ShowScore: 0 = Không hiển thị, 1 = Ngay khi nộp, 2 = Khi kết thúc kỳ thi
        bool canShowScore = exam.ShowScore == 1 || (exam.ShowScore == 2 && exam.CloseAt.HasValue && now >= exam.CloseAt.Value);
        
        // ── 7. Return response ──────────────────────────────────────────
        return new SubmitExamResponse(
            submission.SubmissionId,
            now,
            isLate,
            canShowScore ? finalTotalPoints : null,
            canShowScore ? finalCorrectCount : null,
            canShowScore ? finalTotalQuestions : null,
            exam.ShowScore,
            exam.ShowAnswer,
            exam.AnswerTimingMode
        );
    }
}
