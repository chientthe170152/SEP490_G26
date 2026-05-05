document.addEventListener('DOMContentLoaded', () => {
    const API = API_BASE_URL; // From _Layout or global
    const bankId = document.getElementById('currentBankId').value;
    
    let currentBank = null;
    let selectedQuestionIds = new Set();
    let currentPage = 1;
    let currentChapters = [];
    
    // UI Elements
    const qBody = document.getElementById('qBody');
    const masterCheck = document.getElementById('masterCheck');
    const thCheckbox = document.getElementById('thCheckbox');
    const thAuthorStatus = document.getElementById('thAuthorStatus');
    const selectionBar = document.getElementById('selectionBar');
    const selCount = document.getElementById('selCount');
    const btnPromote = document.getElementById('btnPromote');
    
    const filterChapter = document.getElementById('filterChapter');
    const filterDiff = document.getElementById('filterDiff');
    const searchInput = document.getElementById('searchInput');
    const btnResetFilter = document.getElementById('btnResetFilter');
    
    // Bind Filter Events
    filterChapter.addEventListener('change', () => { currentPage = 1; fetchQuestions(); });
    filterDiff.addEventListener('change', () => { currentPage = 1; fetchQuestions(); });
    searchInput.addEventListener('keypress', (e) => { if(e.key === 'Enter') { currentPage = 1; fetchQuestions(); } });
    btnResetFilter.addEventListener('click', () => {
        filterChapter.value = ''; filterDiff.value = ''; searchInput.value = '';
        currentPage = 1; fetchQuestions();
    });
    
    // Setup Checkbox
    if (masterCheck) {
        masterCheck.addEventListener('change', (e) => {
            const checks = document.querySelectorAll('.q-check:not(:disabled)');
            checks.forEach(c => {
                c.checked = e.target.checked;
                const id = parseInt(c.value);
                if (c.checked) selectedQuestionIds.add(id);
                else selectedQuestionIds.delete(id);
            });
            updateSelectionUI();
        });
    }
    
    init();
    
    async function init() {
        await fetchBankDetail();
        if (currentBank) {
            await fetchChapters(currentBank.subjectId);
            fetchQuestions();
        }
    }
    
    async function fetchBankDetail() {
        try {
            const res = await fetch(`${API}/api/question-banks/${bankId}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            if (!res.ok) {
                Toastify({text: "Không thể tải chi tiết ngân hàng", backgroundColor: "#ea580c"}).showToast();
                return;
            }
            const data = await res.json();
            currentBank = data.data || data;
            
            // Populate Header
            document.getElementById('bcBankName').textContent = currentBank.name;
            document.getElementById('headerBankName').textContent = currentBank.name;
            document.getElementById('headerSubjectCode').textContent = currentBank.subjectCode || '';
            document.getElementById('headerSubjectName').textContent = currentBank.subjectName || '';
            document.getElementById('headerKind').textContent = currentBank.questionPurpose === 1 ? 'Kiểm tra' : 'Luyện tập';
            document.getElementById('headerQCount').textContent = currentBank.questionCount || 0;
            document.getElementById('headerDesc').textContent = currentBank.description || 'Không có mô tả';
            
            // Personal vs Shared
            if (currentBank.ownerType === 1) { // Personal
                document.getElementById('headerActions').style.display = 'flex';
                document.getElementById('toolbarRow').style.display = 'flex';
                document.getElementById('toolbarQCount').textContent = currentBank.questionCount || 0;
                document.getElementById('btnAddQuestion').href = `/Question/Create?bankId=${bankId}`;
                
                // Bind header actions
                document.getElementById('btnEditBankInfo').addEventListener('click', () => {
                    if (window.editBank) window.editBank(bankId); // defined in bank-form-modal.js globally if included
                });
                document.getElementById('btnArchiveBank').addEventListener('click', () => {
                    document.getElementById('archiveBankName').textContent = currentBank.name;
                    new bootstrap.Modal(document.getElementById('archiveModal')).show();
                });
                document.getElementById('btnConfirmArchive').addEventListener('click', async () => {
                    await fetch(`${API}/api/question-banks/${bankId}/archive`, {
                        method: 'PATCH',
                        headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                    });
                    window.location.href = '/QuestionBank';
                });
            } else { // Shared
                document.getElementById('headerSharedBadge').classList.remove('d-none');
                document.getElementById('headerAuthorSep').style.display = 'inline';
                document.getElementById('headerAuthor').style.display = 'inline';
                document.getElementById('headerAuthorName').textContent = currentBank.authorName || 'Nhiều giáo viên';
                
                // Hide checkboxes and selection bar
                thCheckbox.style.display = 'none';
                thAuthorStatus.textContent = 'Tác giả';
            }
            
            // Expose globally for promotion-modal.js
            window.currentBankContext = currentBank;
            
        } catch (e) {
            console.error(e);
        }
    }
    
    async function fetchChapters(subjId) {
        try {
            const res = await fetch(`${API}/api/class/subject/${subjId}/chapters`);
            if (res.ok) {
                const data = await res.json();
                currentChapters = data.data || data;
                currentChapters.forEach(c => {
                    filterChapter.add(new Option(c.name, c.chapterId));
                });
            }
        } catch (e) { console.error(e); }
    }
    
    async function fetchQuestions() {
        qBody.innerHTML = '<tr><td colspan="8" class="text-center py-4">Đang tải...</td></tr>';
        
        const url = new URL(`${API}/api/question`);
        url.searchParams.append('questionBankId', bankId);
        if (filterChapter.value) url.searchParams.append('chapterId', filterChapter.value);
        if (filterDiff.value) url.searchParams.append('difficulty', filterDiff.value);
        if (searchInput.value) url.searchParams.append('keyword', searchInput.value);
        url.searchParams.append('page', currentPage);
        url.searchParams.append('pageSize', 20);
        
        try {
            const res = await fetch(url.toString(), {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            const data = await res.json();
            
            const items = data.data?.items || data.items || [];
            const totalCount = data.data?.totalCount || data.totalCount || 0;
            
            renderQuestions(items);
            renderPager(items.length, totalCount, data.data?.totalPages || data.totalPages || 1);
            
        } catch(e) {
            console.error(e);
            qBody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-danger">Lỗi tải dữ liệu</td></tr>';
        }
    }
    
    function renderQuestions(items) {
        qBody.innerHTML = '';
        if (items.length === 0) {
            qBody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">Không có câu hỏi nào.</td></tr>';
            if (masterCheck) { masterCheck.checked = false; masterCheck.disabled = true; }
            return;
        }
        if (masterCheck) masterCheck.disabled = false;
        
        const isShared = currentBank.ownerType === 2;
        
        items.forEach(q => {
            const isPending = q.isPromotionPending === true || (q.status === 2); // Assume status 2 is Pending or uses flag
            const diffMap = { 1: 'NB', 2: 'TH', 3: 'VD', 4: 'VDC' };
            const typeMap = { 1: 'MCQ', 2: 'FIB' };
            
            const chName = currentChapters.find(c => c.chapterId === q.chapterId)?.name || `C${q.chapterId}`;
            
            const tr = document.createElement('tr');
            tr.className = `q-row ${isPending ? 'pending-row' : ''}`;
            tr.dataset.qid = q.questionId;
            
            let chkHtml = '';
            if (!isShared) {
                const disabled = isPending ? 'disabled title="Đang gửi đề xuất"' : '';
                const checked = selectedQuestionIds.has(q.questionId) ? 'checked' : '';
                chkHtml = `<td class="row-checkbox"><input type="checkbox" class="form-check-input q-check" value="${q.questionId}" ${disabled} ${checked}></td>`;
            }
            
            let statusOrAuthorHtml = '';
            if (isShared) {
                // In shared bank, show author
                statusOrAuthorHtml = `<td><span class="text-muted-cell">${escapeHtml(q.authorName || 'N/A')}</span></td>`;
            } else {
                let statusPill = '';
                if (q.status === 1) statusPill = `<span class="pill-status active"><span class="dot"></span>Active</span>`;
                else if (q.status === 0) statusPill = `<span class="pill-status draft"><span class="dot"></span>Draft</span>`;
                else if (q.status === 4) statusPill = `<span class="pill-status inprogress"><span class="dot"></span>InProgress</span>`;
                else statusPill = `<span class="pill-status"><span class="dot"></span>Unknown</span>`;
                statusOrAuthorHtml = `<td>${statusPill}</td>`;
            }
            
            tr.innerHTML = `
                ${chkHtml}
                <td class="row-num">Q-${q.questionId}</td>
                <td>
                    <div class="q-content">${escapeHtml(q.content || '').substring(0, 100)}...</div>
                    <div class="text-muted small mt-1">
                        ${isPending ? `<span class="pending-tag"><span class="dot"></span>Đang gửi đề xuất</span>` : 'Cập nhật gần đây'}
                    </div>
                </td>
                <td class="text-muted-cell">${typeMap[q.questionType] || q.questionType}</td>
                <td class="text-muted-cell" title="${escapeHtml(chName)}">${escapeHtml(chName).length > 20 ? escapeHtml(chName).substring(0, 20)+'...' : escapeHtml(chName)}</td>
                <td class="text-muted-cell">M${q.difficulty} · ${diffMap[q.difficulty] || ''}</td>
                ${statusOrAuthorHtml}
                <td><button class="btn btn-link btn-sm p-1 text-secondary btn-view-q" data-id="${q.questionId}">Xem</button></td>
            `;
            qBody.appendChild(tr);
            
            // Render detail hidden row
            const detTr = document.createElement('tr');
            detTr.className = 'q-detail-row';
            detTr.id = `detail-${q.questionId}`;
            detTr.style.display = 'none';
            
            // Build answers
            let ansHtml = '';
            if (q.questionAnswers) {
                q.questionAnswers.forEach((a, idx) => {
                    const label = String.fromCharCode(65 + idx);
                    ansHtml += `
                        <div class="q-answer-box ${a.isCorrect ? 'correct' : ''}">
                            <div class="ans-label">${q.questionType === 2 ? '★' : label}</div>
                            <div class="ans-text">${escapeHtml(a.content)}</div>
                            ${a.isCorrect ? '<span class="check">✓ Đúng</span>' : ''}
                        </div>
                    `;
                });
            }
            
            let actionHtml = '';
            if (!isShared) {
                if (isPending) {
                    actionHtml = `<div class="q-detail-pending-banner"><strong>Đang chờ admin duyệt.</strong><span>Câu hỏi vẫn hiển thị nhưng tạm khóa Sửa/Xóa.</span></div>`;
                } else {
                    actionHtml = `
                        <div class="q-detail-actions">
                            <a href="/Question/Edit/${q.questionId}" class="btn-action-text"><i class="bi bi-pencil"></i> Sửa</a>
                        </div>
                    `;
                }
            } else {
                 actionHtml = `<div class="q-detail-actions"><span class="text-muted small">Kho chung — Không thể sửa trực tiếp.</span></div>`;
            }
            
            detTr.innerHTML = `
                <td colspan="${isShared ? '7' : '8'}">
                    <div class="q-detail-content">
                        ${isPending && !isShared ? actionHtml : ''}
                        <div class="q-detail-section">
                            <span class="q-detail-label">Đề bài:</span>
                            <div class="q-detail-stem">${escapeHtml(q.content || '')}</div>
                        </div>
                        <div class="q-detail-section">
                            <span class="q-detail-label">Đáp án:</span>
                            <div class="q-detail-answers">${ansHtml}</div>
                        </div>
                        ${q.explanation ? `
                        <div class="q-detail-section">
                            <span class="q-detail-label">Giải thích:</span>
                            <div class="q-detail-explanation">${escapeHtml(q.explanation)}</div>
                        </div>
                        ` : ''}
                        ${!isPending && !isShared ? actionHtml : (!isPending && isShared ? actionHtml : '')}
                    </div>
                </td>
            `;
            qBody.appendChild(detTr);
        });
        
        // Bind View buttons
        document.querySelectorAll('.btn-view-q').forEach(b => {
            b.addEventListener('click', (e) => {
                const id = e.target.dataset.id;
                toggleDetail(id);
            });
        });
        
        // Bind Checkboxes
        document.querySelectorAll('.q-check').forEach(c => {
            c.addEventListener('change', (e) => {
                const id = parseInt(e.target.value);
                if (e.target.checked) selectedQuestionIds.add(id);
                else selectedQuestionIds.delete(id);
                updateSelectionUI();
            });
        });
        
        updateSelectionUI();
    }
    
    function toggleDetail(qid) {
        const row = document.getElementById('detail-' + qid);
        const main = document.querySelector(`tr.q-row[data-qid="${qid}"]`);
        if (!row || !main) return;
        const opening = row.style.display === 'none';
        
        document.querySelectorAll('.q-detail-row').forEach(r => r.style.display = 'none');
        document.querySelectorAll('tr.q-row').forEach(r => r.classList.remove('expanded'));
        
        if (opening) {
            row.style.display = '';
            main.classList.add('expanded');
        }
    }
    
    function updateSelectionUI() {
        if (currentBank.ownerType === 2) return;
        const count = selectedQuestionIds.size;
        selCount.textContent = count;
        
        if (count > 0) {
            selectionBar.classList.remove('d-none');
            selectionBar.classList.add('has-selection');
            btnPromote.disabled = false;
        } else {
            selectionBar.classList.add('d-none');
            selectionBar.classList.remove('has-selection');
            btnPromote.disabled = true;
        }
        
        if (masterCheck) {
            const available = document.querySelectorAll('.q-check:not(:disabled)').length;
            const checkedOnPage = document.querySelectorAll('.q-check:checked').length;
            masterCheck.checked = available > 0 && available === checkedOnPage;
            masterCheck.indeterminate = checkedOnPage > 0 && checkedOnPage < available;
        }
        
        // Update global for promote modal
        window.selectedQuestionsForPromote = Array.from(selectedQuestionIds);
    }
    
    function renderPager(count, total, pages) {
        const wrap = document.getElementById('pagerWrap');
        if (total === 0) { wrap.classList.add('d-none'); return; }
        
        wrap.classList.remove('d-none');
        document.getElementById('pagerCount').textContent = count;
        document.getElementById('pagerTotal').textContent = total;
        
        const ctrls = document.getElementById('pagerCtrls');
        ctrls.innerHTML = '';
        
        const prevBtn = document.createElement('button');
        prevBtn.textContent = '‹';
        prevBtn.disabled = currentPage <= 1;
        prevBtn.onclick = () => { currentPage--; fetchQuestions(); };
        ctrls.appendChild(prevBtn);
        
        for (let i = 1; i <= pages; i++) {
            if (i === 1 || i === pages || (i >= currentPage - 1 && i <= currentPage + 1)) {
                const b = document.createElement('button');
                b.textContent = i;
                if (i === currentPage) b.classList.add('active');
                b.onclick = () => { currentPage = i; fetchQuestions(); };
                ctrls.appendChild(b);
            } else if (i === currentPage - 2 || i === currentPage + 2) {
                const d = document.createElement('span');
                d.textContent = '...';
                d.style.margin = '0 5px';
                ctrls.appendChild(d);
            }
        }
        
        const nextBtn = document.createElement('button');
        nextBtn.textContent = '›';
        nextBtn.disabled = currentPage >= pages;
        nextBtn.onclick = () => { currentPage++; fetchQuestions(); };
        ctrls.appendChild(nextBtn);
    }
    
    // Listen for promotion success
    window.addEventListener('promotionSuccess', () => {
        selectedQuestionIds.clear();
        updateSelectionUI();
        fetchQuestions(); // reload
    });
    
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.innerText = text;
        return div.innerHTML;
    }
});
