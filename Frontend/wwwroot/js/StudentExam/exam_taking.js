let examSignalRConnection = null;

$(document).ready(function () {
    const examId = $('#ExamId').val();
    const paperId = $('#PaperId').val();

    if (!examId || !paperId) {
        Swal.fire('Lỗi', 'Thiếu định danh đề thi!', 'error');
        return;
    }

    // 1. Khởi tạo bài làm (Start Submission)
    // Hệ thống sẽ tự bốc thăm PaperId nếu không gửi lên
    apiClient.post('/api/student/exams/submission/start', { ExamId: parseInt(examId), PaperId: parseInt(paperId) })
        .then(function (res) {
            $('#SubmissionId').val(res.submissionId);
            const assignedPaperId = res.paperId || paperId;
            loadExamPaper(examId, assignedPaperId, res.remainingSeconds, res.savedAnswers);

            // 2. Khởi tạo kết nối SignalR để giám sát
            initSignalRConnection(examId);

            // 3. Khởi tạo Anti-Cheat
            initAntiCheat();
        })
        .catch(function (error) {
            console.error("Start submission error:", error);
            Swal.fire('Lỗi đăng nhập', 'Lỗi khi bắt đầu làm bài. Vui lòng kiểm tra tài khoản.', 'error');
        });
});

function loadExamPaper(examId, paperId, remainingSeconds, savedAnswers) {
    apiClient.get(`/api/student/exams/${examId}/paper/${paperId}`)
        .then(function (paper) {
            renderExamUI(paper, remainingSeconds, savedAnswers);
        })
        .catch(function (error) {
            console.error("Load paper error:", error);
            Swal.fire('Không thể tìm thấy', 'Không thể tải hệ thống đề thi.', 'error');
        });
}

// Simple Seeded PRNG
function mulberry32(a) {
    return function () {
        var t = a += 0x6D2B79F5;
        t = Math.imul(t ^ t >>> 15, t | 1);
        t ^= t + Math.imul(t ^ t >>> 7, t | 61);
        return ((t ^ t >>> 14) >>> 0) / 4294967296;
    }
}

// Helper function to shuffle an array consistently based on a seed
function shuffleArray(array, seed) {
    const randomFunc = mulberry32(seed);
    let currentIndex = array.length, randomIndex;
    while (currentIndex !== 0) {
        randomIndex = Math.floor(randomFunc() * currentIndex);
        currentIndex--;
        [array[currentIndex], array[randomIndex]] = [array[randomIndex], array[currentIndex]];
    }
    return array;
}

function renderExamUI(paper, remainingSeconds, savedAnswers) {
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

    // 1. Lấy ID bài làm hiện tại làm Hạt giống (Seed) để trộn đề
    // Cách này giúp 10 sinh viên có 10 thứ tự khác nhau, nhưng 1 sinh viên reload lại trang vẫn giữ nguyên thứ tự cũ
    const currentSubmissionId = parseInt($('#SubmissionId').val()) || paper.paperId;
    let shuffledQuestions = [...paper.questions];
    shuffleArray(shuffledQuestions, currentSubmissionId);

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

        if (q.questionType === 'MultipleChoice') {
            try {
                if (q.answer) {
                    multipleChoiceData = JSON.parse(q.answer); // ["...", "..."] or {"opts": [...], "correct": "..."} 
                }
            } catch (e) {
                console.error("Lỗi parse MultipleChoice JSON:", e);
            }
        }
        else if (q.questionType === 'StepByStep') {
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
            // 2. Không tráo ngẫu nhiên các bước Step-by-Step
            const shuffledSteps = [...stepByStepData];
            // shuffleArray(shuffledSteps); // Đã tắt để giữ đúng thứ tự các bước

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
        else if (q.questionType === 'MultipleChoice') {

            let mcOpts = [];

            if (multipleChoiceData) {
                let optionsArray = null;
                if (Array.isArray(multipleChoiceData)) {
                    optionsArray = multipleChoiceData;
                } else if (multipleChoiceData.opts && Array.isArray(multipleChoiceData.opts)) {
                    optionsArray = multipleChoiceData.opts;
                }

                if (optionsArray && optionsArray.length >= 4) {
                    mcOpts = [
                        { id: 'A', text: optionsArray[0] || '' },
                        { id: 'B', text: optionsArray[1] || '' },
                        { id: 'C', text: optionsArray[2] || '' },
                        { id: 'D', text: optionsArray[3] || '' }
                    ];
                }
            }

            if (mcOpts.length === 0) {
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
        if (q.questionType === 'MultipleChoice') typeLabel = '(Trắc nghiệm)';
        else if (q.questionType === 'StepByStep') typeLabel = '(Từng bước)';
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

    // Start countdown timer from server calculation
    startCountdownTimer(remainingSeconds);

    // Fill saved answers
    if (savedAnswers && savedAnswers.length > 0) {
        savedAnswers.forEach(ans => {
            const rowIdx = ans.questionIndex;
            const block = $(`#question-index-${rowIdx}`);
            if (block.length > 0) {
                const inputs = block.find('.answer-field');
                let isAnswered = false;

                if (inputs.attr('type') === 'radio') {
                    inputs.each(function () {
                        if ($(this).val() === ans.responseText) {
                            $(this).prop('checked', true);
                            isAnswered = true;
                        }
                    });
                } else if (inputs.length === 1 && inputs[0].tagName.toLowerCase() === 'math-field') {
                    // For single short-answer
                    inputs[0].value = ans.responseText;
                    isAnswered = true;
                } else if (inputs.length > 1 && inputs[0].tagName.toLowerCase() === 'math-field') {
                    // For StepByStep
                    try {
                        let stepAnswers = JSON.parse(ans.responseText);
                        inputs.each(function () {
                            let sIdx = $(this).data('step');
                            if (sIdx && stepAnswers[`step${sIdx}`]) {
                                this.value = stepAnswers[`step${sIdx}`];
                                isAnswered = true;
                            }
                        });
                    } catch (e) {
                        console.error("Error parsing saved StepByStep answers:", e);
                    }
                }

                if (isAnswered) {
                    const qId = block.data('question-id');
                    $(`#nav-btn-${qId}`).removeClass('btn-outline-primary').addClass('btn-primary');
                }
            }
        });
    }

    $('#loadingSpinner').hide();
    $('#examWorkspace').show();

    // Start 5-minute Auto-Save interval
    startBatchAutoSave(300000); // 300,000 ms = 5 minutes
}

let pendingSaves = {};
let batchAutoSaveInterval;
let autoSaveDebounceTimer;

function savePendingBatch() {
    const questionIndicesToSave = Object.keys(pendingSaves);
    if (questionIndicesToSave.length === 0) {
        return Promise.resolve();
    }

    $('#autoSaveStatus').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang đẩy lên máy chủ...').removeClass('text-success text-danger').addClass('text-warning');

    const submissionId = $('#SubmissionId').val();
    const bulkData = questionIndicesToSave.map(qIndex => {
        return {
            QuestionIndex: parseInt(qIndex),
            ResponseText: pendingSaves[qIndex].responseText
        };
    });

    return apiClient.post(`/api/student/exams/submission/${submissionId}/answers/batch`, bulkData)
        .then(() => {
            const now = new Date();
            const timeString = `lúc ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`;
            $('#autoSaveStatus').html(`✅ Đã lưu tự động an toàn <strong>${timeString}</strong>.`).removeClass('text-warning text-danger text-muted').addClass('text-success');

            // Update UI and clear from buffer ONLY the ones we just saved
            questionIndicesToSave.forEach(qIndex => {
                if (pendingSaves[qIndex]) {
                    $(`#nav-btn-${pendingSaves[qIndex].questionId}`).removeClass('btn-warning btn-danger').addClass('btn-primary');
                    delete pendingSaves[qIndex];
                }
            });
        })
        .catch(err => {
            console.error('Batch Save error', err);
            $('#autoSaveStatus').html('❌ Lỗi kết nối! Hệ thống sẽ thử lại.').removeClass('text-success text-warning').addClass('text-danger');
            questionIndicesToSave.forEach(qIndex => {
                if (pendingSaves[qIndex]) {
                    $(`#nav-btn-${pendingSaves[qIndex].questionId}`).removeClass('btn-warning btn-primary').addClass('btn-danger');
                }
            });
            throw err;
        });
}

function startBatchAutoSave(intervalMs) {
    clearInterval(batchAutoSaveInterval);
    batchAutoSaveInterval = setInterval(() => {
        savePendingBatch().catch(() => { });
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
            Swal.fire({
                title: 'Hết giờ làm bài!',
                text: 'Hệ thống đang tự động thu bài của bạn.',
                icon: 'info',
                timer: 3000,
                showConfirmButton: false
            }).then(() => {
                forceSubmitExam();
            });
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
    $('#autoSaveStatus').html('⏳ Đã ghi nhận thay đổi (Đang đợi vài giây để đẩy lên máy chủ...)').removeClass('text-success text-danger text-muted').addClass('text-warning');

    // Mới cập nhật: Dùng debounce để tự động gửi dữ liệu lên server sau khi học viên ngưng thao tác 3 giây
    clearTimeout(autoSaveDebounceTimer);
    autoSaveDebounceTimer = setTimeout(() => {
        savePendingBatch().catch(() => { });
    }, 3000); // 3 giây
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
        Swal.fire('Chú ý', 'Bạn còn câu hỏi chưa trả lời! Xin kiểm tra các ô màu đỏ trên màn hình.', 'warning');
    } else {
        Swal.fire('Tuyệt vời', 'Bạn đã điền đầy đủ các câu hỏi.', 'success');
    }
}

function flushPendingSaves() {
    return savePendingBatch().catch(err => {
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
    Swal.fire({
        title: 'Xác nhận nộp bài',
        text: "Bạn có chắc chắn muốn nộp bài? Hành động này không thể hoàn tác.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Có, Nộp bài luôn!',
        cancelButtonText: 'Không nộp'
    }).then((result) => {
        if (result.isConfirmed) {
            clearInterval(countdownTimerInterval); // Dừng đồng hồ đếm ngược
            Swal.fire({
                title: 'Hệ thống đang thu bài...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });
            flushPendingSaves().then(() => {
                apiClient.post(`/api/student/exams/submission/${submissionId}/submit`)
                    .then(() => {
                        if (examSignalRConnection) examSignalRConnection.stop();
                        Swal.fire('Thành công', 'Nộp bài thành công!', 'success').then(() => {
                            window.location.href = '/Home/Index'; // Redirect to Dashboard
                        });
                    })
                    .catch(err => {
                        console.error('Submit error:', err);
                        Swal.fire('Lỗi thao tác', 'Lỗi khi nộp bài: ' + err, 'error');
                    });
            }).catch(err => {
                Swal.fire('Gian đoạn thu bài lỗi', 'Không thể hoàn tất do mất kết nối', 'error');
            });
        }
    });
}

// ----------------------------------------------------------------------------
// Tích hợp Giám sát thời gian thực (Realtime Proctoring) qua SignalR
// ----------------------------------------------------------------------------
function initSignalRConnection(examId) {
    if (typeof signalR === 'undefined') {
        console.warn("SignalR library not loaded.");
        return;
    }

    const token = getToken();
    let studentId = 0;
    let studentName = "Học sinh";

    // Parse JWT để lấy thông tin Student gửi cho cơ sở dữ liệu giám sát
    if (token) {
        const payload = parseJwt(token);
        if (payload) {
            studentId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload['nameid'] || payload.sub || 0;
            studentName = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload['name'] || payload.email || 'Học sinh';
            studentId = parseInt(studentId);
        }
    }

    examSignalRConnection = new signalR.HubConnectionBuilder()
        .withUrl(API_BASE_URL + "/examHub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    examSignalRConnection.start()
        .then(() => {
            console.log("🔥 Đã kết nối SignalR Giám sát bài thi!");
            // Gọi hàm trên server để đăng ký phiên làm bài
            examSignalRConnection.invoke("JoinExamGroup", parseInt(examId), studentId, studentName)
                .catch(err => console.error("Lỗi JoinExamGroup: " + err.toString()));
        })
        .catch(err => console.error("SignalR Connection Error: ", err));
}

// ----------------------------------------------------------------------------
// Tích hợp Chống gian lận Client-Side (Anti-Cheat Kit)
// ----------------------------------------------------------------------------
let cheatWarnings = 0;
const MAX_CHEAT_WARNINGS = 3;

function initAntiCheat() {
    // 1. Chặn chuột phải
    document.addEventListener("contextmenu", function (e) {
        e.preventDefault();
    });

    // 2. Chặn các hành động Copy, Cut, Paste
    document.addEventListener("copy", function (e) {
        e.preventDefault();
        Swal.fire('Hành động mờ ám', 'Tính năng Copy bị vô hiệu hóa trong phòng thi!', 'error');
    });
    document.addEventListener("cut", function (e) {
        e.preventDefault();
        Swal.fire('Hành động mờ ám', 'Tính năng Cut bị vô hiệu hóa trong phòng thi!', 'error');
    });
    document.addEventListener("paste", function (e) {
        e.preventDefault();
        Swal.fire('Hành động mờ ám', 'Tính năng Paste bị vô hiệu hóa trong phòng thi!', 'error');
    });

    // 3. Chặn phím F12 (Dev Tools) và các cụm phím tắt phổ biến
    document.addEventListener("keydown", function (e) {
        // F12
        if (e.key === "F12" || e.keyCode === 123) {
            e.preventDefault();
        }
        // Ctrl+Shift+I, Ctrl+Shift+J, Ctrl+Shift+C
        if (e.ctrlKey && e.shiftKey && (e.key === "I" || e.key === "J" || e.key === "C" || e.keyCode === 73 || e.keyCode === 74 || e.keyCode === 67)) {
            e.preventDefault();
        }
        // Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+P
        if (e.ctrlKey && (e.key === "c" || e.key === "C" || e.key === "v" || e.key === "V" || e.key === "x" || e.key === "X" || e.key === "p" || e.key === "P")) {
            e.preventDefault();
        }
    });

    // 4. Phát hiện chuyển Tab / Rời khỏi cửa sổ (Blur Event)
    let isForcingSubmit = false;
    window.addEventListener('blur', function () {
        // Nếu đang trong quá trình nộp cấm hiện thêm cảnh báo
        if (isForcingSubmit || !examSignalRConnection || $('#examWorkspace').is(':hidden')) return;

        // Khóa không cho đếm thêm sự kiện blur nếu đang hiện thông báo
        if (window.isAlerting) return;

        cheatWarnings++;

        if (cheatWarnings >= MAX_CHEAT_WARNINGS) {
            isForcingSubmit = true; // Block các sự kiện blur tiếp theo
            // Block giao diện ngay lập tức mà không dùng alert (vì alert gây nghẽn luồng)
            $('#examWorkspace').hide();
            $('#questionNavMap').parent().hide();
            $('body').html('<h2 style="text-align:center; margin-top: 20%; color: red;">Cảnh báo cấp độ cao nhất! Lỗi vi phạm chuyển màn hình. Hệ thống đang thu bài tự động...</h2>');

            forceSubmitExam();
        } else {
            window.isAlerting = true;
            Swal.fire({
                title: `CẢNH BÁO VI PHẠM (${cheatWarnings}/${MAX_CHEAT_WARNINGS})`,
                text: `Bạn vừa rời khỏi màn hình làm bài! Hệ thống có ghi nhận hành vi này. Nếu vi phạm vượt mức ${MAX_CHEAT_WARNINGS} lần sẽ tự động thu bài.`,
                icon: 'warning',
                confirmButtonText: 'Tôi đã hiểu',
                willClose: () => {
                    // Đợi giao diện ổn định lại rồi mới nhả khóa
                    setTimeout(() => { window.isAlerting = false; }, 500);
                }
            });
        }
    });
}
