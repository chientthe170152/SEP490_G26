document.addEventListener('DOMContentLoaded', () => {
    let currentTab = 'personal';
    let currentPage = 1;
    let currentArchiveId = 0;
    
    const API = API_BASE_URL; // assume this is defined in _Layout
    
    // Elements
    const tabPersonal = document.getElementById('tabPersonal');
    const tabShared = document.getElementById('tabShared');
    const tablePersonal = document.getElementById('tablePersonal');
    const tableShared = document.getElementById('tableShared');
    const tbodyPersonal = document.getElementById('personalBankTbody');
    const tbodyShared = document.getElementById('sharedBankTbody');
    
    const filterSubject = document.getElementById('filterSubject');
    const filterKind = document.getElementById('filterKind');
    const searchInput = document.getElementById('searchInput');
    const btnApplyFilter = document.getElementById('btnApplyFilter');
    const btnResetFilter = document.getElementById('btnResetFilter');
    const linkResetFilter = document.getElementById('linkResetFilter');
    
    const pagerWrap = document.getElementById('pagerWrap');
    const pagerCount = document.getElementById('pagerCount');
    const pagerTotal = document.getElementById('pagerTotal');
    const pagerCtrls = document.getElementById('pagerCtrls');
    const emptyState = document.getElementById('emptyState');
    
    // Bind Events
    tabPersonal.addEventListener('click', () => switchTab('personal'));
    tabShared.addEventListener('click', () => switchTab('shared'));
    btnApplyFilter.addEventListener('click', () => { currentPage = 1; fetchBanks(); });
    btnResetFilter.addEventListener('click', resetFilter);
    linkResetFilter.addEventListener('click', (e) => { e.preventDefault(); resetFilter(); });
    
    document.getElementById('btnConfirmArchive').addEventListener('click', confirmArchiveRequest);
    
    // Load subjects for filter
    loadSubjects();
    
    // Initial fetch
    fetchBanks();
    
    function switchTab(tab) {
        currentTab = tab;
        tabPersonal.classList.toggle('active', tab === 'personal');
        tabShared.classList.toggle('active', tab === 'shared');
        tablePersonal.classList.toggle('d-none', tab !== 'personal');
        tableShared.classList.toggle('d-none', tab !== 'shared');
        currentPage = 1;
        fetchBanks();
    }
    
    function resetFilter() {
        filterSubject.value = '';
        filterKind.value = '';
        searchInput.value = '';
        currentPage = 1;
        fetchBanks();
    }
    
    async function loadSubjects() {
        try {
            const res = await fetch(`${API}/api/class/subjects`);
            if (res.ok) {
                const data = await res.json();
                const subjects = data.data || data;
                subjects.forEach(s => {
                    const opt = new Option(`${s.code} — ${s.name}`, s.subjectId);
                    filterSubject.add(opt);
                    // Also populate modal select
                    const modalOpt = new Option(`${s.code} — ${s.name}`, s.subjectId);
                    document.getElementById('bankSubjectSelect')?.add(modalOpt);
                });
            }
        } catch (e) {
            console.error('Error loading subjects:', e);
        }
    }
    
    async function fetchBanks() {
        const ownerType = currentTab === 'personal' ? 1 : 2;
        const subjectId = filterSubject.value;
        const bankKind = filterKind.value;
        const keyword = searchInput.value;
        
        const url = new URL(`${API}/api/question-banks`);
        url.searchParams.append('ownerType', ownerType);
        if (subjectId) url.searchParams.append('subjectId', subjectId);
        if (bankKind) url.searchParams.append('bankKind', bankKind);
        if (keyword) url.searchParams.append('keyword', keyword);
        url.searchParams.append('page', currentPage);
        url.searchParams.append('pageSize', 20);
        
        try {
            const res = await fetch(url.toString(), {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            const data = await res.json();
            if (res.ok) {
                renderTable(data);
                renderPager(data);
            } else {
                Toastify({ text: data.message || "Lỗi tải ngân hàng", backgroundColor: "#ea580c" }).showToast();
            }
        } catch (e) {
            console.error(e);
        }
    }
    
    function renderTable(data) {
        const tbody = currentTab === 'personal' ? tbodyPersonal : tbodyShared;
        tbody.innerHTML = '';
        
        const items = data.items || [];
        
        if (currentTab === 'personal') {
            document.getElementById('countPersonal').textContent = data.totalCount;
        } else {
            document.getElementById('countShared').textContent = data.totalCount;
        }
        
        if (items.length === 0) {
            emptyState.classList.remove('d-none');
            pagerWrap.classList.add('d-none');
            tablePersonal.classList.add('d-none');
            tableShared.classList.add('d-none');
            return;
        }
        
        emptyState.classList.add('d-none');
        if (currentTab === 'personal') tablePersonal.classList.remove('d-none');
        else tableShared.classList.remove('d-none');
        
        items.forEach(b => {
            const tr = document.createElement('tr');
            tr.onclick = () => window.location.href = `/QuestionBank/Detail/${b.questionBankId}`;
            
            const kindText = b.questionPurpose === 1 ? 'Kiểm tra' : 'Luyện tập';
            const statusClass = b.status === 1 ? 'active' : 'archived';
            const statusText = b.status === 1 ? 'Active' : 'Archived';
            
            const updatedDate = new Date(b.updatedAt || b.createdAt).toLocaleDateString('vi-VN');
            
            if (currentTab === 'personal') {
                tr.innerHTML = `
                    <td>
                        <div class="bank-name">${escapeHtml(b.name)}</div>
                        <div class="bank-desc">${escapeHtml(b.description || '')}</div>
                    </td>
                    <td>
                        <div class="text-muted-cell">${escapeHtml(b.subjectName || '')}</div>
                        <div class="subject-code-cell">${escapeHtml(b.subjectCode || '')}</div>
                    </td>
                    <td class="text-muted-cell">${kindText}</td>
                    <td style="text-align:right;"><span class="num-strong">${b.questionCount}</span></td>
                    <td><div class="updated-by">${updatedDate}</div></td>
                    <td><span class="pill-status ${statusClass}"><span class="dot"></span>${statusText}</span></td>
                    <td onclick="event.stopPropagation()">
                        <div class="row-actions">
                            <button class="btn btn-link btn-sm p-1 text-secondary" onclick="editBank(${b.questionBankId})">Sửa</button>
                            <button class="btn btn-link btn-sm p-1 text-secondary" onclick="openArchiveModal(${b.questionBankId}, '${escapeHtml(b.name).replace(/'/g, "\\'")}')">Lưu trữ</button>
                        </div>
                    </td>
                `;
            } else {
                tr.innerHTML = `
                    <td>
                        <div class="bank-name">${escapeHtml(b.name)}</div>
                        <div class="bank-desc">${escapeHtml(b.description || '')}</div>
                    </td>
                    <td>
                        <div class="text-muted-cell">${escapeHtml(b.subjectName || '')}</div>
                        <div class="subject-code-cell">${escapeHtml(b.subjectCode || '')}</div>
                    </td>
                    <td class="text-muted-cell">${kindText}</td>
                    <td style="text-align:right;"><span class="num-strong">${b.questionCount}</span></td>
                    <td><span class="text-muted-cell">${escapeHtml(b.authorName || '')}</span></td>
                    <td><span class="pill-status ${statusClass}"><span class="dot"></span>${statusText}</span></td>
                    <td onclick="event.stopPropagation()">
                        <a class="btn btn-sm btn-outline-secondary" href="/QuestionBank/Detail/${b.questionBankId}">Xem</a>
                    </td>
                `;
            }
            tbody.appendChild(tr);
        });
    }
    
    function renderPager(data) {
        if (!data.items || data.items.length === 0) {
            pagerWrap.classList.add('d-none');
            return;
        }
        pagerWrap.classList.remove('d-none');
        pagerCount.textContent = data.items.length;
        pagerTotal.textContent = data.totalCount;
        
        pagerCtrls.innerHTML = '';
        const totalPages = data.totalPages || 1;
        
        const prevBtn = document.createElement('button');
        prevBtn.textContent = '‹';
        prevBtn.disabled = currentPage <= 1;
        prevBtn.onclick = () => { currentPage--; fetchBanks(); };
        pagerCtrls.appendChild(prevBtn);
        
        for (let i = 1; i <= totalPages; i++) {
            if (i === 1 || i === totalPages || (i >= currentPage - 1 && i <= currentPage + 1)) {
                const b = document.createElement('button');
                b.textContent = i;
                if (i === currentPage) b.classList.add('active');
                b.onclick = () => { currentPage = i; fetchBanks(); };
                pagerCtrls.appendChild(b);
            } else if (i === currentPage - 2 || i === currentPage + 2) {
                const d = document.createElement('span');
                d.textContent = '...';
                d.style.margin = '0 5px';
                pagerCtrls.appendChild(d);
            }
        }
        
        const nextBtn = document.createElement('button');
        nextBtn.textContent = '›';
        nextBtn.disabled = currentPage >= totalPages;
        nextBtn.onclick = () => { currentPage++; fetchBanks(); };
        pagerCtrls.appendChild(nextBtn);
    }
    
    window.openArchiveModal = function(id, name) {
        currentArchiveId = id;
        document.getElementById('archiveBankName').textContent = name;
        new bootstrap.Modal(document.getElementById('archiveModal')).show();
    };
    
    window.editBank = async function(id) {
        try {
            const res = await fetch(`${API}/api/question-banks/${id}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            if (res.ok) {
                const data = await res.json();
                const b = data.data || data;
                
                document.getElementById('bankIdInput').value = b.questionBankId;
                document.getElementById('bankNameInput').value = b.name;
                document.getElementById('bankSubjectSelect').value = b.subjectId;
                if (b.questionPurpose === 1) document.getElementById('kindExam').checked = true;
                else document.getElementById('kindPractice').checked = true;
                document.getElementById('bankDescInput').value = b.description || '';
                
                document.getElementById('bankModalTitle').textContent = 'Sửa ngân hàng';
                document.getElementById('btnSubmitBank').textContent = 'Lưu thay đổi';
                
                new bootstrap.Modal(document.getElementById('createBankModal')).show();
            }
        } catch (e) {
            console.error(e);
        }
    };
    
    async function confirmArchiveRequest() {
        if (!currentArchiveId) return;
        try {
            const res = await fetch(`${API}/api/question-banks/${currentArchiveId}/archive`, {
                method: 'PATCH',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
            if (res.ok) {
                Toastify({ text: "Đã lưu trữ ngân hàng", backgroundColor: "#16a34a" }).showToast();
                bootstrap.Modal.getInstance(document.getElementById('archiveModal')).hide();
                fetchBanks();
            } else {
                const err = await res.json();
                Toastify({ text: err.message || "Lỗi lưu trữ", backgroundColor: "#ea580c" }).showToast();
            }
        } catch(e) {
            console.error(e);
        }
    }
    
    // Listen for form modal success to reload
    window.addEventListener('bankSaved', () => {
        fetchBanks();
    });
    
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.innerText = text;
        return div.innerHTML;
    }
});
