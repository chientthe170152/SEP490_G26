using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs;
using Backend.Jobs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.AssignExamUnitTest
{
    public class AssignExamServiceCoverageTests
    {
        private readonly Mock<IAssignExamRepository> _repo = new();
        private readonly Mock<IExamStatusScheduler> _scheduler = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly AssignExamService _service;
        private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public AssignExamServiceCoverageTests()
        {
            _service = new AssignExamService(_repo.Object, _currentUser.Object, _scheduler.Object, new FakeTimeProvider(FixedNow));
            _currentUser.Setup(u => u.UserId).Returns(1);
        }

        // ─────── GetBlueprintsAsync ───────

        [Fact(DisplayName = "GetBlueprintsAsync - UTCID01 - Teacher inactive -> TeacherNotFound")]
        public async Task GetBlueprints_TeacherInactive_NotFound()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(false);

            var result = await _service.GetBlueprintsAsync(null, null);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.TeacherNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetBlueprintsAsync - UTCID02 - Trim filters and return")]
        public async Task GetBlueprints_Active_TrimFilters()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintsAsync(1, "MAE", "PT", default))
                .ReturnsAsync(new List<BlueprintListItemDto>());

            var result = await _service.GetBlueprintsAsync("  MAE  ", "  PT  ");

            Assert.True(result.IsSuccess);
        }

        // ─────── GetBlueprintDetailAsync ───────

        [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID01 - Trả về data")]
        public async Task GetBlueprintDetail_ShouldReturn()
        {
            _repo.Setup(r => r.GetBlueprintDetailAsync(5, default)).ReturnsAsync(new List<BlueprintDetailRowDto>());

            var result = await _service.GetBlueprintDetailAsync(5);

            Assert.True(result.IsSuccess);
        }

        // ─────── GetQuestionsAsync ───────

        [Fact(DisplayName = "GetQuestionsAsync - UTCID01 - Teacher inactive -> TeacherNotFound")]
        public async Task GetQuestions_TeacherInactive_NotFound()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(false);

            var result = await _service.GetQuestionsAsync(null, null, null);

            Assert.Equal(AssignExamErrors.TeacherNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetQuestionsAsync - UTCID02 - Active -> trả về list")]
        public async Task GetQuestions_Active_ReturnsList()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetQuestionsAsync(1, "M", 1, 1, It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<QuestionListItemDto>());

            var result = await _service.GetQuestionsAsync(" M ", 1, 1);

            Assert.True(result.IsSuccess);
        }

        // ─────── CreateAssignExamAsync ───────

        private CreateAssignExamRequest BaseBlueprintRequest() => new()
        {
            Title = "X",
            Duration = 60,
            MaxAttempts = 1,
            PaperCount = 1,
            PaperCode = 1,
            VisibleFrom = FixedNow.UtcDateTime.AddDays(1),
            OpenAt = FixedNow.UtcDateTime.AddDays(2),
            CloseAt = FixedNow.UtcDateTime.AddDays(3),
            ShuffleQuestion = false,
            IsPublic = false,
            ClassId = 10,
            GenerationMode = "blueprint",
            ExamBlueprintId = 100
        };

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-InvalidWindow - VisibleFrom > OpenAt -> InvalidTimeWindow")]
        public async Task Create_InvalidTimeWindow()
        {
            var req = BaseBlueprintRequest();
            req.VisibleFrom = FixedNow.UtcDateTime.AddDays(5);

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.InvalidTimeWindow.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-InvalidWindow2 - OpenAt >= CloseAt")]
        public async Task Create_OpenAfterClose()
        {
            var req = BaseBlueprintRequest();
            req.OpenAt = FixedNow.UtcDateTime.AddDays(5);
            req.CloseAt = FixedNow.UtcDateTime.AddDays(4);

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.InvalidTimeWindow.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-TeacherInactive")]
        public async Task Create_TeacherInactive()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(false);

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.Equal(AssignExamErrors.TeacherNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-MissingClassId - IsPublic=false + ClassId null -> MissingClassId")]
        public async Task Create_MissingClassId()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            var req = BaseBlueprintRequest();
            req.IsPublic = false;
            req.ClassId = null;

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.MissingClassId.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-PublicWithClass - IsPublic=true + ClassId -> PublicWithClassId")]
        public async Task Create_PublicWithClass()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            var req = BaseBlueprintRequest();
            req.IsPublic = true;

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.PublicWithClassId.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-BlueprintNotFound")]
        public async Task Create_BlueprintNotFound()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintWithChaptersAsync(100, default)).ReturnsAsync((ExamBlueprint?)null);

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.Equal(AssignExamErrors.BlueprintNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Insufficient - Bank không đủ câu")]
        public async Task Create_BlueprintInsufficient()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintWithChaptersAsync(100, default))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 100, SubjectId = 1,
                    ExamBlueprintChapters = new List<ExamBlueprintChapter>
                    {
                        new() { ChapterId = 1, Difficulty = 1, TotalOfQuestions = 10 }
                    }
                });
            _repo.Setup(r => r.GetAllQuestionIdsForBlueprintRowAsync(1, 1, It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<int> { 1, 2, 3 });

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.Equal(AssignExamErrors.InsufficientQuestions.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-ClassNotFound")]
        public async Task Create_ClassNotFound()
        {
            SetupBlueprintHappyPath();
            _repo.Setup(r => r.GetClassByIdAsync(10, default)).ReturnsAsync((Class?)null);

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.Equal(AssignExamErrors.ClassNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-ClassNotOwned")]
        public async Task Create_ClassNotOwned()
        {
            SetupBlueprintHappyPath();
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 999, SubjectId = 1 });

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.Equal(AssignExamErrors.ClassNotOwnedByTeacher.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-SubjectMismatch")]
        public async Task Create_SubjectMismatch()
        {
            SetupBlueprintHappyPath();
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 999 });

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.Equal(AssignExamErrors.SubjectMismatch.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Blueprint-Success")]
        public async Task Create_Blueprint_Success()
        {
            SetupBlueprintHappyPath();
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 1 });
            SetupTransactionAndPersistence();

            var result = await _service.CreateAssignExamAsync(BaseBlueprintRequest());

            Assert.True(result.IsSuccess);
            Assert.Equal(500, result.Value.ExamId);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Blueprint-MultiPaper-WrapAround - Bank phải reshuffle")]
        public async Task Create_Blueprint_MultiPaper_BankWrap()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintWithChaptersAsync(100, default))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 100, SubjectId = 1,
                    ExamBlueprintChapters = new List<ExamBlueprintChapter>
                    {
                        new() { ChapterId = 1, Difficulty = 1, TotalOfQuestions = 3 }
                    }
                });
            _repo.Setup(r => r.GetAllQuestionIdsForBlueprintRowAsync(1, 1, It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5 });
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 1 });
            SetupTransactionAndPersistence();
            var req = BaseBlueprintRequest();
            req.PaperCount = 3; // exhaust pool

            var result = await _service.CreateAssignExamAsync(req);

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Manual-Empty -> ManualEmptyQuestions")]
        public async Task Create_Manual_Empty()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            var req = BaseBlueprintRequest();
            req.GenerationMode = "manual";
            req.QuestionIds = new List<int>();

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.ManualEmptyQuestions.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Manual-Inactive -> InvalidOrInactiveQuestions")]
        public async Task Create_Manual_Inactive()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<QuestionSubjectDto> { new(1, 1) }); // 1 trả về nhưng yêu cầu 2
            var req = BaseBlueprintRequest();
            req.GenerationMode = "manual";
            req.QuestionIds = new List<int> { 1, 2 };

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.InvalidOrInactiveQuestions.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Manual-MultiSubjects")]
        public async Task Create_Manual_MultipleSubjects()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<QuestionSubjectDto> { new(1, 1), new(2, 2) });
            var req = BaseBlueprintRequest();
            req.GenerationMode = "manual";
            req.QuestionIds = new List<int> { 1, 2 };

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.MultipleSubjects.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Manual-SubjectMismatch")]
        public async Task Create_Manual_SubjectMismatch()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<QuestionSubjectDto> { new(1, 1), new(2, 1) });
            var req = BaseBlueprintRequest();
            req.GenerationMode = "manual";
            req.SubjectId = 999;
            req.QuestionIds = new List<int> { 1, 2 };

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.SubjectMismatch.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Manual-Success")]
        public async Task Create_Manual_Success()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<QuestionSubjectDto> { new(1, 1), new(2, 1) });
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 1 });
            SetupTransactionAndPersistence();
            var req = BaseBlueprintRequest();
            req.GenerationMode = "manual";
            req.QuestionIds = new List<int> { 1, 2 };

            var result = await _service.CreateAssignExamAsync(req);

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Manual-ShuffleMultiPaper - Pool đủ cho nhiều đề")]
        public async Task Create_Manual_Shuffle_MultiPaper()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<QuestionSubjectDto>
                {
                    new(1, 1), new(2, 1), new(3, 1), new(4, 1)
                });
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 1 });
            SetupTransactionAndPersistence();
            var req = BaseBlueprintRequest();
            req.GenerationMode = "manual";
            req.QuestionIds = new List<int> { 1, 2, 3, 4 };
            req.ShuffleQuestion = true;
            req.PaperCount = 3;

            var result = await _service.CreateAssignExamAsync(req);

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-Public-NoClass-Success")]
        public async Task Create_Public_NoClass_Success()
        {
            SetupBlueprintHappyPath();
            SetupTransactionAndPersistence();
            var req = BaseBlueprintRequest();
            req.IsPublic = true;
            req.ClassId = null;

            var result = await _service.CreateAssignExamAsync(req);

            Assert.True(result.IsSuccess);
        }

        // ─────── GetExamReviewAsync ───────

        [Fact(DisplayName = "GetExamReviewAsync - UTCID-NotFound")]
        public async Task GetReview_NotFound()
        {
            _repo.Setup(r => r.GetExamReviewDataAsync(1, default)).ReturnsAsync((Exam?)null);

            var result = await _service.GetExamReviewAsync(1);

            Assert.Equal(AssignExamErrors.ExamNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetExamReviewAsync - UTCID-Found - Trả về review chi tiết")]
        public async Task GetReview_Found_ShouldMap()
        {
            var exam = new Exam
            {
                ExamId = 1, ClassId = 10, Title = "T",
                Subject = new Subject { Code = "MATH" },
                Description = "D", Duration = 60, Status = ExamStatus.Ready,
                Teacher = new User { FullName = "Mr A", Email = "a@a" },
                ExamBlueprint = new ExamBlueprint
                {
                    ExamBlueprintChapters = new List<ExamBlueprintChapter>
                    {
                        new() { Chapter = new Chapter { Name = "Ch1" }, Difficulty = 1, TotalOfQuestions = 2 },
                        new() { Chapter = new Chapter { Name = "Ch1" }, Difficulty = 2, TotalOfQuestions = 1 },
                        new() { Chapter = null, Difficulty = 3, TotalOfQuestions = 1 },
                        new() { Chapter = new Chapter { Name = "Ch1" }, Difficulty = 4, TotalOfQuestions = 1 }
                    }
                },
                Papers = new List<Paper>
                {
                    new()
                    {
                        PaperId = 5, Code = 1,
                        Questions = new List<Question>
                        {
                            new()
                            {
                                QuestionId = 100, QuestionType = "MCQ", QuestionContent = "Q",
                                Difficulty = 1,
                                Chapter = new Chapter { Name = "Ch1" },
                                QuestionAnswers = new List<QuestionAnswer>
                                {
                                    new() { QuestionAnswerId = 200, Content = "A", CorrectAnswer = "A", IsCorrect = true }
                                }
                            }
                        }
                    }
                }
            };
            _repo.Setup(r => r.GetExamReviewDataAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.GetExamReviewAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal("MATH", result.Value.SubjectCode);
            Assert.NotEmpty(result.Value.BlueprintMatrix);
            Assert.NotEmpty(result.Value.Papers);
        }

        // ─────── GetAlternativeQuestionsAsync ───────

        [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID-PaperNotFound")]
        public async Task GetAlternatives_PaperNotFound()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default)).ReturnsAsync((Paper?)null);

            var result = await _service.GetAlternativeQuestionsAsync(1, 100);

            Assert.Equal(AssignExamErrors.PaperNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID-QuestionNotFound")]
        public async Task GetAlternatives_QuestionNotFound()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper { PaperId = 1, ExamId = 5, Exam = new Exam { ExamId = 5, SubjectId = 1 }, Questions = new List<Question>() });
            _repo.Setup(r => r.GetQuestionByIdAsync(100, default)).ReturnsAsync((Question?)null);

            var result = await _service.GetAlternativeQuestionsAsync(1, 100);

            Assert.Equal(AssignExamErrors.QuestionNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID-NoExam - Paper.Exam null -> ExamNotFound")]
        public async Task GetAlternatives_NoExam()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper { PaperId = 1, Exam = null, Questions = new List<Question>() });
            _repo.Setup(r => r.GetQuestionByIdAsync(100, default))
                .ReturnsAsync(new Question { QuestionId = 100, ChapterId = 1, Difficulty = 1 });

            var result = await _service.GetAlternativeQuestionsAsync(1, 100);

            Assert.Equal(AssignExamErrors.ExamNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID-Success")]
        public async Task GetAlternatives_Success()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1, ExamId = 5,
                    Exam = new Exam { ExamId = 5, SubjectId = 1 },
                    Questions = new List<Question> { new() { QuestionId = 100 } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(100, default))
                .ReturnsAsync(new Question { QuestionId = 100, ChapterId = 1, Difficulty = 1 });
            _repo.Setup(r => r.GetAlternativeQuestionsAsync(1, 1, 1, It.IsAny<string[]>(), It.IsAny<List<int>>(), default))
                .ReturnsAsync(new List<QuestionListItemDto>());

            var result = await _service.GetAlternativeQuestionsAsync(1, 100);

            Assert.True(result.IsSuccess);
        }

        // ─────── SwapPaperQuestionAsync ───────

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-MissingFields")]
        public async Task Swap_MissingFields()
        {
            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto());

            Assert.Equal(AssignExamErrors.SwapMissingFields.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-PaperNotFound")]
        public async Task Swap_PaperNotFound()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default)).ReturnsAsync((Paper?)null);

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200
            });

            Assert.Equal(AssignExamErrors.PaperNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-QuestionNotInPaper")]
        public async Task Swap_QuestionNotInPaper()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper { PaperId = 1, Questions = new List<Question>() });

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200
            });

            Assert.Equal(AssignExamErrors.QuestionNotInPaper.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-NewNotFound")]
        public async Task Swap_NewQuestionNotFound()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1,
                    Questions = new List<Question> { new() { QuestionId = 100, ChapterId = 1, Difficulty = 1, Status = QuestionStatus.Active } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(200, default)).ReturnsAsync((Question?)null);

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200
            });

            Assert.Equal(AssignExamErrors.QuestionNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-Inactive")]
        public async Task Swap_NewInactive()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1,
                    Questions = new List<Question> { new() { QuestionId = 100, ChapterId = 1, Difficulty = 1 } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(200, default))
                .ReturnsAsync(new Question { QuestionId = 200, Status = QuestionStatus.Draft, ChapterId = 1, Difficulty = 1 });

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200
            });

            Assert.Equal(AssignExamErrors.QuestionInactive.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-DiffMismatch")]
        public async Task Swap_DifficultyMismatch()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1,
                    Questions = new List<Question> { new() { QuestionId = 100, ChapterId = 1, Difficulty = 1 } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(200, default))
                .ReturnsAsync(new Question { QuestionId = 200, Status = QuestionStatus.Active, ChapterId = 1, Difficulty = 2 });

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200
            });

            Assert.Equal(AssignExamErrors.DifficultyMismatch.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-ChapterMismatch")]
        public async Task Swap_ChapterMismatch()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1,
                    Questions = new List<Question> { new() { QuestionId = 100, ChapterId = 1, Difficulty = 1 } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(200, default))
                .ReturnsAsync(new Question { QuestionId = 200, Status = QuestionStatus.Active, ChapterId = 999, Difficulty = 1 });

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200
            });

            Assert.Equal(AssignExamErrors.ChapterMismatch.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-Success-Local")]
        public async Task Swap_Local_Success()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1, ExamId = 5,
                    Questions = new List<Question> { new() { QuestionId = 100, ChapterId = 1, Difficulty = 1 } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(200, default))
                .ReturnsAsync(new Question { QuestionId = 200, Status = QuestionStatus.Active, ChapterId = 1, Difficulty = 1 });
            _repo.Setup(r => r.SwapPaperQuestionAsync(1, 100, 200, default)).Returns(Task.CompletedTask);

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200, SwapGlobal = false
            });

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.SwapPaperQuestionAsync(1, 100, 200, default), Times.Once);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID-Success-Global")]
        public async Task Swap_Global_Success()
        {
            _repo.Setup(r => r.GetPaperWithQuestionsAsync(1, default))
                .ReturnsAsync(new Paper
                {
                    PaperId = 1, ExamId = 5,
                    Questions = new List<Question> { new() { QuestionId = 100, ChapterId = 1, Difficulty = 1 } }
                });
            _repo.Setup(r => r.GetQuestionByIdAsync(200, default))
                .ReturnsAsync(new Question { QuestionId = 200, Status = QuestionStatus.Active, ChapterId = 1, Difficulty = 1 });
            _repo.Setup(r => r.SwapExamQuestionGloballyAsync(5, 100, 200, default)).Returns(Task.CompletedTask);

            var result = await _service.SwapPaperQuestionAsync(new SwapQuestionRequestDto
            {
                PaperId = 1, OldQuestionId = 100, NewQuestionId = 200, SwapGlobal = true
            });

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.SwapExamQuestionGloballyAsync(5, 100, 200, default), Times.Once);
        }

        // ─────── helpers ─────────────────

        private void SetupBlueprintHappyPath()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintWithChaptersAsync(100, default))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 100, SubjectId = 1,
                    ExamBlueprintChapters = new List<ExamBlueprintChapter>
                    {
                        new() { ChapterId = 1, Difficulty = 1, TotalOfQuestions = 2 }
                    }
                });
            _repo.Setup(r => r.GetAllQuestionIdsForBlueprintRowAsync(1, 1, It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5 });
        }

        private void SetupTransactionAndPersistence()
        {
            var tx = new Mock<IDbContextTransaction>();
            tx.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);
            tx.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);
            tx.Setup(t => t.Dispose());

            _repo.Setup(r => r.BeginTransactionAsync(default)).ReturnsAsync(tx.Object);
            _repo.Setup(r => r.SaveExamAsync(It.IsAny<Exam>(), default))
                .Returns<Exam, CancellationToken>((e, _) => { e.ExamId = 500; return Task.FromResult(e); });
            _repo.Setup(r => r.SavePaperAsync(It.IsAny<Paper>(), default))
                .Returns<Paper, CancellationToken>((p, _) => { p.PaperId = 1000; return Task.FromResult(p); });
            _repo.Setup(r => r.AddPaperQuestionsAsync(It.IsAny<int>(), It.IsAny<List<int>>(), default))
                .Returns(Task.CompletedTask);
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;
            public FakeTimeProvider(DateTimeOffset now) => _now = now;
            public override DateTimeOffset GetUtcNow() => _now;
        }
    }
}
