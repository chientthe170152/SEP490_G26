# MTCA.Domain — Conventions

Quy tắc cho tầng Domain. Áp dụng cho mọi PR đụng `MTCA.Domain/**/*.cs` và `MTCA.Infrastructure/Persistence/Configurations/**/*.cs`.

## 1. Pattern: anemic POCO + state transition method

Domain hiện tại dùng **POCO entity** với public setter. KHÔNG dùng factory method, KHÔNG private ctor, KHÔNG private setter. Lý do:

- DTO + FluentValidation ở Application layer đã đảm bảo dữ liệu đến Domain là **dữ liệu sạch** (đúng định dạng, range, required field, business rule cấp input).
- Domain chỉ chịu trách nhiệm enforce **Critical Business Rules (CBR)** — bất biến của domain, không phụ thuộc use case. Chủ yếu là **state machine rules** và **cross-field invariant**.
- Default entry state, workflow orchestration là **Application Business Rules (ABR)** — đặt ở handler, không ở Domain.

Tham chiếu phân loại CBR vs ABR: Robert C. Martin — *Clean Architecture*, ch. 16, 20.

## 2. Skeleton entity hiện tại

```csharp
public class Exam : AggregateRoot<int>
{
    public string Name { get; set; } = default!;
    public int SubjectId { get; set; }
    public ExamStatus Status { get; set; }              // KHÔNG default
    public ResultVisibilityTiming ResultVisibilityTiming { get; set; }
    public bool ShowTotalScore { get; set; }            // KHÔNG default — handler set tường minh
    public bool ShowCorrectAnswers { get; set; }
    public int VariantCount { get; set; }
    // ...

    public Subject Subject { get; set; } = default!;    // null-forgiving cho EF nav
    public ICollection<ExamVariant> Variants { get; set; } = new List<ExamVariant>();   // collection init technical
}
```

→ Caller (handler/seeder) tạo bằng `new Entity { ... }`, set TẤT CẢ business field tường minh.

## 3. Quy tắc default value — RULE CHÍNH

| Loại field | Default ở Domain field initializer? | Default ở EF `HasDefaultValue` / `HasDefaultValueSql`? |
|---|---|---|
| **Mọi business field** (enum, bool, decimal, int, double, string, ...) | ❌ KHÔNG | ❌ KHÔNG |
| **Audit / DB-managed** (`CreatedAt`, `UpdatedAt`, `Timestamp`, `StartedAt` cho PracticeSession) | ❌ KHÔNG | ✅ `HasDefaultValueSql("SYSUTCDATETIME()")` |
| **Concurrency** (`RowVersion`) | — | ✅ `IsRowVersion()` |
| **Technical C#** (`= default!` cho non-nullable nav, `= new List<T>()` cho collection) | ✅ GIỮ — không phải business default, là kỹ thuật C# tránh null warning | — |

**Lý do**:
- Mọi business field handler PHẢI set tường minh khi tạo entity → buộc developer suy nghĩ entry state cho từng use case.
- Tách rõ CBR (Domain) khỏi ABR (Application). Default entry state là ABR — thuộc handler.
- Linh hoạt khi feature mới có entry state khác — chỉ thêm handler, không phải override default ở Domain.
- Đọc handler thấy ngay entry state, không phải tra Domain hoặc EF Configuration.

**Ngoại lệ DB-managed**: `CreatedAt SYSUTCDATETIME()` thuộc infrastructure (DB tự đặt thời gian), không phải application logic — đặt ở EF Configuration là đúng. Không phải "business default".

## 4. State transition method — chỗ duy nhất Domain validate

Chỉ thêm method khi PR feature có handler thực sự dùng. Pattern:

```csharp
public class Exam : AggregateRoot<int>
{
    // ... properties ...

    public void Activate()
    {
        if (Status != ExamStatus.DRAFT)
            throw new InvariantViolationException($"Cannot activate exam in status {Status}");
        Status = ExamStatus.ACTIVE;
    }
}
```

**Quy tắc trong state transition**:
- Validate **state machine rule** (đang ở state nào, được phép chuyển sang state nào).
- Validate **cross-field invariant** không thể validate ở DTO (vd `CK_ExamSession_MinDurationPolicy`).
- KHÔNG validate input format/range/required (Validator đã làm).
- Throw `InvariantViolationException` khi vi phạm — handler bắt và return `Result.Failure(409 Conflict)`.

## 5. YAGNI — khi nào thêm method

| Tình huống | Hành động |
|---|---|
| Handler tạo entity mới | `new Entity { ... }` trực tiếp, set TẤT CẢ business field tường minh. KHÔNG cần factory. |
| Handler đổi state (Status enum) | Thêm `public void {Verb}()` validate state machine. |
| Handler update field thường (rename, edit) | Đổi qua public setter. Nếu có invariant cross-field, gói vào method. |
| Handler thêm child collection | `parent.Children.Add(new Child { ... })`. Nếu cần invariant ("chỉ add khi parent ở Draft") → thêm `parent.AddChild(...)` method. |
| Worker bulk update | Public setter trực tiếp. |

Mỗi method thêm phải có **≥1 caller trong cùng PR** — không speculative.

## 6. Verb naming khi thêm state transition

Theo ubiquitous language feature:

| Aggregate | State transition đề xuất | Feature ID |
|---|---|---|
| `Exam` | `Activate`, `Archive` | T.12 |
| `ExamBlueprintVersion` | `Activate(activatedAt)`, `Archive(archivedAt)` | §9.4 |
| `ExamSession` | `Activate`, `Complete`, `Cancel(reason, byId, at)` | T.14, T.18 |
| `Submission` | `MarkDisconnected`, `AllowContinue`, `Submit(score, at)`, `Heartbeat(at)` | S.4-9, T.15-17 |
| `QuestionProposal` | `Approve(reviewerId)`, `Reject(reviewerId, reason)`, `RequestRevision(reviewerId, reason)` | T.6, H.1 |
| `UserProfile` | `Suspend`, `Reactivate`, `UpdateNickname(nickname, semesterId)` | A.2, S.18 |
| `Classroom` | `EnableJoinCode(code)`, `DisableJoinCode`, `Archive`, `RemoveStudent(id, reason)` | T.7 |
| `ClassroomStudent` | `Approve(approvedAt)` | T.7 |
| `PracticeSession` | `End(score, totalQuestions)` | S.15 |
| `Question` | `UpdateEmpiricalDifficulty(p)` | T.21 (worker) |

## 7. Audit fields

`CreatedAt`, `CreatedById`, `UpdatedAt`, `UpdatedById` được `AuditSaveChangesInterceptor` fill tự động cho entity implement `IAuditable`. Handler KHÔNG set tay. Yêu cầu: HttpContext có authenticated user khi insert — nếu không, interceptor throw `InvariantViolationException`.

`CreatedAt`, `Timestamp`, `StartedAt` (PracticeSession) có DB DEFAULT `SYSUTCDATETIME()` làm safety net cho raw SQL/seed insert.

## 8. Concurrency

Entity implement `IConcurrencyAware` (có RowVersion). EF detect conflict → throw `DbUpdateConcurrencyException` ở `SaveChangesAsync`. Handler bắt và return `Result.Failure(409 Conflict)`.

## 9. Schema.dbml policy

`schema.dbml` KHÔNG đặc tả `[default: 'XXX']` cho cột business (default được handler set tường minh). Audit field (`CreatedAt SYSUTCDATETIME()`) giữ DB default vì infrastructure-level.

## 10. Review checklist khi PR đụng Domain hoặc EF Configuration

- [ ] Entity mới: KHÔNG có default trên BẤT KỲ business field (enum, bool, decimal, int, double, string)?
- [ ] EF Configuration: KHÔNG có `HasDefaultValue` cho business field?
- [ ] Audit field (`CreatedAt`, `Timestamp`): GIỮ `HasDefaultValueSql("SYSUTCDATETIME()")`?
- [ ] Technical C# initializer (`= default!`, `new List<T>()`): GIỮ?
- [ ] Handler tạo entity: set TẤT CẢ business field tường minh trong `new Entity { ... }`?
- [ ] State transition method (nếu thêm): validate state machine, throw `InvariantViolationException`?
- [ ] PR có handler/seeder consumer cho method/property đụng vào? (Nếu KHÔNG → đừng thêm)
- [ ] Domain KHÔNG validate input format/range/required (Validator job)?
