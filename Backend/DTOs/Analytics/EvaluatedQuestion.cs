using System.Collections.Generic;

namespace Backend.DTOs.Analytics;

public record EvaluatedQuestion(
    int QuestionId,
    string QuestionContent,
    string QuestionType,
    int ChapterId,
    string ChapterName,
    int Difficulty,
    bool IsCorrect,
    List<EvaluatedOption> Options
);
