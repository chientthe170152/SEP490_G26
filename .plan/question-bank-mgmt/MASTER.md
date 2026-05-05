# Question Bank & Shared Repository — Master Plan

> **Trạng thái**: ✅ **ĐÃ HOÀN THÀNH**
> Folder này gồm **tài liệu phân tích** (`01`…`08`) + **phase plan triển khai** (`phase-0`…`phase-9`).
>
> **Schema cuối cùng**: [`08-finalized-schema.md`](08-finalized-schema.md) — Q2 theo phương án **C** (`Purpose` enum + `Description` text). `Bank.Name` NOT NULL cho mọi loại; Shared Bank tự sinh theo công thức `"Kho chung {Purpose} {Subject.Name} {Subject.Code}"` ở `SubjectService.CreateAsync` (override CLAUDE.md "BE không ship Vietnamese strings" theo yêu cầu user 2026-05-05).
>
> **Promotion flow (chốt 2026-05-05)**: admin **batch-finalize** — duyệt/từ chối từng câu trên FE (client-side staging, không hit DB), rồi nhấn "Xác nhận hoàn tất yêu cầu" → BE atomic update mọi item + move câu Approve sang Shared bank + đặt header Status = Resolved. KHÔNG có PartiallyResolved. KHÔNG có endpoint move giữa Personal Banks. Chi tiết: [`phase-4-be-promotion.md`](phase-4-be-promotion.md) §FinalizeAsync.
>
> Tham chiếu nền: [`Backend/Models/Question.cs`](../../Backend/Models/Question.cs#L1-L35), [`Backend/Models/Paper.cs`](../../Backend/Models/Paper.cs#L1-L19), [`Backend/Models/ExamBlueprint.cs`](../../Backend/Models/ExamBlueprint.cs#L1-L33), [`Backend/Constants/QuestionPurpose.cs`](../../Backend/Constants/QuestionPurpose.cs), [`.plan/curriculum-mgmt/MASTER.md`](../curriculum-mgmt/MASTER.md) (đã định nghĩa Subject/Chapter lifecycle).

---

## 0. Tóm tắt nhanh yêu cầu của user

Trích nguyên văn (2026-05-03):

> Phát triển cơ chế ngân hàng đề và cơ chế kho đề chung. Toàn bộ tài liệu thiết kế cần lưu vào một thư mục mới trong `.plan`. Sẽ có 2 loại kho đề: kho đề làm đề luyện tập và kho đề tạo bài kiểm tra. Khi tạo một môn học sẽ tự động tạo 2 kho đề trống gắn liền với môn học này. Giáo viên có thể đề xuất đưa câu hỏi lên ngân hàng câu hỏi chung và admin có thể duyệt. Như vậy cần xác định được câu hỏi giáo viên yêu cầu đang đẩy lên ngân hàng câu hỏi nào, khi admin từ chối bắt buộc cần lý do. Khi admin duyệt thì giáo viên có còn sở hữu câu hỏi đó nữa không (nghĩa là đổi FK question bank từ ngân hàng câu hỏi của giáo viên thành ngân hàng câu hỏi chung, khi đó giáo viên không thể sửa câu hỏi). Giáo viên có thể tạo nhiều ngân hàng câu hỏi, vì vậy chức năng tạo câu hỏi và chọn nguồn câu hỏi khi tạo bài kiểm tra cần thay đổi. Chức năng tạo ngân hàng đề chưa có.

→ Có **rất nhiều quyết định mở** ẩn trong yêu cầu này. Mục tiêu của folder là **lôi tất cả ra**, đề xuất phương án và đánh giá, để user duyệt từng cái trước khi code.

---

## 1. Phạm vi tài liệu

```
.plan/question-bank-mgmt/
├── MASTER.md                          ← (file này) tổng quan + danh sách quyết định mở
├── 01-current-state-analysis.md       ← khảo sát hiện trạng schema + code điểm chạm
├── 02-domain-design.md                ← từ vựng + 4 khái niệm cốt lõi (Personal Bank / Shared Bank / Practice Pool / Test Pool)
├── 03-schema-options.md               ← 4 phương án mô hình dữ liệu, so sánh
├── 04-promotion-workflow-options.md   ← cơ chế đẩy câu hỏi lên kho chung + ownership transfer (5 phương án)
├── 05-create-exam-flow-options.md     ← thay đổi UI/API tạo bài kiểm tra khi giáo viên có nhiều bank
├── 06-edge-cases-and-risks.md         ← tác động lên code hiện có (Practice, Blueprint, Swap, Archive, …)
└── 07-recommendation.md               ← đề xuất chốt cho từng quyết định + lý do
```

**Quy ước**: mỗi tài liệu có format `Vấn đề → Phương án → So sánh → Đề xuất`. Đề xuất ở `07-recommendation.md` là **bản tổng hợp** — không quyết định thay user, chỉ gợi ý cùng lý do. User chốt thì viết phase plan.

---

## 2. Từ vựng đề xuất (vì user dùng "ngân hàng" và "kho đề" lẫn lộn)

Để tránh hiểu sai trong toàn bộ tài liệu, tôi cố định từ vựng như sau:

| Tiếng Việt | Tiếng Anh / code | Nghĩa |
|---|---|---|
| **Ngân hàng câu hỏi cá nhân** | Personal Question Bank | Kho do giáo viên tạo, chứa các câu hỏi do chính họ soạn. Một giáo viên có **nhiều** kho cá nhân. |
| **Kho câu hỏi chung của môn** | Shared Question Bank | Kho gắn với một Môn học, do Admin quản lý. Câu hỏi vào đây phải qua duyệt. **2 kho/môn**: một cho mục đích Luyện tập, một cho mục đích Kiểm tra. Đây chính là "2 kho đề" mà user nhắc tới. |
| **Đề xuất đẩy lên** | Promotion Request | Bản ghi giáo viên gửi để xin admin đưa một (hoặc nhiều) câu hỏi từ Personal Bank lên Shared Bank. |
| **Bài kiểm tra** | Exam (đã có) | Bài thi chính thức gán cho Class. Giữ nguyên schema. |
| **Đề luyện tập** | Practice Paper (đã có) | Đề tự sinh khi học sinh bấm "luyện tập". Giữ nguyên schema. |
| **Khung đề** | Exam Blueprint (đã có) | Recipe (chương × độ khó × số câu) để sinh đề kiểm tra. Giữ nguyên. |

> **Lưu ý**: trong tài liệu user dùng "ngân hàng đề" và "kho đề" gần như đồng nghĩa. Tôi quy về **Question Bank** (cái chứa **câu hỏi**, không phải đề bài) — vì model `Paper` hiện có là instance gắn với một `Exam` cụ thể, không phải một kho. Lập luận chi tiết xem [`02-domain-design.md`](02-domain-design.md) §1.
>
> **Nếu user muốn "kho đề" thực sự là kho chứa các đề mẫu (sample papers / templates) khác hẳn với kho câu hỏi**, đây là một quyết định mở Q1 dưới đây — phải làm rõ trước khi đi tiếp.

---

## 3. Quyết định đã chốt (user 2026-05-03)

> Mỗi câu trỏ tới phần thảo luận chi tiết ở các file kèm. Phương án **(R)** là phương án tôi khuyến nghị; phương án còn lại liệt kê đầy đủ để user so sánh.
>
> **Tóm tắt chốt**: Q1 ✅ Q2 ⚠️ chờ clarify Q3 ✅ Q4 ✅ Q5 ✅ + thêm rule "cùng subject" Q6 ✅ chỉ áp dụng cho mode blueprint Q7 ✅ Q8 ✅ teacher xem **tất cả** (đổi từ R) Q9 ✅. Schema cuối + bổ sung [`08-finalized-schema.md`](08-finalized-schema.md).

### Q1 — "Kho đề" là kho **câu hỏi** hay kho **đề (paper template)**?

- **A. (R)** Kho đề = Shared Question Bank — chứa câu hỏi đã duyệt, dùng làm nguồn để sinh đề. Phù hợp với cách `ExamBlueprint` + `Paper` hiện đang sinh đề từ pool câu hỏi.
- **B.** Kho đề = kho chứa các Paper Template — thực thể mới, lưu sẵn các đề mẫu (đầy đủ thứ tự câu hỏi). Phải thiết kế thêm bảng `PaperTemplate`. Cồng kềnh hơn nhiều.
- **C.** Cả hai — câu hỏi vào shared bank, đồng thời cho admin lưu sẵn paper template từ shared bank.

→ Chi tiết: [`02-domain-design.md`](02-domain-design.md) §1.

### Q2 — Cấu trúc **Personal Question Bank** thế nào?

Hiện tại `Question.CreatedByUserId` là cách duy nhất gom câu hỏi của một giáo viên. Không có thực thể "Bank" để giáo viên tự đặt tên / tổ chức / chia sẻ.

- **A.** Bank gắn 1 môn (1 bank ↔ 1 subject). Giáo viên tạo nhiều bank, mỗi cái cho một môn.
- **B.** Bank đa môn (1 bank ↔ N môn) — câu hỏi vẫn gắn `ChapterId` (suy ra môn).
- **C. (R)** Bank gắn 1 môn + có thêm trường `Purpose` (Practice / Exam / Mixed) để giáo viên tự phân loại. Khớp với `QuestionPurpose` đã có ở `Question`.
- **D.** Bỏ thực thể Bank, dùng **tag** để giáo viên tự gắn nhãn. Linh hoạt nhất, nhưng không đáp ứng "tạo nhiều ngân hàng" trong yêu cầu.

→ Chi tiết: [`03-schema-options.md`](03-schema-options.md) §2.

### Q3 — Khi admin duyệt, giáo viên có còn sở hữu câu hỏi không?

Đây là câu hỏi user nêu trực tiếp. Có 5 phương án:

- **A.** Move (user gợi ý): chuyển FK `QuestionBankId` từ Personal → Shared. Giáo viên mất quyền sửa hoàn toàn.
- **B.** Copy-on-promote: clone câu hỏi sang Shared. Bản gốc ở Personal vẫn của giáo viên, nhưng 2 bản tách rời, sửa Personal **không** ảnh hưởng Shared.
- **C. (R)** Move + giữ author: chuyển `QuestionBankId` sang Shared, nhưng `CreatedByUserId` giữ nguyên (giáo viên là **tác giả**, admin là **chủ kho**). Quyền sửa: chỉ admin (hoặc theo policy). Lịch sử attribution rõ ràng. Đây là cách Stack Overflow / Wikipedia làm.
- **D.** Move + giáo viên xin "rút lại": như C, nhưng có flow `WithdrawRequest` để giáo viên xin gỡ câu hỏi khỏi Shared (admin duyệt).
- **E.** Move + cho author edit nhẹ (sửa typo, không đổi nội dung gốc) thông qua endpoint riêng, admin có thể chặn.

→ Chi tiết: [`04-promotion-workflow-options.md`](04-promotion-workflow-options.md) §3. Cảnh báo về `Question.Status = Inprogress` (đã dùng trong đề thi) ở [`06-edge-cases-and-risks.md`](06-edge-cases-and-risks.md) §2.

### Q4 — Promotion request gửi ở mức **một câu** hay **một batch**?

- **A.** Một câu / request — admin duyệt từng cái, đơn giản nhất.
- **B. (R)** Một batch (N câu) / request — giáo viên check nhiều câu rồi submit chung. Admin có thể duyệt từng câu trong batch (Approve/Reject riêng từng câu, lưu lý do từng cái). Khớp UX với việc giáo viên thường tạo nhiều câu cùng lúc.
- **C.** Cả hai — endpoint `POST /requests` nhận `questionIds[]`. Không cấm size = 1.

→ Chi tiết: [`04-promotion-workflow-options.md`](04-promotion-workflow-options.md) §1.

### Q5 — Câu hỏi được **đẩy lên kho nào** (Practice vs Exam)?

User nêu: "cần xác định được câu hỏi giáo viên yêu cầu đang đẩy lên ngân hàng câu hỏi nào". Có 3 cách:

- **A.** Suy từ `Question.QuestionPurpose` (đã có): purpose=Practice → đẩy vào Shared Practice; purpose=Exam → vào Shared Exam.
- **B. (R)** Cho giáo viên **chọn rõ ràng** ở form đẩy lên (radio: Luyện tập / Kiểm tra), default = giá trị `QuestionPurpose` của câu hỏi đó. Giáo viên có thể đổi ý.
- **C.** Admin chọn lúc duyệt — giáo viên chỉ submit, admin quyết định kho. Kém minh bạch.

→ Chi tiết: [`04-promotion-workflow-options.md`](04-promotion-workflow-options.md) §2.

### Q6 — Khi tạo bài kiểm tra, giáo viên chọn nguồn câu hỏi như thế nào?

Hiện tại pool ở `AssignExamRepository.GetAllQuestionIdsForBlueprintRowAsync` lấy **tất cả câu hỏi `purpose=Exam`** trong toàn hệ thống — không có filter theo teacher / theo bank. Đây là bug âm thầm. Sau khi có Bank, phải sửa.

- **A.** Mặc định = Personal Banks **của tôi** ∪ Shared Bank của môn. Có toggle "chỉ Shared".
- **B. (R)** Multi-select bank ở form tạo đề — giáo viên check các bank muốn dùng (mặc định check tất cả của mình + Shared của môn). Pool câu hỏi tính từ union.
- **C.** Single-select — chỉ lấy từ 1 bank.
- **D.** Như B, có thêm rule "chỉ admin mới được dùng exclusive Shared" — không dùng Personal nữa khi đề hướng tới class chính thức. Quá cứng nhắc.

→ Chi tiết: [`05-create-exam-flow-options.md`](05-create-exam-flow-options.md).

### Q7 — Auto-create Shared Bank khi tạo Subject — chính xác làm ở đâu?

User: "Khi tạo một môn học sẽ tự động tạo 2 kho đề trống gắn liền với môn học này".

→ Hook vào [`SubjectService.CreateAsync`](../../Backend/Services/Implements/) (sẽ ra đời ở [`curriculum-mgmt/phase-3-be-subject.md`](../curriculum-mgmt/phase-3-be-subject.md)). Trong cùng transaction insert Subject, insert 2 dòng `QuestionBank` với `OwnerType=Shared, SubjectId=…, Purpose=Practice/Exam`. Không có quyết định mở — đây là detail kỹ thuật, ghi nhận để khi viết phase plan đụng vào curriculum-mgmt.

> Phụ thuộc: phase này **phải đi sau** `curriculum-mgmt P3 (Subject CRUD)`. Nếu muốn làm song song, tách ra: P3 tạo Subject; QB-mgmt phase đầu thêm cột FK + bảng QuestionBank + sửa `SubjectService.CreateAsync` để tạo 2 dòng ngay lúc tạo subject.

### Q8 — Quyền của Shared Bank: ai được **xem**, ai được **dùng**?

- **A. (R)** Mọi giáo viên xem được Shared Bank của môn họ đang dạy (lớp đang Active), Admin xem tất cả. Học sinh **không** xem trực tiếp (chỉ thấy gián tiếp qua đề luyện tập / bài kiểm tra).
- **B.** Mọi giáo viên xem được toàn bộ Shared Bank (mọi môn) — tăng tính chia sẻ, nhưng có thể nhiễu.
- **C.** Phân quyền theo role: thêm "Subject Admin" — phase sau.

→ Chi tiết: [`02-domain-design.md`](02-domain-design.md) §3.

### Q9 — Phụ thuộc vào curriculum-mgmt: chờ hay chạy song song?

Curriculum-mgmt đang ở giai đoạn "chờ duyệt phase plan, chưa code". Question Bank phụ thuộc Subject lifecycle (Q7).

- **A. (R)** Chờ curriculum-mgmt chạy xong P3 (Subject) rồi mới mở P0 của Question Bank. An toàn nhất.
- **B.** Đẩy song song: Question Bank tách thành phase riêng "thêm bảng QuestionBank, FK SubjectId, default insert 2 dòng cho mỗi subject hiện có"; sau khi P3 curriculum-mgmt chạy → patch `SubjectService.CreateAsync` thêm bước tạo 2 bank.
- **C.** Gộp Question Bank thành phase mới của curriculum-mgmt — tăng phạm vi phase đó.

---

## 4. Vấn đề âm thầm phát hiện trong code hiện có

Trong khi khảo sát, tôi phát hiện **3 vấn đề tồn đọng** mà cơ chế Question Bank phải giải quyết:

### 4.1 — Pool câu hỏi cho đề kiểm tra **không có** filter teacher (PARTIAL FIX)

**List view đã được fix** sau khi refactor AssignExam sang Result pattern: [`AssignExamRepository.GetQuestionsAsync:184-187`](../../Backend/Repositories/Implements/AssignExamRepository.cs#L184-L187) đã có `if (teacherId.HasValue && teacherId.Value > 0) query = query.Where(...CreatedByUserId == teacherId);`.

**Pool generation vẫn không filter teacher** ở:
- [`GetQuestionIdsForBlueprintRowAsync:203-212`](../../Backend/Repositories/Implements/AssignExamRepository.cs#L203-L212) (sinh đề blueprint)
- [`GetAllQuestionIdsForBlueprintRowAsync:214-221`](../../Backend/Repositories/Implements/AssignExamRepository.cs#L214-L221) (count pool blueprint)
- [`BuildQuestionQuery:470-478`](../../Backend/Repositories/Implements/AssignExamRepository.cs#L470-L478) helper (dùng bởi list view + `GetAlternativeQuestionsAsync` swap câu)

→ So với [`PracticeExamRepository.GetAllPracticeQuestionIdsAsync`](../../Backend/Repositories/Implements/PracticeExamRepository.cs#L134-L146) có filter `q.CreatedByUserId == teacherId`. Vẫn bất nhất ở chỗ critical (sinh đề thực tế + swap).

→ Hệ quả: hôm nay giáo viên A vẫn có thể vô tình lấy câu của giáo viên B vào đề kiểm tra. Phase 5 sẽ refactor pool sang Bank — định nghĩa rõ pool = "Personal Banks của tôi ∪ Shared Bank cùng Subject+Kind".

### 4.2 — `Question.QuestionPurpose` vs Bank.Purpose: trùng thông tin

Câu hỏi có sẵn field `QuestionPurpose (Practice|Exam)`. Nếu Bank cũng có `Purpose`, sẽ có 2 nguồn sự thật.

→ Đề xuất: bỏ `Question.QuestionPurpose` (data migration: copy sang field `Purpose` của bank gốc), hoặc giữ và **enforce trùng với Bank.Purpose** trong cùng transaction. Xem [`03-schema-options.md`](03-schema-options.md) §3.

### 4.3 — `Question.Status = Inprogress` lock soft khi câu hỏi bị dùng trong đề

[`QuestionService.UpdateQuestionAsync`](../../Backend/Services/Implements/QuestionService.cs#L59-L88) đã có cơ chế clone-on-write khi câu hỏi đang Inprogress / đã dùng. Cơ chế Promotion phải **hợp tác** với cái này — không được Promote một câu Inprogress ra Shared (nếu giáo viên còn đang sửa) hoặc cần policy rõ ràng cho trường hợp đó.

→ Chi tiết: [`06-edge-cases-and-risks.md`](06-edge-cases-and-risks.md) §2.

---

## 5. Roadmap (10 phase — đã chốt)

Mỗi phase được thiết kế để **1 AI agent có thể làm trọn vẹn trong 1 context window** (≤ ~30 file đụng, ≤ ~700 LOC mới). Build BE + FE phải xanh ngay sau khi đóng phase.

| Phase | File | Mục tiêu | Phụ thuộc | LOC mới |
|---|---|---|---|---|
| **P0** | [phase-0-mockups.md](phase-0-mockups.md) | Vẽ mockup HTML chạy được trong browser cho 6 màn (teacher list bank, teacher detail bank, modal promotion, admin list request, admin detail request, teacher tạo đề kiểm tra cập nhật). | Không | ~6 file HTML / ~1500 LOC |
| ✅ **P2** | [phase-2-be-question-bank.md](phase-2-be-question-bank.md) | BE QuestionBank CRUD: `QuestionBankController`/Service/Repo cho Personal Bank (teacher); list Shared Bank (read-only cho teacher, full CRUD cho admin sau). Hook `SubjectService.CreateAsync` để insert 2 Shared Bank. | P1 | ~600 |
| ✅ **P3** | [phase-3-be-question-refactor.md](phase-3-be-question-refactor.md) | Refactor `QuestionService` ownership từ `CreatedByUserId` → `Bank`. Thêm endpoint move câu giữa Personal Banks. Đổi `QuestionDto` để nhận `QuestionBankId`. Update `QuestionUnitTest`. | P2 | ~500 |
| ✅ **P4** | [phase-4-be-promotion.md](phase-4-be-promotion.md) | BE Promotion: `PromotionRequestController` (teacher) + `AdminPromotionController` (admin) + service + 2 repo + Errors + Validators (lý do bắt buộc). Logic create batch, approve/reject/withdraw, race-safe update. | P2 | ~700 |
| ✅ **P5** | [phase-5-be-pool-refactor.md](phase-5-be-pool-refactor.md) | Refactor pool query: `AssignExamRepository` + `PracticeExamRepository` đổi sang join Bank. Endpoint `preview-pool` + `usable-banks`. Mode `manual` giữ chọn tay (theo Q6). Update test pool. | P3 | ~450 |
| ✅ **P6** | [phase-6-fe-teacher-bank.md](phase-6-fe-teacher-bank.md) | FE giáo viên: trang `QuestionBank/Index`, `QuestionBank/Detail`, modal Promotion. Cập nhật form tạo câu hỏi để bind vào Bank. | P2, P4 | ~700 |
| ✅ **P7** | [phase-7-fe-teacher-create-exam.md](phase-7-fe-teacher-create-exam.md) | FE giáo viên: cập nhật `Class/CreateExam` mode blueprint = multi-select bank + preview pool count. Cập nhật trang Practice cho học sinh (badge nguồn câu). | P5, P6 | ~450 |
| ✅ **P8** | [phase-8-fe-admin-promotion.md](phase-8-fe-admin-promotion.md) | FE admin: trang `Admin/PromotionRequests` (list + detail + approve/reject modal lý do). | P4 | ~500 |
| ✅ **P9** | [phase-9-smoke-and-docs.md](phase-9-smoke-and-docs.md) | Smoke test 25 endpoint, cập nhật `database/script-schema.sql`, `CLAUDE.md`, `.plan/test-accounts.md`. | P0–P8 | ~150 |

**Quy tắc đóng phase**:
- `dotnet build MTCA_SEP490_G26.sln` → 0 errors.
- `dotnet test BackEnd_UnitTest/BackEnd_UnitTest.csproj --filter "FullyQualifiedName~<Phase>UnitTest"` → pass (nếu phase có test mới).
  > **LƯU Ý KHI CHẠY TEST**: Hiện tại toàn bộ project `BackEnd_UnitTest` đang bị lỗi biên dịch do các module cũ (Analytics, StudentExam, Profile). Khi cần chạy test cho phase mới: (1) Tạm thời xóa/xóa thư mục các file test lỗi đó, (2) Viết file test mới bằng **xUnit**, (3) Chạy `dotnet test`, (4) NẾU PASS: **xóa file test vừa tạo** và chạy `git restore BackEnd_UnitTest/` để khôi phục các test cũ. **Mục đích: Code xong phải kiểm chứng ngay nhưng tuyệt đối KHÔNG commit file test mới lên repo.**
- Smoke test mô tả ở cuối mỗi file phase phải chạy được tay.
- Tick checklist trong file phase. Không đóng phase nếu còn task chưa tick.

**Quy tắc mở phase**:
- Đọc `MASTER.md` (file này) + `08-finalized-schema.md` + file phase tương ứng. Không cần đọc các file phase trước (đều đã đóng).
- Đọc các file phân tích `01`…`07` chỉ khi file phase chỉ ra cụ thể.
