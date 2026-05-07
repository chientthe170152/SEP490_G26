using System;
using System.Collections.Generic;

namespace Backend.Models;

/// <summary>
/// Ngân hàng câu hỏi — cá nhân (Personal) hoặc kho chung (Shared).
/// <list type="bullet">
///   <item>Personal: OwnerType=1, OwnerUserId=teacher. Giáo viên tạo qua API.</item>
///   <item>Shared: OwnerType=2, OwnerUserId=NULL. Tự sinh khi tạo Subject (SubjectService.CreateAsync).
///         Name theo công thức "Kho chung {Purpose} {Subject.Name} {Subject.Code}".</item>
/// </list>
/// </summary>
public partial class QuestionBank
{
    public int      QuestionBankId   { get; set; }
    public int      SubjectId        { get; set; }

    /// <summary>
    /// NOT NULL cho mọi loại. Shared Bank tự sinh theo công thức trong SubjectService.CreateAsync.
    /// Personal Bank do giáo viên đặt qua API.
    /// </summary>
    public string   Name             { get; set; } = null!;

    /// <summary>Mô tả tự do do giáo viên/admin nhập — tùy chọn.</summary>
    public string?  Description      { get; set; }

    /// <summary>1=Exam (Kiểm tra), 2=Practice (Luyện tập). Xem <see cref="Backend.Constants.BankPurpose"/>.</summary>
    public byte     Purpose          { get; set; }

    /// <summary>1=Personal, 2=Shared. Xem <see cref="Backend.Constants.BankOwnerType"/>.</summary>
    public byte     OwnerType        { get; set; }

    /// <summary>NOT NULL khi OwnerType=Personal; NULL khi OwnerType=Shared.</summary>
    public int?     OwnerUserId      { get; set; }

    /// <summary>1=Active, 0=Archived. Xem <see cref="Backend.Constants.BankStatus"/>.</summary>
    public int      Status           { get; set; }

    /// <summary>Shared Bank: = admin gọi CreateSubject. Personal Bank: = OwnerUserId.</summary>
    public int      CreatedByUserId  { get; set; }
    public DateTime CreatedAtUtc     { get; set; }

    /// <summary>Mặc định = CreatedByUserId lúc insert.</summary>
    public int      UpdatedByUserId  { get; set; }
    public DateTime UpdatedAtUtc     { get; set; }
    public byte[]   ConcurrencyStamp { get; set; } = null!;

    // ── Navigations ────────────────────────────────────────
    public virtual Subject  Subject       { get; set; } = null!;
    public virtual User?    Owner         { get; set; }
    public virtual User     CreatedByUser { get; set; } = null!;
    public virtual User     UpdatedByUser { get; set; } = null!;

    public virtual ICollection<Question>                 Questions               { get; set; } = new List<Question>();
    public virtual ICollection<QuestionPromotionRequest> SourcePromotionRequests { get; set; } = new List<QuestionPromotionRequest>();
    public virtual ICollection<QuestionPromotionRequest> TargetPromotionRequests { get; set; } = new List<QuestionPromotionRequest>();
}
