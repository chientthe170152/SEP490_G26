$(document).ready(function () {
    const examId = $('#ExamId').val();
    const paperId = $('#PaperId').val();

    if (!examId || !paperId) {
        alert("Thiếu định danh đề thi!");
        return;
    }

    // 1. Khởi tạo bài làm (Start Submission)
    // Hệ thống sẽ tự bốc thăm PaperId nếu không gửi lên
    apiClient.post('/api/student/exams/submission/start', { ExamId: parseInt(examId), PaperId: parseInt(paperId) })
        .then(function (res) {
            $('#SubmissionId').val(res.submissionId);
            const assignedPaperId = res.paperId || paperId;
            loadExamPaper(examId, assignedPaperId);
        })
        .catch(function (error) {
            console.error("Start submission error:", error);
            alert("Lỗi khi bắt đầu làm bài. Vui lòng kiểm tra đăng nhập.");
        });
});

function loadExamPaper(examId, paperId) {
    apiClient.get(`/api/student/exams/${examId}/paper/${paperId}`)
        .then(function (paper) {
            renderExamUI(paper);
        })
        .catch(function (error) {
            console.error("Load paper error:", error);
            alert("Không thể tải đề thi.");
        });
}

// Helper function to shuffle an array (Fisher-Yates)
function shuffleArray(array) {
    let currentIndex = array.length, randomIndex;
    while (currentIndex !== 0) {
        randomIndex = Math.floor(Math.random() * currentIndex);
        currentIndex--;
        [array[currentIndex], array[randomIndex]] = [array[randomIndex], array[currentIndex]];
    }
    return array;
}

function renderExamUI(paper) {
    $('#examTitle').text(paper.title);
    $('#examSubtitle').text(paper.description || 'Sinh viên đang làm bài tự động lưu');

    // Display Paper Code
    if (paper.code) {
        $('#paperCodeDisplay').text(paper.code);
    } else {
        $('#paperCodeDisplay').text('N/A');
    }

    const container = $('#questionContainer');
    const navMap = $('#questionNavMap');
    container.empty();
    navMap.empty();

    // 1. Shuffle Questions array so each student gets different order
    let shuffledQuestions = [...paper.questions];
    shuffleArray(shuffledQuestions);

    shuffledQuestions.forEach((q, index) => {
        const qIndex = index + 1;

        // Build Navigation Button
        const navBtn = `<button class="btn btn-sm btn-outline-primary question-nav-btn" id="nav-btn-${q.questionId}" onclick="goToQuestion(${qIndex})">${qIndex}</button>`;
        navMap.append(navBtn);

        let answerAreaHtml = '';
        let displayContent = q.contentLatex || '';
        let optA = 'A', optB = 'B', optC = 'C', optD = 'D';

        // Parse any JSON attached to the question
        let multipleChoiceData = null;
        let stepByStepData = null;

        if (q.questionType === 'MultipleChoice' || q.questionType === 'Trắc nghiệm') {
            try {
                if (q.answer) {
                    multipleChoiceData = JSON.parse(q.answer); // {"type": "multiple", "opts": [...], "correct": "..."} 
                }
            } catch (e) {
                console.error("Lỗi parse MultipleChoice JSON:", e);
            }
        }
        else if (q.questionType === 'StepByStep' || q.questionType === 'Từng bước') {
            try {
                if (q.answer) {
                    stepByStepData = JSON.parse(q.answer); // [{"s": 1, "a": "...", "h": "..."}]
                }
            } catch (e) {
                console.error("Lỗi parse StepByStep JSON:", e);
            }
        }

        // Handle Step-by-Step format
        if (stepByStepData && Array.isArray(stepByStepData)) {
            // 2. Shuffle Step-by-Step questions steps
            const shuffledSteps = [...stepByStepData];
            shuffleArray(shuffledSteps);

            let stepHtml = '';
            shuffledSteps.forEach((stepObj, idx) => {
                let s = stepObj.s || stepObj.step || stepObj.Step;
                let hint = stepObj.h || stepObj.hint || stepObj.Hint;
                let correctAnswer = stepObj.a || stepObj.answer || stepObj.Answer || '';

                // Escape quotes for inserting into HTML attribute
                let escapedAnswer = correctAnswer.toString().replace(/"/g, '&quot;').replace(/'/g, '&#39;');

                stepHtml += `
                    <div class="step-card mb-3 p-3 border rounded bg-light" id="step-${q.questionId}-${s}" style="box-shadow: 0 0.125rem 0.25rem rgba(0,0,0,0.075);">
                        <h6 class="fw-bold text-primary mb-2">Bước ${s}:</h6>
                        <!-- Mốc để học sinh điền kết quả vào -->
                        <label class="form-label mt-2">Trả lời bước ${s}:</label>
                        <math-field class="math-input answer-field mb-2" id="input-${q.questionId}-${s}" data-qid="${q.questionId}" data-step="${s}" oninput="autoSaveAnswer(${q.questionId}, this.value, '${s}')"></math-field>
                `;
                if (hint) {
                    stepHtml += `
                        <div class="alert alert-secondary py-2 mt-2 mb-0" style="font-size: 0.85rem;">
                            <strong>💡 Gợi ý bước ${s}:</strong> ${hint}
                        </div>
                    `;
                }
                stepHtml += `</div>`;
            });
            answerAreaHtml = stepHtml;
        }
        // Handle MultipleChoice format
        else if (q.questionType === 'MultipleChoice' || q.questionType === 'Trắc nghiệm') {

            let mcOpts = [];

            if (multipleChoiceData && multipleChoiceData.opts) {
                // The DB stores MultipleChoice as JSON
                mcOpts = [
                    { id: 'A', text: multipleChoiceData.opts[0] || '' },
                    { id: 'B', text: multipleChoiceData.opts[1] || '' },
                    { id: 'C', text: multipleChoiceData.opts[2] || '' },
                    { id: 'D', text: multipleChoiceData.opts[3] || '' }
                ];
            } else {
                // Fallback Regex
                const rex = /(.*?)(?:A\.|A\))(.*?)(?:B\.|B\))(.*?)(?:C\.|C\))(.*?)(?:D\.|D\))(.*)/is;
                const match = displayContent.match(rex);

                if (match && match.length === 6) {
                    displayContent = match[1].trim();
                    mcOpts = [
                        { id: 'A', text: match[2].trim() },
                        { id: 'B', text: match[3].trim() },
                        { id: 'C', text: match[4].trim() },
                        { id: 'D', text: match[5].trim() }
                    ];
                }
            }

            // 3. Shuffle Multiple Choice Options if array is populated
            if (mcOpts.length > 0) {
                shuffleArray(mcOpts);

                // Build answerAreaHtml using shuffled options
                const letters = ['A', 'B', 'C', 'D'];
                mcOpts.forEach((opt, idx) => {
                    const displayLetter = letters[idx];
                    // IMPORTANT: The value attribute is still opt.id (A,B,C,D) to map back to the real correct answer in DB.
                    // The student sees A, B, C, D visually mapped to the new shuffled text.
                    answerAreaHtml += `
                        <div class="form-check mb-2 d-flex align-items-center">
                            <input class="form-check-input answer-field me-2" type="radio" name="q-${q.questionId}" id="q${q.questionId}${displayLetter}" value="${opt.id}" data-qid="${q.questionId}" onchange="autoSaveAnswer(${q.questionId}, this.value)">
                            <label class="form-check-label w-100" for="q${q.questionId}${displayLetter}">
                                <math-field class="math-input math-display" read-only style="border:none !important; background:transparent !important; min-height:auto;">${displayLetter}. ${opt.text}</math-field>
                            </label>
                        </div>
                    `;
                });
            } else {
                answerAreaHtml = `Lỗi hiển thị đáp án: không tìm thấy JSON hoặc Text không đúng chuẩn.`;
            }
        } else {
            answerAreaHtml = `
                <math-field class="math-input answer-field" data-qid="${q.questionId}" oninput="autoSaveAnswer(${q.questionId}, this.value)"></math-field>
            `;
        }

        let typeLabel = '(Tự luận)';
        if (q.questionType === 'MultipleChoice' || q.questionType === 'Trắc nghiệm') typeLabel = '(Trắc nghiệm)';
        else if (q.questionType === 'StepByStep' || q.questionType === 'Từng bước') typeLabel = '(Từng bước)';
        else if (q.questionType === 'ShortAnswer' || q.questionType === 'Trả lời ngắn') typeLabel = '(Trả lời ngắn)';
        else if (q.questionType) typeLabel = `(${q.questionType})`;

        // Build Question HTML (Pagination style)
        const displayStyle = qIndex === 1 ? 'block' : 'none';
        const html = `
          <section class="question-block" id="question-index-${qIndex}" data-question-id="${q.questionId}" data-question-index="${qIndex}" style="display: ${displayStyle}">
            <h2 class="question-title">Câu ${qIndex} ${typeLabel}</h2>
            <div class="mb-2">
                <!-- Content Latex is normally displayed by MathLive or MathJax -->
                <math-field class="math-input math-display mb-2" read-only>${displayContent}</math-field>
            </div>
            <label class="form-label">Phần trả lời của bạn:</label>
            <div>
                ${answerAreaHtml}
            </div>
          </section>
        `;
        container.append(html);
    });

    // Append Pagination Controls
    const paginationHtml = `
      <div class="d-flex justify-content-between mt-4 pt-3 border-top" id="pagination-controls">
         <button class="btn btn-secondary px-4" id="btn-prev-question" onclick="goToQuestion(currentQuestionIndex - 1)" disabled>
           &laquo; Câu trước
         </button>
         <button class="btn btn-primary px-4" id="btn-next-question" onclick="goToQuestion(currentQuestionIndex + 1)">
           Câu tiếp theo &raquo;
         </button>
      </div>
    `;
    container.append(paginationHtml);

    // Initial Nav Button Update
    updateNavButtonsStyling();

    // Start countdown timer
    const durationInMinutes = paper.duration || 40;
    const initTimerSeconds = durationInMinutes * 60;
    startCountdownTimer(initTimerSeconds);

    $('#loadingSpinner').hide();
    $('#examWorkspace').show();

    // Start 5-minute Auto-Save interval
    startBatchAutoSave(300000); // 300,000 ms = 5 minutes
}

let pendingSaves = {};
let batchAutoSaveInterval;

function startBatchAutoSave(intervalMs) {
    clearInterval(batchAutoSaveInterval);
    batchAutoSaveInterval = setInterval(() => {
        const questionIndicesToSave = Object.keys(pendingSaves);
        if (questionIndicesToSave.length > 0) {
            $('#autoSaveStatus').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang đẩy lên máy chủ...').removeClass('text-success text-danger').addClass('text-warning');

            const submissionId = $('#SubmissionId').val();
            const bulkData = questionIndicesToSave.map(qIndex => {
                return {
                    QuestionIndex: parseInt(qIndex),
                    ResponseText: pendingSaves[qIndex].responseText
                };
            });

            apiClient.post(`/api/student/exams/submission/${submissionId}/answers/batch`, bulkData)
                .then(() => {
                    const now = new Date();
                    const timeString = `lúc ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`;
                    $('#autoSaveStatus').html(`✅ Đã lưu tự động an toàn <strong>${timeString}</strong>.`).removeClass('text-warning text-danger text-muted').addClass('text-success');

                    // Update UI and clear from buffer ONLY the ones we just saved
                    questionIndicesToSave.forEach(qIndex => {
                        $(`#nav-btn-${pendingSaves[qIndex].questionId}`).removeClass('btn-warning btn-danger').addClass('btn-primary');
                        delete pendingSaves[qIndex];
                    });
                })
                .catch(err => {
                    console.error('Batch Save error', err);
                    $('#autoSaveStatus').html('❌ Lỗi kết nối! 5 phút sau sẽ thử lại.').removeClass('text-success text-warning').addClass('text-danger');
                    questionIndicesToSave.forEach(qIndex => {
                        $(`#nav-btn-${pendingSaves[qIndex].questionId}`).removeClass('btn-warning btn-primary').addClass('btn-danger');
                    });
                });
        }
    }, intervalMs);
}

let currentQuestionIndex = 1;
let totalQuestions = 0;
let countdownTimerInterval;

function startCountdownTimer(totalSeconds) {
    clearInterval(countdownTimerInterval);
    let remaining = totalSeconds;

    function updateDisplay() {
        let m = Math.floor(remaining / 60);
        let s = remaining % 60;
        let pM = m < 10 ? '0' + m : m;
        let pS = s < 10 ? '0' + s : s;
        $('#countdownTimer').text(`${pM}:${pS}`);

        if (remaining <= 300) { // < 5 mins
            $('#countdownTimer').addClass('text-danger font-weight-bold');
        }
    }

    updateDisplay();

    countdownTimerInterval = setInterval(() => {
        remaining--;
        if (remaining <= 0) {
            clearInterval(countdownTimerInterval);
            $('#countdownTimer').text('00:00');
            alert('Đã hết thời gian làm bài! Hệ thống tự động nộp bài.');
            forceSubmitExam();
        } else {
            updateDisplay();
        }
    }, 1000);
}

function goToQuestion(targetIndex) {
    totalQuestions = $('.question-block').length;
    if (targetIndex < 1 || targetIndex > totalQuestions) return;

    // Hide current
    $(`#question-index-${currentQuestionIndex}`).hide();

    // Show new
    currentQuestionIndex = targetIndex;
    $(`#question-index-${currentQuestionIndex}`).fadeIn(200);

    // Update Prev/Next buttons
    $('#btn-prev-question').prop('disabled', currentQuestionIndex === 1);

    if (currentQuestionIndex === totalQuestions) {
        $('#btn-next-question').removeClass('btn-primary').addClass('btn-secondary').prop('disabled', true);
    } else {
        $('#btn-next-question').removeClass('btn-secondary').addClass('btn-primary').prop('disabled', false);
    }

    updateNavButtonsStyling();
}

function updateNavButtonsStyling() {
    $('.question-nav-btn').each(function () {
        const txt = parseInt($(this).text());
        if (txt === currentQuestionIndex) {
            $(this).addClass('active').css('border-width', '2px');
        } else {
            $(this).removeClass('active').css('border-width', '1px');
        }
    });
}

// Timeout holder for autosave debouncing (removed in favor of batch save)
function autoSaveAnswer(questionId, value, stepIndex = null) {
    const qBlock = $(`#question-index-${currentQuestionIndex}`);
    const qIndex = qBlock.data('question-index');

    let responseTextToSave = value;

    if (stepIndex !== null) {
        let stepAnswers = {};
        qBlock.find('.answer-field').each(function () {
            let sIdx = $(this).data('step');
            if (sIdx) {
                stepAnswers[`step${sIdx}`] = this.value;
            }
        });
        responseTextToSave = JSON.stringify(stepAnswers);
    }

    // Queue for batch save instead of sending immediately
    pendingSaves[qIndex] = {
        questionId: questionId,
        responseText: responseTextToSave
    };

    // Immediate Visual Feedback that changes are pending
    $(`#nav-btn-${questionId}`).removeClass('btn-outline-primary btn-primary btn-danger').addClass('btn-warning');
    $('#autoSaveStatus').html('⏳ Đã ghi nhận thay đổi (Đợi đến lượt tự động lưu)').removeClass('text-success text-danger text-muted').addClass('text-warning');
}

function highlightUnanswered() {
    let hasUnanswered = false;

    // Check all questions loaded in the DOM
    $('.question-block').each(function () {
        const qid = $(this).data('question-id');
        let isAnswered = false;

        // Find input inside this block (radio or math-field)
        const inputs = $(this).find('.answer-field');

        if (inputs.attr('type') === 'radio') {
            // Check if any radio is checked
            if ($(this).find('.answer-field:checked').length > 0) {
                isAnswered = true;
            }
        } else {
            // Check if math-field has value
            const mathVal = inputs.prop('value');
            if (mathVal && mathVal.trim() !== '') {
                isAnswered = true;
            }
        }

        if (!isAnswered) {
            $(this).css('border', '2px solid red');
            hasUnanswered = true;
        } else {
            $(this).css('border', 'none');
        }
    });

    if (hasUnanswered) {
        alert("Bạn còn câu hỏi chưa trả lời! Xin kiểm tra các ô màu đỏ.");
    } else {
        alert("Bạn đã điền đầy đủ các câu hỏi.");
    }
}

function flushPendingSaves() {
    const questionIndicesToSave = Object.keys(pendingSaves);
    if (questionIndicesToSave.length === 0) {
        return Promise.resolve();
    }
    const submissionId = $('#SubmissionId').val();
    const bulkData = questionIndicesToSave.map(qIndex => {
        return {
            QuestionIndex: parseInt(qIndex),
            ResponseText: pendingSaves[qIndex].responseText
        };
    });

    return apiClient.post(`/api/student/exams/submission/${submissionId}/answers/batch`, bulkData).then(() => {
        // Clear saved answers
        questionIndicesToSave.forEach(qIndex => delete pendingSaves[qIndex]);
    }).catch(err => {
        console.error('Lỗi khi lưu câu trả lời trước khi nộp:', err);
        throw err;
    });
}

function forceSubmitExam() {
    const submissionId = $('#SubmissionId').val();
    flushPendingSaves().then(() => {
        apiClient.post(`/api/student/exams/submission/${submissionId}/submit`)
            .then(() => {
                window.location.href = '/Home/Index';
            })
            .catch(err => {
                console.error('Submit error:', err);
                window.location.href = '/Home/Index';
            });
    });
}

function submitExam() {
    const submissionId = $('#SubmissionId').val();
    if (confirm("Bạn có chắc chắn muốn nộp bài? Hành động này không thể hoàn tác.")) {
        flushPendingSaves().then(() => {
            apiClient.post(`/api/student/exams/submission/${submissionId}/submit`)
                .then(() => {
                    alert("Nộp bài thành công!");
                    window.location.href = '/Home/Index'; // Redirect to Dashboard
                })
                .catch(err => {
                    console.error('Submit error:', err);
                    alert("Lỗi khi nộp bài.");
                });
        });
    }
}
