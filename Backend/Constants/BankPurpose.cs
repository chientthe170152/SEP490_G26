namespace Backend.Constants;

/// <summary>
/// Mục đích sử dụng của QuestionBank. Câu hỏi kế thừa Purpose qua bank chứa nó.
/// </summary>
public static class BankPurpose
{
    public const byte Exam     = 1;   // Kiểm tra
    public const byte Practice = 2;   // Luyện tập

    public static bool IsValid(byte v) => v == Exam || v == Practice;

    public static string GetLabel(byte v) => v switch
    {
        Exam     => "Kiểm tra",
        Practice => "Luyện tập",
        _        => "Không xác định"
    };
}
