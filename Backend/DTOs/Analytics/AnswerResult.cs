namespace Backend.DTOs.Analytics;

public record AnswerResult(
    int QuestionId,
    string QuestionContent,
    int ChapterId,
    string ChapterName,
    int Difficulty,
    bool IsCorrect
);
