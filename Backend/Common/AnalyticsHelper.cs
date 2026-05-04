using Backend.Constants;
using Backend.DTOs.Analytics;
using Backend.Models;
using Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backend.Common;

/// <summary>
/// Helper methods cho module Analytics: kiểm tra đúng/sai, tính median, phân bố điểm, sinh đề xuất.
/// </summary>
public static class AnalyticsHelper
{
    /// <summary>
    /// Chấm điểm đồng bộ — chỉ so sánh chuỗi.
    /// Dùng khi không có IMathGradingService.
    /// </summary>
    public static bool CheckIsCorrect(QuestionAnswer qa, StudentAnswer sa)
    {
        if (!string.IsNullOrEmpty(qa.CorrectAnswer) && !string.IsNullOrEmpty(sa.Response))
            return qa.CorrectAnswer.Trim().Equals(sa.Response.Trim(), StringComparison.OrdinalIgnoreCase);

        if (qa.IsCorrect.HasValue)
            return qa.IsCorrect.Value && !string.IsNullOrEmpty(sa.Response);

        return false;
    }

    /// <summary>
    /// Chấm điểm bất đồng bộ — đối với FillInBlank toán sẽ gọi pynum API.
    /// Câu MCQ / TrueFalse / blank văn bản thuần sẽ fallback về so sánh chuỗi.
    /// </summary>
    public static async Task<bool> CheckIsCorrectAsync(
        QuestionAnswer qa, StudentAnswer sa, string questionType, IMathGradingService? mathGrading)
    {
        // MCQ / TrueFalse → sync path
        if (questionType != QuestionType.FillBlank || mathGrading == null)
            return CheckIsCorrect(qa, sa);

        // FillInBlank with CorrectAnswer → check if it's a math expression
        if (!string.IsNullOrEmpty(qa.CorrectAnswer) && !string.IsNullOrEmpty(sa.Response))
        {
            var expected = qa.CorrectAnswer.Trim();
            var student = sa.Response.Trim();

            // Quick exact match first
            if (expected.Equals(student, StringComparison.OrdinalIgnoreCase))
                return true;

            // Strict Validation: Kiểm tra "Giới hạn nhập liệu" (Regex) từ giáo viên
            if (qa.BlankInputs != null && qa.BlankInputs.Any(bi => !string.IsNullOrEmpty(bi.InputType?.Regex)))
            {
                bool matchesRegex = false;
                foreach (var bi in qa.BlankInputs)
                {
                    var regexStr = bi.InputType?.Regex;
                    if (!string.IsNullOrEmpty(regexStr))
                    {
                        try 
                        {
                            var pattern = regexStr;
                            if (!pattern.StartsWith("^")) pattern = "^" + pattern;
                            if (!pattern.EndsWith("$")) pattern = pattern + "$";
                            
                            if (System.Text.RegularExpressions.Regex.IsMatch(student, pattern))
                            {
                                matchesRegex = true;
                                break;
                            }
                        }
                        catch 
                        {
                            // Bỏ qua nếu Regex trong DB lỗi
                        }
                    }
                }

                if (!matchesRegex)
                {
                    // Nếu không khớp với bất kỳ định dạng nào được cho phép -> Tính sai luôn
                    return false;
                }
            }

            // Check if this blank uses math input types (has BlankInputs with GroupType containing math-related keywords)
            bool isMathInput = qa.BlankInputs?.Any(bi =>
                bi.InputType?.GroupType != null &&
                !bi.InputType.GroupType.Equals("Chữ cái", StringComparison.OrdinalIgnoreCase)) ?? false;

            if (isMathInput)
            {
                // Delegate to pynum API for symbolic math comparison
                return await mathGrading.IsEquivalentAsync(expected, student);
            }

            // Text-only blank → case-insensitive string comparison
            return false;
        }

        // IsCorrect-based (MCQ style in FillInBlank)
        if (qa.IsCorrect.HasValue)
            return qa.IsCorrect.Value && !string.IsNullOrEmpty(sa.Response);

        return false;
    }

    /// <summary>
    /// Chấm điểm bất đồng bộ với tích hợp pynum cho FillInBlank toán.
    /// </summary>
    public static async Task<(int CorrectCount, int TotalQuestions, decimal TotalPoints)> GradeSubmissionAsync(
        Submission submission, IMathGradingService? mathGrading)
    {
        var paper = submission.Paper;
        if (paper == null || paper.Questions == null) return (0, 0, 0);

        var evaluatedQuestions = await EvaluateSubmissionAsync(
            paper.Questions.DistinctBy(q => q.QuestionId), submission.StudentAnswers, mathGrading);
        
        int correctCount = evaluatedQuestions.Count(q => q.IsCorrect);
        int totalQuestions = evaluatedQuestions.Count;

        decimal totalPoints = totalQuestions > 0
            ? Math.Round((decimal)correctCount / totalQuestions * 10, 3)
            : 0;

        return (correctCount, totalQuestions, totalPoints);
    }

    /// <summary>
    /// Đánh giá bất đồng bộ với tích hợp pynum — dùng Task.WhenAll để chấm song song.
    /// </summary>
    public static async Task<List<EvaluatedQuestion>> EvaluateSubmissionAsync(
        IEnumerable<Question> questions, IEnumerable<StudentAnswer> studentAnswers, IMathGradingService? mathGrading)
    {
        var questionList = questions.ToList();
        var saDict = studentAnswers.ToDictionary(a => a.QuestionAnswerId, a => a);

        // Pre-evaluate all grading tasks in parallel
        var gradingTasks = questionList
            .SelectMany(q => q.QuestionAnswers.Select(qa => new { q, qa }))
            .Select(async x =>
            {
                saDict.TryGetValue(x.qa.QuestionAnswerId, out var sa);
                bool isCorrect;
                if (sa != null)
                    isCorrect = await CheckIsCorrectAsync(x.qa, sa, x.q.QuestionType, mathGrading);
                else
                    isCorrect = false;
                return new { x.q.QuestionId, x.qa.QuestionAnswerId, sa, isCorrect, x.qa, NeedCorrect = x.qa.IsCorrect == true && sa == null };
            })
            .ToList();

        var gradingResults = await Task.WhenAll(gradingTasks);

        // Group results back per question
        var resultsByQuestion = gradingResults.GroupBy(r => r.QuestionId);
        var result = new List<EvaluatedQuestion>();

        foreach (var question in questionList)
        {
            var qResults = resultsByQuestion.FirstOrDefault(g => g.Key == question.QuestionId);
            bool questionCorrect = true;
            var options = new List<EvaluatedOption>();

            if (qResults != null)
            {
                foreach (var r in qResults)
                {
                    if (r.sa != null && !r.isCorrect)
                        questionCorrect = false;
                    else if (r.NeedCorrect)
                        questionCorrect = false;

                    options.Add(new EvaluatedOption(
                        r.qa.QuestionAnswerId,
                        r.qa.Content,
                        r.sa?.Response,
                        r.sa != null,
                        r.qa.IsCorrect,
                        r.qa.CorrectAnswer
                    ));
                }
            }

            result.Add(new EvaluatedQuestion(
                question.QuestionId,
                question.QuestionContent,
                question.QuestionType,
                question.Chapter?.ChapterId ?? 0,
                question.Chapter?.Name ?? "N/A",
                question.Difficulty,
                questionCorrect,
                options
            ));
        }

        return result;
    }

    public static decimal GetMedian(List<decimal> sorted)
    {
        int count = sorted.Count;
        if (count == 0) return 0;
        if (count % 2 == 0)
            return Math.Round((sorted[count / 2 - 1] + sorted[count / 2]) / 2, 2);
        return sorted[count / 2];
    }

    public static Dictionary<string, int> BuildScoreDistribution(List<decimal> scores)
    {
        var dist = new Dictionary<string, int>
        {
            ["0-1"] = 0,  ["1-2"] = 0,  ["2-3"] = 0,  ["3-4"] = 0,  ["4-5"] = 0,
            ["5-6"] = 0,  ["6-7"] = 0,  ["7-8"] = 0,  ["8-9"] = 0,  ["9-10"] = 0
        };

        foreach (var score in scores)
        {
            var bucket = score switch
            {
                < 1 => "0-1",
                < 2 => "1-2",
                < 3 => "2-3",
                < 4 => "3-4",
                < 5 => "4-5",
                < 6 => "5-6",
                < 7 => "6-7",
                < 8 => "7-8",
                < 9 => "8-9",
                _ => "9-10"
            };
            dist[bucket]++;
        }
        return dist;
    }

    /// <summary>
    /// Phiên bản bất đồng bộ — dùng pynum cho FillInBlank toán.
    /// </summary>
    public static async Task<AnswerResult?> MapStudentAnswerAsync(
        StudentAnswer sa, Dictionary<int, Question> questionDict, IMathGradingService? mathGrading)
    {
        var qa = sa.QuestionAnswer;
        if (qa == null) return null;

        var question = qa.Question;
        if (question == null)
        {
            if (questionDict.TryGetValue(qa.QuestionId, out var dictQuestion)) question = dictQuestion;
        }

        if (question == null) return null;

        var isCorrect = await CheckIsCorrectAsync(qa, sa, question.QuestionType, mathGrading);

        return new AnswerResult(
            question.QuestionId,
            question.QuestionContent,
            question.Chapter?.ChapterId ?? 0,
            question.Chapter?.Name ?? "N/A",
            question.Difficulty,
            isCorrect);
    }

}
