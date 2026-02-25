namespace Backend.DTOs.StudentExam
{
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string ContentLatex { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public string? Answer { get; set; }
    }
}
