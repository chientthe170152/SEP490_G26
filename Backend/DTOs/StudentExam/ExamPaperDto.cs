namespace Backend.DTOs.StudentExam
{
    public class ExamPaperDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxAttempts { get; set; }
        public int Duration { get; set; }
        public int PaperId { get; set; }
        public int Code { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
