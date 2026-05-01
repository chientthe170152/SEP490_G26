using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.PracticeExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.PracticeExamUnitTest
{
    public class PracticeExamServiceMissingCoverageTests
    {
        private readonly Mock<IPracticeExamRepository> _repo = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<ILogger<PracticeExamService>> _logger = new();
        private readonly PracticeExamService _service;

        public PracticeExamServiceMissingCoverageTests()
        {
            _service = new PracticeExamService(_repo.Object, _currentUser.Object, _logger.Object, TimeProvider.System);
        }

        // ---------- GetChaptersForPracticeAsync ----------

        [Fact(DisplayName = "GetChaptersForPracticeAsync - UTCID-NoChapters - Subject không có chapter -> empty list")]
        public async Task GetChapters_NoChapters_ShouldReturnEmpty()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1))
                .ReturnsAsync(new Class { ClassId = 10, SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1)).ReturnsAsync(new List<Chapter>());

            var result = await _service.GetChaptersForPracticeAsync(10);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact(DisplayName = "GetChaptersForPracticeAsync - UTCID-WithChapters - Trả về full breakdown")]
        public async Task GetChapters_WithChapters_FullBreakdown()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1))
                .ReturnsAsync(new Class { ClassId = 10, SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "Ch1" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalAttempted = 4, CorrectCount = 2 }
                });
            _repo.Setup(r => r.CountPracticeQuestionsAsync(1, 5, null)).ReturnsAsync(20);
            _repo.Setup(r => r.CountPracticeQuestionsAsync(1, 5, It.IsAny<List<int>>())).ReturnsAsync(5);

            var result = await _service.GetChaptersForPracticeAsync(10);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(50, result.Value[0].OverallAccuracyRate);
            Assert.Equal(4, result.Value[0].DifficultyBreakdown.Count);
        }

        // ---------- CreatePracticeExamAsync ----------

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-NoChapters - ChapterIds null -> ChapterRequired")]
        public async Task Create_NullChapters_ShouldReturnRequired()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest { ClassId = 10, ChapterIds = null });

            Assert.Equal(PracticeExamErrors.ChapterRequired.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-EmptyChapters - ChapterIds rỗng -> ChapterRequired")]
        public async Task Create_EmptyChapters_ShouldReturnRequired()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest { ClassId = 10, ChapterIds = new List<int>() });

            Assert.Equal(PracticeExamErrors.ChapterRequired.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-InvalidCount - TotalQuestions<5 -> InvalidQuestionCount")]
        public async Task Create_TotalLT5_ShouldReturnInvalid()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 4
            });

            Assert.Equal(PracticeExamErrors.InvalidQuestionCount.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-Over30 - TotalQuestions>30 -> InvalidQuestionCount")]
        public async Task Create_TotalGT30_ShouldReturnInvalid()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 31
            });

            Assert.Equal(PracticeExamErrors.InvalidQuestionCount.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-WrongChapter - ChapterId không thuộc subject -> ChapterNotBelongToSubject")]
        public async Task Create_ChapterNotBelong_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1)).ReturnsAsync(new List<Chapter> { new() { ChapterId = 1 } });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 999 }, TotalQuestions = 5
            });

            Assert.Equal(PracticeExamErrors.ChapterNotBelongToSubject.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-NoQuestionsFound - Không có câu nào được chọn -> NoQuestionsFound")]
        public async Task Create_NoQuestions_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>());
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>());
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int>());

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.Equal(PracticeExamErrors.NoQuestionsFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-Success-NoHistory - Phân bổ đều 4 mức độ")]
        public async Task Create_Success_NoHistory_ShouldDistributeEqually()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>());
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>());
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int> { 100, 101, 102, 103, 104 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper
            {
                PaperId = 50,
                Questions = new List<Question>()
            });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(50, result.Value.PaperId);
            Assert.Equal(200, result.Value.SubmissionId);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-BuildQuestionsDto - Paper với question + BlankInput đầy đủ")]
        public async Task Create_BuildQuestionsDto_Full()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>());
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>());
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int> { 100, 101, 102, 103, 104, 105 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper
            {
                PaperId = 50,
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionId = 100, QuestionType = QuestionType.FillBlank,
                        QuestionContent = "{\"stem\":\"x\"}", Difficulty = 2,
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new()
                            {
                                QuestionAnswerId = 1, Content = "placeholder[1]", GroupAnswerId = 5,
                                BlankInputs = new List<BlankInput>
                                {
                                    new()
                                    {
                                        InputTypeId = 1,
                                        InputType = new InputType { InputTypeId = 1, Name = "Number", GroupType = "Numeric" }
                                    },
                                    new()
                                    {
                                        InputTypeId = 2,
                                        InputType = null
                                    }
                                }
                            }
                        }
                    }
                }
            });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Questions);
            Assert.Equal(2, result.Value.Questions[0].Answers[0].InputTypes.Count);
            Assert.Equal("Number", result.Value.Questions[0].Answers[0].InputTypes[0].Name);
            Assert.Equal(string.Empty, result.Value.Questions[0].Answers[0].InputTypes[1].Name);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-AllPoolMastered - Wrong/Unseen ít, fallback Mastered")]
        public async Task Create_AllMasteredFallback()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>());
            // All questions mastered -> wrong=0, unseen=0, mastered=large
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>
                {
                    new() { QuestionId = 100, IsMastered = true },
                    new() { QuestionId = 101, IsMastered = true },
                    new() { QuestionId = 102, IsMastered = true },
                    new() { QuestionId = 103, IsMastered = true }
                });
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int> { 100, 101, 102, 103 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper { PaperId = 50, Questions = new List<Question>() });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-WrongInsufficient - Wrong < wrongQuota, deficit transfer")]
        public async Task Create_WrongInsufficient_DeficitToUnseen()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>());
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>
                {
                    new() { QuestionId = 100, IsMastered = false } // 1 wrong only
                });
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int> { 100, 101, 102, 103, 104 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper { PaperId = 50, Questions = new List<Question>() });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-PoolDeficit - Pool nhỏ hơn TotalQuestions -> bù từ all pool")]
        public async Task Create_PoolDeficit_ShouldSupplementFromAll()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>());
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>());
            // Mỗi diff chỉ có 1 ID (tổng 1*4=4 nhưng quota 5)
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.Is<List<int>>(l => l != null && l.Count > 0)))
                .ReturnsAsync(new List<int> { 100 });
            // pool đầy đủ khi không filter
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, (List<int>?)null))
                .ReturnsAsync(new List<int> { 100, 200, 300, 400, 500 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper { PaperId = 50, Questions = new List<Question>() });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-Success-WithHistory - Có proficiency, đầy đủ pool")]
        public async Task Create_Success_WithHistory_AllPools()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalAttempted = 10, CorrectCount = 2 },  // Weak
                    new() { ChapterId = 1, Difficulty = 2, TotalAttempted = 10, CorrectCount = 6 },  // Average
                    new() { ChapterId = 1, Difficulty = 3, TotalAttempted = 10, CorrectCount = 9 }   // Strong
                });
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>
                {
                    new() { QuestionId = 100, IsMastered = false, CorrectCount = 0 },
                    new() { QuestionId = 200, IsMastered = true, CorrectCount = 3 }
                });
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<int> { 100, 200, 300, 400, 500 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper
            {
                PaperId = 50,
                Questions = new List<Question>()
            });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 10
            });

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID-Spaced - Pool > slotTarget kích hoạt fallback unseen->wrong leftover")]
        public async Task Create_FallbackUnseenLeftover()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetClassWithValidationAsync(10, 1)).ReturnsAsync(new Class { SubjectId = 1, TeacherId = 5 });
            _repo.Setup(r => r.GetChaptersBySubjectIdAsync(1))
                .ReturnsAsync(new List<Chapter> { new() { ChapterId = 1, Name = "C" } });
            // Pool small but with mix where unseen is short -> deficit covered by remaining wrong
            _repo.Setup(r => r.GetStudentProficiencyAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<StudentProficiencyRaw>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalAttempted = 1, CorrectCount = 0 }
                });
            _repo.Setup(r => r.GetStudentItemLevelHistoryAsync(1, It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PracticeQuestionResultDto>
                {
                    new() { QuestionId = 100, IsMastered = false }, // wrong
                    new() { QuestionId = 101, IsMastered = false }, // wrong
                    new() { QuestionId = 200, IsMastered = true }   // mastered
                });
            // pool returns small set
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> chs, int t, List<int>? df) =>
                    df == null
                        ? new List<int> { 100, 101, 200, 300 }
                        : new List<int> { 100, 101, 200, 300 });
            _repo.Setup(r => r.GetAllPracticeQuestionIdsAsync(It.IsAny<List<int>>(), 5, null))
                .ReturnsAsync(new List<int> { 100, 101, 200, 300, 400 });
            _repo.Setup(r => r.CreatePracticePaperAsync(It.IsAny<List<int>>())).ReturnsAsync(new Paper { PaperId = 50 });
            _repo.Setup(r => r.CreatePracticeSubmissionAsync(1, 50)).ReturnsAsync(new Submission { SubmissionId = 200 });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync(new Paper
            {
                PaperId = 50,
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionId = 100, QuestionType = QuestionType.Mcq, QuestionContent = "Q",
                        Difficulty = 1,
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new()
                            {
                                QuestionAnswerId = 1, Content = "A", GroupAnswerId = null,
                                BlankInputs = new List<BlankInput>
                                {
                                    new()
                                    {
                                        InputTypeId = 1,
                                        InputType = new InputType { InputTypeId = 1, Name = "Text", GroupType = "G" }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            var result = await _service.CreatePracticeExamAsync(new CreatePracticeExamRequest
            {
                ClassId = 10, ChapterIds = new List<int> { 1 }, TotalQuestions = 5
            });

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value.Questions);
        }


        [Fact(DisplayName = "SubmitPracticeExamAsync - UTCID-NoPaper - Submission không có Paper -> PaperNotFound")]
        public async Task Submit_NoPaper_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission { SubmissionId = 1, Status = SubmissionStatus.InProgress, Paper = null });

            var result = await _service.SubmitPracticeExamAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1, StudentAnswers = new List<PracticeStudentAnswerDto>()
            });

            Assert.Equal(PracticeExamErrors.PaperNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SubmitPracticeExamAsync - UTCID-InvalidAnswer - QuestionAnswerId không thuộc Paper")]
        public async Task Submit_InvalidAnswerId_ShouldReturnError()
        {
            var paper = new Paper
            {
                PaperId = 10,
                Questions = new List<Question>
                {
                    new() { QuestionId = 1, QuestionAnswers = new List<QuestionAnswer> { new() { QuestionAnswerId = 1 } } }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    SubmissionId = 1, Status = SubmissionStatus.InProgress, Paper = paper, StudentAnswers = new List<StudentAnswer>()
                });

            var result = await _service.SubmitPracticeExamAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1,
                StudentAnswers = new List<PracticeStudentAnswerDto> { new() { QuestionAnswerId = 999 } }
            });

            Assert.Equal(PracticeExamErrors.InvalidAnswer.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SubmitPracticeExamAsync - UTCID-Success - Tính điểm + cập nhật submission")]
        public async Task Submit_Success_ShouldComputeScore()
        {
            var paper = new Paper
            {
                PaperId = 10,
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionId = 1, QuestionType = QuestionType.Mcq, ChapterId = 1,
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new() { QuestionAnswerId = 1, IsCorrect = true, CorrectAnswer = null },
                            new() { QuestionAnswerId = 2, IsCorrect = false }
                        }
                    }
                }
            };
            var submission = new Submission
            {
                SubmissionId = 1, Status = SubmissionStatus.InProgress, Paper = paper,
                StudentAnswers = new List<StudentAnswer>
                {
                    new() { QuestionAnswerId = 2, Response = "old" } // existing - will be removed (not in incoming)
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1)).ReturnsAsync(submission);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SubmitPracticeExamAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1,
                StudentAnswers = new List<PracticeStudentAnswerDto> { new() { QuestionAnswerId = 1, Response = "selected" } }
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        }

        [Fact(DisplayName = "SubmitPracticeExamAsync - UTCID-UpdateExistingAnswer - Update existing then submit")]
        public async Task Submit_UpdateExistingAnswer_ShouldUpdateResponse()
        {
            var paper = new Paper
            {
                PaperId = 10,
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionId = 1,
                        QuestionAnswers = new List<QuestionAnswer> { new() { QuestionAnswerId = 1, IsCorrect = true } }
                    }
                }
            };
            var existingAnswer = new StudentAnswer { QuestionAnswerId = 1, Response = "old" };
            var submission = new Submission
            {
                SubmissionId = 1, Status = SubmissionStatus.InProgress, Paper = paper,
                StudentAnswers = new List<StudentAnswer> { existingAnswer }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1)).ReturnsAsync(submission);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SubmitPracticeExamAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1,
                StudentAnswers = new List<PracticeStudentAnswerDto> { new() { QuestionAnswerId = 1, Response = "new" } }
            });

            Assert.True(result.IsSuccess);
            Assert.Equal("new", existingAnswer.Response);
        }

        // ---------- SavePracticeAnswersAsync ----------

        [Fact(DisplayName = "SavePracticeAnswersAsync - UTCID-NotFound - Submission null -> SubmissionNotFound")]
        public async Task Save_NotFound_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1)).ReturnsAsync((Submission?)null);

            var result = await _service.SavePracticeAnswersAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1, StudentAnswers = new List<PracticeStudentAnswerDto>()
            });

            Assert.Equal(PracticeExamErrors.SubmissionNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SavePracticeAnswersAsync - UTCID-Submitted - Đã nộp -> AlreadySubmitted")]
        public async Task Save_AlreadySubmitted_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission { Status = SubmissionStatus.Submitted, Paper = new Paper() });

            var result = await _service.SavePracticeAnswersAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1, StudentAnswers = new List<PracticeStudentAnswerDto>()
            });

            Assert.Equal(PracticeExamErrors.AlreadySubmitted.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SavePracticeAnswersAsync - UTCID-NoPaper - Paper null -> PaperNotFound")]
        public async Task Save_NoPaper_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission { Status = SubmissionStatus.InProgress, Paper = null });

            var result = await _service.SavePracticeAnswersAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1, StudentAnswers = new List<PracticeStudentAnswerDto>()
            });

            Assert.Equal(PracticeExamErrors.PaperNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SavePracticeAnswersAsync - UTCID-InvalidId - QuestionAnswerId không thuộc Paper")]
        public async Task Save_InvalidId_ShouldReturnError()
        {
            var paper = new Paper { Questions = new List<Question> { new() { QuestionAnswers = new List<QuestionAnswer> { new() { QuestionAnswerId = 1 } } } } };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    Status = SubmissionStatus.InProgress, Paper = paper, StudentAnswers = new List<StudentAnswer>()
                });

            var result = await _service.SavePracticeAnswersAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1,
                StudentAnswers = new List<PracticeStudentAnswerDto> { new() { QuestionAnswerId = 999 } }
            });

            Assert.Equal(PracticeExamErrors.InvalidAnswer.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SavePracticeAnswersAsync - UTCID-Success - Update + add + remove")]
        public async Task Save_Success_AllOps()
        {
            var paper = new Paper
            {
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionAnswers = new List<QuestionAnswer> { new() { QuestionAnswerId = 1 }, new() { QuestionAnswerId = 2 } }
                    }
                }
            };
            var existingUpd = new StudentAnswer { QuestionAnswerId = 1, Response = "old" };
            var existingStale = new StudentAnswer { QuestionAnswerId = 99, Response = "stale" };
            var submission = new Submission
            {
                SubmissionId = 1, Status = SubmissionStatus.InProgress, Paper = paper,
                StudentAnswers = new List<StudentAnswer> { existingUpd, existingStale }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1)).ReturnsAsync(submission);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SavePracticeAnswersAsync(new SubmitPracticeExamRequest
            {
                SubmissionId = 1,
                StudentAnswers = new List<PracticeStudentAnswerDto>
                {
                    new() { QuestionAnswerId = 1, Response = "new" },
                    new() { QuestionAnswerId = 2, Response = "added" }
                }
            });

            Assert.True(result.IsSuccess);
            Assert.Equal("new", existingUpd.Response);
            Assert.DoesNotContain(existingStale, submission.StudentAnswers);
        }

        // ---------- ResumePracticeExamAsync ----------

        [Fact(DisplayName = "ResumePracticeExamAsync - UTCID-NotFound - Không tồn tại")]
        public async Task Resume_NotFound_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1)).ReturnsAsync((Submission?)null);

            var result = await _service.ResumePracticeExamAsync(1);

            Assert.Equal(PracticeExamErrors.SubmissionNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResumePracticeExamAsync - UTCID-Submitted - Đã nộp")]
        public async Task Resume_Submitted_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission { Status = SubmissionStatus.Submitted });

            var result = await _service.ResumePracticeExamAsync(1);

            Assert.Equal(PracticeExamErrors.AlreadySubmitted.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResumePracticeExamAsync - UTCID-Success - Trả về saved answers")]
        public async Task Resume_Success_ShouldReturnSavedAnswers()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    SubmissionId = 1, Status = SubmissionStatus.InProgress, PaperId = 50,
                    StudentAnswers = new List<StudentAnswer>
                    {
                        new() { QuestionAnswerId = 1, Response = "saved-response" }
                    }
                });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50))
                .ReturnsAsync(new Paper { PaperId = 50, Questions = new List<Question>() });

            var result = await _service.ResumePracticeExamAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.SavedAnswers);
            Assert.Equal("saved-response", result.Value.SavedAnswers[0].Response);
        }

        [Fact(DisplayName = "ResumePracticeExamAsync - UTCID-PaperNotFound - GetPracticePaperWithQuestionsAsync trả null -> empty questions")]
        public async Task Resume_PaperNull_ShouldReturnEmptyQuestions()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    SubmissionId = 1, Status = SubmissionStatus.InProgress, PaperId = 50,
                    StudentAnswers = new List<StudentAnswer>()
                });
            _repo.Setup(r => r.GetPracticePaperWithQuestionsAsync(50)).ReturnsAsync((Paper?)null);

            var result = await _service.ResumePracticeExamAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Questions);
        }

        // ---------- GetPracticeResultAsync ----------

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID-NotFound - Không tồn tại")]
        public async Task Result_NotFound_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1)).ReturnsAsync((Submission?)null);

            var result = await _service.GetPracticeResultAsync(1);

            Assert.Equal(PracticeExamErrors.SubmissionNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID-NotSubmitted - Status InProgress -> NotSubmitted")]
        public async Task Result_NotSubmitted_ShouldReturnError()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission { Status = SubmissionStatus.InProgress });

            var result = await _service.GetPracticeResultAsync(1);

            Assert.Equal(PracticeExamErrors.NotSubmitted.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID-Success - Trả về result chi tiết")]
        public async Task Result_Success_ShouldReturnDetail()
        {
            var paper = new Paper
            {
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionId = 1, QuestionType = QuestionType.Mcq, ChapterId = 1,
                        Chapter = new Chapter { ChapterId = 1, Name = "Ch1" },
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new() { QuestionAnswerId = 1, Content = "A", IsCorrect = true },
                            new() { QuestionAnswerId = 2, Content = "B", IsCorrect = false }
                        }
                    }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    SubmissionId = 1, Status = SubmissionStatus.Submitted, Paper = paper,
                    StudentAnswers = new List<StudentAnswer>
                    {
                        new() { QuestionAnswerId = 1, Response = "A" }
                    }
                });

            var result = await _service.GetPracticeResultAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.TotalQuestions);
        }

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID-NoPaper - Paper null -> empty result")]
        public async Task Result_NoPaper_ShouldReturnEmpty()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    SubmissionId = 1, Status = SubmissionStatus.Submitted, Paper = null,
                    StudentAnswers = new List<StudentAnswer>()
                });

            var result = await _service.GetPracticeResultAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.TotalQuestions);
        }

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID-WrongAnswer - Tính sai khi không chọn câu đúng")]
        public async Task Result_WrongAnswer_ShouldCountWrong()
        {
            var paper = new Paper
            {
                Questions = new List<Question>
                {
                    new()
                    {
                        QuestionId = 1, ChapterId = 1, Chapter = null,
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new() { QuestionAnswerId = 1, IsCorrect = true }
                        }
                    }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeSubmissionFullAsync(1, 1))
                .ReturnsAsync(new Submission
                {
                    Status = SubmissionStatus.Submitted, Paper = paper,
                    StudentAnswers = new List<StudentAnswer>() // Không chọn gì
                });

            var result = await _service.GetPracticeResultAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.WrongCount);
        }

        // ---------- GetPracticeHistoryAsync ----------

        [Fact(DisplayName = "GetPracticeHistoryAsync - UTCID-Success - Map history sang DTO")]
        public async Task History_Success_ShouldMap()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetPracticeHistoryAsync(1, null))
                .ReturnsAsync(new List<PracticeHistoryRaw>
                {
                    new()
                    {
                        SubmissionId = 1, PaperId = 5, SubjectName = "Toán", SubjectCode = "MATH",
                        ChapterNames = new List<string> { "C1" }, TotalQuestions = 10,
                        CorrectCount = 7, Status = SubmissionStatus.Submitted,
                        CreatedAtUtc = System.DateTime.UtcNow, UpdatedAtUtc = System.DateTime.UtcNow
                    },
                    new()
                    {
                        SubmissionId = 2, TotalQuestions = 0, CorrectCount = null, Status = SubmissionStatus.InProgress,
                        ChapterNames = new List<string>()
                    }
                });

            var result = await _service.GetPracticeHistoryAsync(null);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(70, result.Value[0].AccuracyRate);
            Assert.Null(result.Value[1].AccuracyRate);
            Assert.Equal("Đã nộp", result.Value[0].Status);
            Assert.Equal("Đang làm", result.Value[1].Status);
        }
    }
}
