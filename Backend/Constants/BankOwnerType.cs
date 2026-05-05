namespace Backend.Constants;

/// <summary>Loại chủ sở hữu của QuestionBank.</summary>
public static class BankOwnerType
{
    public const byte Personal = 1;   // Ngân hàng cá nhân giáo viên
    public const byte Shared   = 2;   // Kho chung theo môn học

    public static bool IsValid(byte v) => v == Personal || v == Shared;
}
