(async () => {
    await window.userReady;

    const rid = window.REQUEST_ID;
    if (!rid) return;

    window.stagedDecisions = new Map();
    window.requestConcurrencyStamp = '';

    const UI = {
        headerInfo: document.getElementById('requestHeaderInfo'),
        statusBadge: document.getElementById('requestStatusBadge'),
        noteBox: document.getElementById('requestNoteBox'),
        noteContent: document.getElementById('requestNoteContent'),
        container: document.getElementById('questionsContainer'),
        commitBar: document.getElementById('commitBar'),
        cardTemplate: document.getElementById('questionCardTemplate')
    };

    const DIFFICULTY_MAP = { 1: 'Nhận biết', 2: 'Thông hiểu', 3: 'Vận dụng', 4: 'Vận dụng cao' };

    function statusPill(status) {
        switch (status) {
            case 1: return '<span class="pill-warn">Chờ duyệt</span>';
            case 3: return '<span class="pill-success">Đã hoàn tất</span>';
            case 4: return '<span class="pill-soft">Đã rút</span>';
            default: return `<span class="pill-soft">Trạng thái ${status}</span>`;
        }
    }

    async function loadDetail() {
        try {
            const res = await apiClient.get(`/api/admin/promotion-requests/${rid}`);
            window.requestConcurrencyStamp = res.concurrencyStamp;
            window.currentRequestData = res;
            
            renderHeader(res);
            renderItems(res);
            updateCommitBar();
        } catch (e) {
            if (window.AdminUI) window.AdminUI.showError(e);
            else alert('Lỗi tải dữ liệu chi tiết');
        }
    }

    window.reloadDetail = loadDetail;

    function renderHeader(data) {
        UI.headerInfo.innerHTML = `Giáo viên: <strong>${data.requestedByName || data.requestedByEmail}</strong> &middot; Môn: <strong>${data.subjectCode}</strong> &middot; Kho đích: <strong>${data.targetBankName}</strong> &middot; Tạo lúc: ${new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(data.createdAtUtc))}`;
        UI.statusBadge.innerHTML = statusPill(data.status);

        if (data.teacherNote) {
            UI.noteBox.classList.remove('d-none');
            UI.noteContent.textContent = data.teacherNote;
        } else {
            UI.noteBox.classList.add('d-none');
        }
    }

    const asMathLiveLatex = (str) => {
        if (!str || str.includes('\\text{') || str.includes('\\displaylines{')) return str || '';
        return str.split(/(\$[^\$]+\$)/g).map(p => (p.startsWith('$') && p.endsWith('$')) ? p.slice(1, -1) : (p ? `\\text{${p}}` : '')).join('');
    };

    const renderQuestionContent = (raw) => {
        let content = raw;
        try {
            const p = JSON.parse(raw);
            if (p?.stem || p?.frame) {
                const cl = (s) => (s || '').replace(/^\\displaylines\s*\{([\s\S]*)\}\s*$/, '$1').trim();
                content = cl(p.stem) + (p.stem && p.frame ? ' \\\\ ' : '') + cl(p.frame);
            }
        } catch(e) { content = asMathLiveLatex(raw); }
        if (content.includes('\\\\') && !content.trim().startsWith('\\displaylines')) content = `\\displaylines{${content}}`;
        const mf = document.createElement('math-field'); mf.readOnly = true; mf.value = content; mf.style.border = 'none';
        const d = document.createElement('div'); d.appendChild(mf);
        return d;
    };

    function renderItems(data) {
        UI.container.innerHTML = '';
        data.items.forEach(i => {
            const card = UI.cardTemplate.content.cloneNode(true).firstElementChild;
            card.dataset.qid = i.questionId;
            card.id = `qcard-${i.questionId}`;
            
            card.querySelector('[data-field-chapter]').textContent = `Chương ${i.chapterId}`;
            card.querySelector('[data-field-difficulty]').textContent = DIFFICULTY_MAP[i.difficulty] || i.difficulty;
            card.querySelector('[data-field-type]').textContent = i.questionType;
            
            const contentDiv = card.querySelector('[data-field-content]');
            contentDiv.appendChild(renderQuestionContent(i.contentLatex || i.questionContent || ''));

            UI.container.appendChild(card);
            rerenderCardState(i.questionId, data.status, i);
        });
    }

    window.approveItem = function(qid) {
        window.stagedDecisions.set(qid, { decision: 'approve' });
        rerenderCardState(qid, window.currentRequestData.status, window.currentRequestData.items.find(x => x.questionId === qid));
        updateCommitBar();
    };

    window.undoDecision = function(qid) {
        window.stagedDecisions.delete(qid);
        rerenderCardState(qid, window.currentRequestData.status, window.currentRequestData.items.find(x => x.questionId === qid));
        updateCommitBar();
    };

    window.rerenderCardState = function(qid, requestStatus, serverItem) {
        const card = document.getElementById(`qcard-${qid}`);
        if (!card) return;

        const statusLabel = card.querySelector('#cardStatusLabel');
        const reasonBox = card.querySelector('#rejectionReasonBox');
        const reasonText = card.querySelector('[data-field-reason]');
        const hint = card.querySelector('#cardActionHint');
        const btns = card.querySelector('#cardActionButtons');

        card.className = 'promotion-card soft-card';
        reasonBox.classList.add('d-none');
        btns.innerHTML = '';

        if (requestStatus === 1) { // Pending
            const stage = window.stagedDecisions.get(qid);
            if (!stage) {
                statusLabel.innerHTML = '';
                hint.textContent = 'Đang chờ rà soát...';
                hint.className = 'small text-muted fw-medium';
                btns.innerHTML = `
                    <button class="btn btn-sm btn-outline-danger me-2" onclick="openRejectModal(${qid})"><i class="bi bi-x-lg"></i> Từ chối</button>
                    <button class="btn btn-sm btn-outline-success" onclick="approveItem(${qid})"><i class="bi bi-check-lg"></i> Duyệt</button>
                `;
            } else if (stage.decision === 'approve') {
                card.classList.add('staged-approve');
                statusLabel.innerHTML = '<span class="pill-success"><i class="bi bi-clock-history"></i> Sẽ duyệt</span>';
                hint.innerHTML = '<span class="text-success"><i class="bi bi-info-circle"></i> Quyết định nháp: Duyệt — sẽ áp dụng khi hoàn tất.</span>';
                btns.innerHTML = `<button class="btn btn-sm btn-outline-secondary" onclick="undoDecision(${qid})"><i class="bi bi-arrow-counterclockwise"></i> Hoàn tác</button>`;
            } else if (stage.decision === 'reject') {
                card.classList.add('staged-reject');
                statusLabel.innerHTML = '<span class="pill-warn"><i class="bi bi-clock-history"></i> Sẽ từ chối</span>';
                reasonBox.classList.remove('d-none');
                reasonText.textContent = stage.rejectionReason;
                hint.innerHTML = '<span class="text-danger"><i class="bi bi-info-circle"></i> Quyết định nháp: Từ chối — sẽ áp dụng khi hoàn tất.</span>';
                btns.innerHTML = `<button class="btn btn-sm btn-outline-secondary" onclick="undoDecision(${qid})"><i class="bi bi-arrow-counterclockwise"></i> Hoàn tác</button>`;
            }
        } else { // Resolved / Withdrawn
            if (serverItem.isApproved) {
                statusLabel.innerHTML = '<span class="pill-success"><i class="bi bi-check-circle"></i> Đã duyệt</span>';
                hint.innerHTML = `<span class="text-muted"><i class="bi bi-person-check"></i> Xử lý bởi ${serverItem.reviewedByAdminName || 'Admin'}</span>`;
            } else if (serverItem.decisionAtUtc) {
                statusLabel.innerHTML = '<span class="pill-warn"><i class="bi bi-x-circle"></i> Đã từ chối</span>';
                reasonBox.classList.remove('d-none');
                reasonText.textContent = serverItem.rejectionReason;
                hint.innerHTML = `<span class="text-muted"><i class="bi bi-person-x"></i> Xử lý bởi ${serverItem.reviewedByAdminName || 'Admin'}</span>`;
            } else {
                statusLabel.innerHTML = '<span class="pill-soft">Đã đóng</span>';
                hint.innerHTML = '';
            }
        }
    };

    window.updateCommitBar = function() {
        const data = window.currentRequestData;
        if (!data) return;

        if (data.status !== 1) { // Not Pending
            UI.commitBar.classList.add('d-none');
            return;
        }

        UI.commitBar.classList.remove('d-none');
        const total = data.items.length;
        let app = 0, rej = 0;
        window.stagedDecisions.forEach(v => {
            if (v.decision === 'approve') app++;
            if (v.decision === 'reject') rej++;
        });
        const decided = app + rej;
        const pending = total - decided;

        document.getElementById('totalNum').textContent = total;
        document.getElementById('decidedNum').textContent = decided;
        document.getElementById('approvedNum').textContent = app;
        document.getElementById('rejectedNum').textContent = rej;
        document.getElementById('pendingNum').textContent = pending;

        const warn = document.getElementById('commitWarning');
        const btn = document.getElementById('commitBtn');

        if (pending > 0) {
            warn.classList.remove('d-none');
            btn.disabled = true;
        } else {
            warn.classList.add('d-none');
            btn.disabled = false;
        }
    };

    document.addEventListener('PromotionStateChanged', () => {
        if (typeof window.updateCommitBar === 'function') {
            window.updateCommitBar();
        }
    });

    await loadDetail();
})();
