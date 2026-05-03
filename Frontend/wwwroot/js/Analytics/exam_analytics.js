// ═══════════════════════════════════════════
//  ExamAnalytics — Giáo viên (Thực chiến)
// ═══════════════════════════════════════════

var DOM = {};

document.addEventListener("DOMContentLoaded", async function () {
    // ── Cache DOM ──
    DOM = {
        root:       document.getElementById("analyticsRoot"),
        loading:    document.getElementById("analyticsLoading"),
        error:      document.getElementById("analyticsError"),
        errorMsg:   document.getElementById("errorMessage"),
        content:    document.getElementById("analyticsContent"),
        title:      document.getElementById("examTitle"),
        
        // Zone 1
        avgScore:   document.getElementById("avgScore"),
        passRate:   document.getElementById("passRate"),
        failRate:   document.getElementById("failRate"),
        
        // Zone 2
        weakList:   document.getElementById("weakestChaptersList"),
        hardTable:  document.getElementById("hardestTable").querySelector("tbody"),
        hardCount:  document.getElementById("hardQuestionCount"),
        
        // Zone 3
        atRiskTable: document.getElementById("atRiskTable").querySelector("tbody"),
        atRiskCount: document.getElementById("atRiskCount"),
        excTable:    document.getElementById("excellentTable").querySelector("tbody"),
        excCount:    document.getElementById("excellentCount"),

        // Templates
        tplHardest:  document.getElementById("tpl-hardest-row"),
        tplStudent:  document.getElementById("tpl-student-row"),
        // Alerts
        systemAlert: document.getElementById("systemAlert"),
        dashboard:   document.getElementById("analyticsDashboard"),
        tplWeak:     document.getElementById("tpl-weak-chapter")
    };

    await window.userReady;

    if (!isAuthenticated()) { window.location.href = '/Auth/Login'; return; }
    if (getUserRole() !== RoleIds.Teacher) { showError("Bạn không có quyền truy cập. Chỉ Giáo viên mới được xem phân tích bài thi."); return; }

    var examId = DOM.root.dataset.examId;
    if (!examId) { showError("Không tìm thấy mã bài thi."); return; }

    apiClient.get("/api/analytics/exam/" + examId + "/detail")
        .then(renderAnalytics)
        .catch(function (err) { showError(err.message || "Lỗi không xác định."); });
});

// ═══════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════
function getCSSColor(varName, alpha) {
    var val = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    if (val.startsWith('hsl')) {
        return val.replace('hsl(', 'hsla(').replace(')', ',' + (alpha || 1) + ')');
    }
    if (val.startsWith('rgb')) {
        return val.replace('rgb(', 'rgba(').replace(')', ',' + (alpha || 1) + ')');
    }
    return val;
}

function showError(msg) {
    DOM.loading.hidden = true;
    DOM.error.hidden = false;
    DOM.errorMsg.textContent = msg;
}

// ═══════════════════════════════════════════
//  Render Chính
// ═══════════════════════════════════════════
function renderAnalytics(rawData) {
    if (!rawData) { showError("Hệ thống trả về dữ liệu rỗng."); return; }

    var data = {
        examTitle:          rawData.examTitle || rawData.ExamTitle || "N/A",
        totalSubmissions:   rawData.totalSubmissions != null ? rawData.totalSubmissions : (rawData.TotalSubmissions || 0),
        averageScore:       rawData.averageScore || rawData.AverageScore || 0,
        scoreDistribution:  rawData.scoreDistribution || rawData.ScoreDistribution,
        chapterStats:       rawData.chapterStats || rawData.ChapterStats || [],
        hardestQuestions:   rawData.hardestQuestions || rawData.HardestQuestions || [],
        studentResults:     rawData.studentResults || rawData.StudentResults || [],
        recommendations:    rawData.recommendations || rawData.Recommendations || []
    };

    DOM.loading.hidden = true;
    DOM.content.hidden = false;

    DOM.title.textContent = "Phân tích: " + data.examTitle;

    if (data.recommendations && data.recommendations.length > 0) {
        DOM.systemAlert.hidden = false;
        DOM.systemAlert.innerHTML = "<strong>Thông báo hệ thống:</strong> " + data.recommendations.join("<br>");
    } else {
        DOM.systemAlert.hidden = true;
    }

    if (data.totalSubmissions === 0) {
        // Stop rendering charts and tables if no data
        DOM.dashboard.hidden = true;
        return;
    }
    DOM.dashboard.hidden = false;

    // ── Zone 1: Thống kê & Recommendations ──
    var passCount = 0;
    var failCount = 0;
    
    data.studentResults.forEach(function(s) {
        if (s.totalPoints >= 5) passCount++;
        else failCount++;
    });

    var passRate = data.totalSubmissions > 0 ? Math.round((passCount / data.totalSubmissions) * 100) : 0;
    var failRate = data.totalSubmissions > 0 ? Math.round((failCount / data.totalSubmissions) * 100) : 0;

    DOM.avgScore.textContent = data.averageScore.toFixed(2);
    DOM.passRate.textContent = passRate;
    DOM.failRate.textContent = failRate;

    // ── Zone 2: Chuẩn đoán Lỗ hổng ──
    renderWeakChapters(data.chapterStats);
    
    DOM.hardCount.textContent = data.hardestQuestions.length;
    renderTable(DOM.hardTable, DOM.tplHardest, data.hardestQuestions, fillHardestRow);
    
    renderScoreDistChart(data.scoreDistribution);

    // ── Zone 3: Phân loại Học sinh ──
    var atRiskStudents = data.studentResults.filter(s => s.totalPoints < 5).sort((a, b) => a.totalPoints - b.totalPoints);
    var excStudents = data.studentResults.filter(s => s.totalPoints >= 8).sort((a, b) => b.totalPoints - a.totalPoints);

    DOM.atRiskCount.textContent = atRiskStudents.length;
    DOM.excCount.textContent = excStudents.length;

    renderTable(DOM.atRiskTable, DOM.tplStudent, atRiskStudents, fillStudentRow);
    renderTable(DOM.excTable, DOM.tplStudent, excStudents, fillStudentRow);

    renderMath(DOM.content);
}

// ═══════════════════════════════════════════
//  Cụ thể từng phần render
// ═══════════════════════════════════════════

function renderWeakChapters(chapters) {
    DOM.weakList.innerHTML = "";
    
    // Sort chapters by accuracyRate ascending (lowest first)
    var sorted = chapters.slice().sort(function(a, b) {
        var accA = a.accuracyRate || a.AccuracyRate || 0;
        var accB = b.accuracyRate || b.AccuracyRate || 0;
        return accA - accB;
    });

    // Take top 3 weakest (accuracy < 50% for example, or just bottom 3)
    var weakest = sorted.slice(0, 3);
    
    if (weakest.length === 0) {
        DOM.weakList.innerHTML = "<li class='list-group-item text-muted text-center py-4'>Không có dữ liệu chương</li>";
        return;
    }

    weakest.forEach(function(chap) {
        var name = chap.chapterName || chap.ChapterName || "Chưa xác định";
        var acc = chap.accuracyRate || chap.AccuracyRate || 0;
        var wrong = 100 - acc;

        var li = DOM.tplWeak.content.cloneNode(true);
        li.querySelector("[data-col='name']").textContent = name;
        li.querySelector("[data-col='wrong-rate']").textContent = wrong + "%";
        
        var badge = li.querySelector("[data-col='acc-rate']");
        badge.textContent = acc + "% Đúng";
        
        // Color based on severity
        if (acc < 40) {
            badge.className = "badge bg-danger text-white";
        } else if (acc < 60) {
            badge.className = "badge bg-warning text-dark";
        } else {
            badge.className = "badge bg-success text-white";
        }

        DOM.weakList.appendChild(li);
    });
}

function renderTable(tbody, template, items, fillFn) {
    tbody.innerHTML = "";
    if (!items || items.length === 0) {
        var tr = document.createElement("tr");
        tr.innerHTML = `<td colspan="4" class="text-center text-muted py-3">Chưa có dữ liệu</td>`;
        tbody.appendChild(tr);
        return;
    }
    items.forEach(function (item, i) {
        var row = template.content.cloneNode(true);
        fillFn(row, item, i);
        tbody.appendChild(row);
    });
}

function fillHardestRow(row, q, i) {
    row.querySelector("[data-col='index']").textContent = i + 1;
    var contentEl = row.querySelector("[data-col='content']");
    var content = q.questionContent || q.QuestionContent || "";
    contentEl.textContent = truncateText(content, 80);
    if (typeof contentEl.render === "function") contentEl.render();
    
    row.querySelector("[data-col='chapter']").textContent = q.chapterName || q.ChapterName;
    
    var badge = row.querySelector("[data-col='accuracy']");
    var acc = q.accuracyRate || q.AccuracyRate || 0;
    badge.textContent = acc + "%";
    badge.classList.add(getScoreClass(acc));
}

function fillStudentRow(row, s, i) {
    row.querySelector("[data-col='name']").textContent = s.studentName;
    var badge = row.querySelector("[data-col='score']");
    badge.textContent = s.totalPoints != null ? s.totalPoints : "—";
    badge.classList.add(getScoreClass(s.totalPoints));
    row.querySelector("[data-col='date']").textContent = formatDateVN(s.submittedAt);
}

// ═══════════════════════════════════════════
//  Biểu đồ Phổ điểm
// ═══════════════════════════════════════════
function renderScoreDistChart(distribution) {
    var canvas = document.getElementById("scoreDistChart");
    if (!canvas) return;
    var ctx = canvas.getContext("2d");

    if (!distribution || Object.keys(distribution).length === 0) {
        ctx.font = "14px 'Be Vietnam Pro', sans-serif";
        ctx.fillStyle = "#94a3b8";
        ctx.textAlign = "center";
        ctx.fillText("Chưa có dữ liệu phổ điểm", canvas.width / 2, canvas.height / 2);
        return;
    }

    var labels = Object.keys(distribution);
    var values = Object.values(distribution).map(v => v || 0);

    new Chart(ctx, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [{
                label: "Số học sinh",
                data: values,
                backgroundColor: getCSSColor("--clr-primary", 1),
                borderColor: "#09090b",
                borderWidth: 2,
                borderRadius: 0,
                barThickness: 30
            }]
        },
        options: { 
            responsive: true, 
            plugins: { legend: { display: false } }, 
            scales: { 
                y: { beginAtZero: true, ticks: { stepSize: 1, font: { family: 'DM Mono', weight: '500' } }, grid: { color: "#e4e4e7" } },
                x: { grid: { display: false }, ticks: { font: { family: 'Bricolage Grotesque', weight: '600' }, color: '#09090b' } }
            } 
        }
    });
}

// ═══════════════════════════════════════════
//  Utility
// ═══════════════════════════════════════════
function getScoreClass(v) {
    if (v == null) return "score-bad";
    return v >= 70 || v >= 8 ? "score-good" : (v >= 40 || v >= 5 ? "score-medium" : "score-bad");
}

function truncateText(str, maxLen) {
    if (!str) return "";
    try { str = JSON.parse(str).stem || str; } catch (e) { }
    return str.length > maxLen ? str.substring(0, maxLen) + "..." : str;
}

function renderMath(container) {
    if (typeof MathLive !== "undefined") {
        MathLive.renderMathInDocument();
    } else {
        window.addEventListener("load", function () {
            if (typeof MathLive !== "undefined") MathLive.renderMathInDocument();
        });
    }
}
