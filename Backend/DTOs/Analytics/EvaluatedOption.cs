namespace Backend.DTOs.Analytics;

public record EvaluatedOption(
    int QuestionAnswerId,
    string Content,
    string? StudentResponse,
    bool IsSelected,
    bool? IsCorrect,
    string? CorrectAnswer
);
