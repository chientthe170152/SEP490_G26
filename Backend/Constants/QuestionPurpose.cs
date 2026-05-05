namespace Backend.Constants
{
    /// <summary>
    /// Mục đích câu hỏi gắn trực tiếp trên Question — đã chuyển sang <see cref="BankPurpose"/>.
    /// Câu hỏi kế thừa Purpose qua bank chứa nó (Question.QuestionBank.Purpose).
    /// </summary>
    [Obsolete("Use BankPurpose on QuestionBank instead. Will be removed in Phase 9.")]
    public static class QuestionPurpose
    {
        public const byte Exam = 1;
        public const byte Practice = 2;

        public static bool IsValid(byte purpose)
        {
            return purpose == Exam || purpose == Practice;
        }

        public static string GetLabel(byte purpose)
        {
            return purpose switch
            {
                Exam => "Kiểm tra",
                Practice => "Luyện tập",
                _ => "Không xác định"
            };
        }
    }
}
