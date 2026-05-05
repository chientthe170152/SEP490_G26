(async () => {
    await window.userReady;

    const UI = {
        status: document.getElementById('filterStatus'),
        subject: document.getElementById('filterSubject'),
        teacher: document.getElementById('filterTeacher'),
        btnFilter: document.getElementById('btnFilter'),
        tbody: document.getElementById('requestTableBody'),
        pagerInfo: document.getElementById('pagerInfo'),
        pagerControls: document.getElementById('pagerControls'),
        rowTemplate: document.getElementById('requestRowTemplate')
    };

    let currentPage = 1;

    function statusPill(status) {
        switch (status) {
            case 1: return '<span class="pill-warn">Chờ duyệt</span>';
            case 3: return '<span class="pill-success">Đã hoàn tất</span>';
            case 4: return '<span class="pill-soft">Đã rút</span>';
            default: return `<span class="pill-soft">Trạng thái ${status}</span>`;
        }
    }

    async function loadFilters() {
        try {
            const [subjects, teachers] = await Promise.all([
                apiClient.get('/api/admin/curriculum/subjects?status=active').catch(() => []),
                apiClient.get('/api/admin/users?role=teacher').catch(() => [])
            ]);

            subjects.forEach(s => {
                UI.subject.add(new Option(s.subjectCode, s.subjectId));
            });
            teachers.forEach(t => {
                UI.teacher.add(new Option(`${t.firstName} ${t.lastName} (${t.email})`, t.userId));
            });
        } catch (e) {
            console.error("Filter load error", e);
        }
    }

    async function loadList(page = 1) {
        currentPage = page;
        const params = {
            status: UI.status.value || undefined,
            subjectId: UI.subject.value || undefined,
            teacherId: UI.teacher.value || undefined,
            page: currentPage,
            pageSize: 20
        };

        UI.tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Đang tải dữ liệu...</td></tr>';

        try {
            const res = await apiClient.get('/api/admin/promotion-requests', params);
            renderTable(res.items);
            renderPager(res);
        } catch (e) {
            UI.tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-danger">Lỗi tải dữ liệu: ${e.message || 'Không xác định'}</td></tr>`;
        }
    }

    function renderTable(items) {
        UI.tbody.innerHTML = '';
        if (!items || !items.length) {
            UI.tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">Không có yêu cầu nào phù hợp.</td></tr>';
            return;
        }

        items.forEach(i => {
            const row = UI.rowTemplate.content.cloneNode(true).firstElementChild;
            row.onclick = () => window.location.href = `/Admin/PromotionRequests/${i.promotionRequestId}`;
            
            row.querySelector('[data-field-id]').textContent = `PR-${i.promotionRequestId}`;
            row.querySelector('[data-field-teacher]').textContent = i.requestedByName || i.requestedByEmail;
            row.querySelector('[data-field-email]').textContent = i.requestedByEmail;
            row.querySelector('[data-field-subject]').textContent = i.subjectCode;
            row.querySelector('[data-field-targetbank]').textContent = i.targetBankName;
            row.querySelector('[data-field-count]').textContent = i.totalQuestions;
            row.querySelector('[data-field-date]').textContent = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(i.createdAtUtc));
            row.querySelector('[data-field-status]').innerHTML = statusPill(i.status);

            UI.tbody.appendChild(row);
        });
    }

    function renderPager(data) {
        if (!data || data.totalCount === 0) {
            UI.pagerInfo.textContent = '';
            UI.pagerControls.innerHTML = '';
            return;
        }

        const start = (data.page - 1) * data.pageSize + 1;
        const end = Math.min(start + data.pageSize - 1, data.totalCount);
        UI.pagerInfo.textContent = `Hiển thị ${start}-${end} trên ${data.totalCount} yêu cầu`;

        if (typeof window.AdminPager !== 'undefined' && UI.pagerControls) {
            window.AdminPager.render(UI.pagerControls, data.page, data.totalPages, loadList);
        }
    }

    UI.btnFilter.onclick = () => loadList(1);

    await loadFilters();
    await loadList();
})();
