// ═══════════════════════════════════════════
//  StudentResult — Học sinh
// ═══════════════════════════════════════════

var DOM = {};

document.addEventListener("DOMContentLoaded", async function () {
    // ── Cache DOM 1 lần duy nhất ──
    DOM = {
        root:       document.getElementById("analyticsRoot"),
        loading:    document.getElementById("analyticsLoading"),
        error:      document.getElementById("analyticsError"),
        errorMsg:   document.getElementById("errorMessage"),
        content:    document.getElementById("analyticsContent"),
        title:      document.getElementById("examTitle"),
        scoreSection: document.getElementById("scoreSection"),
        noScoreNotice: document.getElementById("noScoreNotice"),
        recBox:     document.getElementById("recommendationsBox"),
        reviewBox:  document.getElementById("answerReviewBox"),
        
        tplRec:     document.getElementById("tpl-rec-item"),
        tplChapBar: document.getElementById("tpl-chapter-bar"),
        tplDiffBar: document.getElementById("tpl-diff-bar"),
        tplQuestion:document.getElementById("tpl-review-question"),
        tplOption:  document.getElementById("tpl-review-option")
    };

    await window.userReady;

    // ── Kiểm tra đăng nhập & role ──
    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Student) {
        showError("Bạn không có quyền truy cập. Chỉ Học sinh mới được xem kết quả bài làm.");
        return;
    }

    var examId = DOM.root.dataset.examId;
    if (!examId) { showError("Không tìm thấy mã bài thi."); return; }

    apiClient.get("/api/analytics/exam/" + examId + "/student")
        .then(renderStudentResult)
        .catch(function (err) { showError(err.message || "Lỗi không xác định."); });
});

function showError(msg) {
    DOM.loading.hidden = true;
    DOM.error.hidden = false;
    DOM.errorMsg.textContent = msg;
}

// ═══════════════════════════════════════════
//  Render chính
// ═══════════════════════════════════════════
function renderStudentResult(data) {
    DOM.loading.hidden = true;
    DOM.content.hidden = false;
    DOM.title.textContent = "Kết quả: " + (data.examTitle || "N/A");

    if (data.showScore) {
        DOM.scoreSection.hidden = false;
        fillScoreCards(data);
        renderChapterBars(data.chapterStats);
    } else {
        DOM.noScoreNotice.hidden = false;
    }

    renderAnswerReview(data.answerReview, data.showAnswer);

    // Render LaTeX sau khi toàn bộ DOM đã sẵn sàng
    renderMath(DOM.content);
}

// ═══════════════════════════════════════════
//  Điểm — batch update
// ═══════════════════════════════════════════
function fillScoreCards(data) {
    // Basic text
    var map = {
        mainScoreText: data.totalPoints != null ? data.totalPoints : "—",
        correctCount: data.correctCount || 0,
        wrongCount:   data.wrongCount || 0,
        classAvgLabel: data.classAverageScore != null ? data.classAverageScore : "—",
        classMaxLabel: data.classMaxScore != null ? data.classMaxScore : "—"
    };

    Object.keys(map).forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.textContent = map[id];
    });

    // Total questions instances
    var totalEls = document.querySelectorAll(".totalQuestions");
    totalEls.forEach(el => el.textContent = data.totalQuestions || 0);

    // Progress Bars
    var tq = data.totalQuestions || 1; // avoid /0
    var cPct = ((data.correctCount || 0) / tq) * 100;
    var wPct = ((data.wrongCount || 0) / tq) * 100;
    
    var cBar = document.getElementById("correctBar");
    var wBar = document.getElementById("wrongBar");
    if(cBar) cBar.style.width = cPct + "%";
    if(wBar) wBar.style.width = wPct + "%";

    // Circular Score
    var scoreCircle = document.getElementById("scoreCirclePath");
    if(scoreCircle && data.totalPoints != null) {
        // Assume score is out of 10
        var scorePct = (parseFloat(data.totalPoints) / 10.0) * 100;
        scoreCircle.style.strokeDasharray = scorePct + ", 100";
        
        // Color scale
        if (scorePct >= 80) scoreCircle.style.stroke = "var(--clr-success)";
        else if (scorePct >= 50) scoreCircle.style.stroke = "var(--clr-warning)";
        else scoreCircle.style.stroke = "var(--clr-danger)";
    }

    // Rank Delta
    var deltaEl = document.getElementById("rankDelta");
    if (deltaEl && data.totalPoints != null && data.classAverageScore != null) {
        var diff = parseFloat(data.totalPoints) - parseFloat(data.classAverageScore);
        if (diff > 0) {
            deltaEl.textContent = "+" + diff.toFixed(1);
            deltaEl.classList.add("text-success");
        } else if (diff < 0) {
            deltaEl.textContent = diff.toFixed(1);
            deltaEl.classList.add("text-danger");
        } else {
            deltaEl.textContent = "0.0";
        }
    }
}

// ═══════════════════════════════════════════
//  Bars
// ═══════════════════════════════════════════
function renderChapterBars(stats) {
    var container = document.getElementById("chapterBarsContainer");
    if (!container) return;
    container.innerHTML = "";

    if (!stats || stats.length === 0) {
        container.innerHTML = '<div class="text-muted fst-italic">Không có dữ liệu phân tích chương.</div>';
        return;
    }

    stats.forEach(function (s) {
        var el = DOM.tplChapBar.content.cloneNode(true);
        el.querySelector(".chapter-name").textContent = s.chapterName;
        el.querySelector(".chapter-rate").textContent = s.accuracyRate + "%";
        
        var bar = el.querySelector(".chapter-bar");
        bar.style.width = s.accuracyRate + "%";
        
        var rate = parseFloat(s.accuracyRate);
        if(rate >= 80) bar.classList.add("bg-success");
        else if(rate >= 50) bar.classList.add("bg-warning");
        else bar.classList.add("bg-danger");

        container.appendChild(el);
    });
}



// ═══════════════════════════════════════════
//  Xem lại bài làm
// ═══════════════════════════════════════════
function renderAnswerReview(answers, showAnswer) {
    DOM.reviewBox.innerHTML = "";
    if (!answers || answers.length === 0) { 
        DOM.reviewBox.innerHTML = '<div class="text-center py-5 text-muted">Không có dữ liệu chi tiết bài làm.</div>'; 
        return; 
    }

    answers.forEach(function (q) {
        var qBlock = DOM.tplQuestion.content.cloneNode(true);
        var container = qBlock.querySelector(".answer-review-item");

        // Status
        var badge = container.querySelector(".question-result-badge");
        var isCorrect = false;
        var isUnanswered = false;

        if (showAnswer) {
            // Dùng cờ IsQuestionCorrect từ backend nếu có, fallback về check manual (cho an toàn)
            if (q.isQuestionCorrect !== undefined && q.isQuestionCorrect !== null) {
                isCorrect = q.isQuestionCorrect;
            } else if (q.isCorrect !== undefined && q.isCorrect !== null) {
                isCorrect = q.isCorrect; // Dành cho PracticeExam
            } else {
                isCorrect = checkQuestionCorrect(q.options);
            }

            isUnanswered = checkQuestionUnanswered(q.options, q.questionType);

            if (isUnanswered) {
                container.classList.add("wrong");
                badge.textContent = "Chưa trả lời";
            } else {
                container.classList.add(isCorrect ? "correct" : "wrong");
                badge.textContent = isCorrect ? "Trả lời Đúng" : "Trả lời Sai";
            }
        } else {
            badge.textContent = "Đã nộp";
            badge.classList.add("bg-light", "text-dark", "border");
        }

        container.querySelector(".question-order").textContent = "Câu " + q.questionOrder;
        
        var chapterEl = container.querySelector(".question-chapter");
        if (q.chapterName) { chapterEl.textContent = q.chapterName; }
        else { chapterEl.remove(); }

        var stemEl = container.querySelector(".question-content");
        stemEl.innerHTML = `<math-span>${parseQuestionContent(q.questionContent)}</math-span>`;

        var optSlot = container.querySelector("[data-slot='options']");

        if (showAnswer && isUnanswered && q.questionType !== "FillInBlank") {
             var notice = document.createElement("div");
             notice.className = "text-danger fw-bold mb-3 fs-6 p-3 rounded bg-danger bg-opacity-10 border border-danger";
             notice.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-2"></i> Em chưa chọn đáp án nào cho câu hỏi này!';
             optSlot.appendChild(notice);
        }

        q.options.forEach(function (opt) { optSlot.appendChild(buildOptionRow(opt, q.questionType, showAnswer)); });

        DOM.reviewBox.appendChild(qBlock);
    });
}

function buildOptionRow(opt, questionType, showAnswer) {
    var optEl = DOM.tplOption.content.cloneNode(true);
    var row = optEl.querySelector(".option-row");

    if (opt.isSelected) row.classList.add("selected");
    if (showAnswer && opt.isCorrect === true) row.classList.add("correct-answer");
    if (showAnswer && opt.isSelected && opt.isCorrect === false) row.classList.add("wrong-answer");

    var icon = row.querySelector(".option-icon");
    icon.innerHTML = getOptionIconHTML(opt, showAnswer);

    var contentEl = row.querySelector(".option-content");
    contentEl.innerHTML = `<math-span>${opt.content}</math-span>`;

    var responseEl = row.querySelector(".option-response");
    if (questionType === "FillInBlank") {
        if (opt.studentResponse && opt.studentResponse.trim() !== "") {
            responseEl.textContent = "Bạn đã điền: " + opt.studentResponse;
        } else {
            responseEl.textContent = "Bạn chưa điền ô này";
            responseEl.classList.add("text-danger");
        }
    } else { responseEl.remove(); }

    var correctEl = row.querySelector(".option-correct");
    if (showAnswer && opt.correctAnswer && questionType === "FillInBlank") {
        correctEl.textContent = "Đáp án đúng: " + opt.correctAnswer;
    } else { correctEl.remove(); }

    return optEl;
}

// ═══════════════════════════════════════════
//  Utility
// ═══════════════════════════════════════════
function checkQuestionCorrect(options) {
    for (var i = 0; i < options.length; i++) {
        if (options[i].isSelected && options[i].isCorrect === false) return false;
        if (!options[i].isSelected && options[i].isCorrect === true) return false;
    }
    return true;
}

function checkQuestionUnanswered(options, type) {
    if (type === "FillInBlank") {
        return options.every(o => !o.studentResponse || o.studentResponse.trim() === "");
    }
    return options.every(o => !o.isSelected);
}

function renderMath(container) {
    // MathLive tự tìm và render LaTeX trong toàn document
    if (typeof MathLive !== "undefined") {
        MathLive.renderMathInDocument();
    } else {
        window.addEventListener("load", function () {
            if (typeof MathLive !== "undefined") MathLive.renderMathInDocument();
        });
    }
}

function parseQuestionContent(content) {
    try { return JSON.parse(content).stem || content; } catch (e) { return content; }
}

function getOptionIconHTML(opt, showAnswer) {
    if (showAnswer) {
        if (opt.isCorrect === true) return '<i class="bi bi-check-circle-fill text-success"></i>';
        if (opt.isSelected) return '<i class="bi bi-x-circle-fill text-danger"></i>';
        return '<i class="bi bi-circle text-muted"></i>';
    }
    return opt.isSelected ? '<i class="bi bi-circle-fill text-primary"></i>' : '<i class="bi bi-circle text-muted"></i>';
}

function getRecClass(rec) {
    if (rec.includes("🚨")) return "rec-danger";
    if (rec.includes("⚠️")) return "rec-warning";
    if (rec.includes("🌟")) return "rec-success";
    return "rec-info";
}
