namespace Backend.DTOs.StudentExam
{
    public class StartSubmissionRequest
    {
        public int ExamId { get; set; }
        public int? PaperId { get; set; }
    }
}
