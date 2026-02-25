namespace Backend.DTOs.StudentExam
{
    public class SubmitAnswerRequest
    {
        public int QuestionIndex { get; set; }
        public string ResponseText { get; set; } = string.Empty;
    }
}
