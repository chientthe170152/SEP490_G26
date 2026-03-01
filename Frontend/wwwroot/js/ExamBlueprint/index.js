$(document).ready(function () {
    const state = {
        page: 1,
        pageSize: 10,
        selectedId: null
    };

    const role = getUserRole();
    if (!(role === 'Teacher' || role === 'Giáo viên' || role === 'Admin' || role === 'Quản trị viên' || role === 'Administrator')) {
        showPageError('Bạn không có quyền truy cập màn hình ma trận đề.');
        return;
    }

    bindEvents();
    renderNoticeFromQuery();
    loadSubjects().then(function () {
        loadList(1);
    });

    function bindEvents() {
        $('#blueprintFilterForm').on('submit', function (e) {
            e.preventDefault();
            loadList(1);
        });

        $('#blueprintTableBody').on('click', '[data-action="view-blueprint"]', function () {
            const id = parseInt($(this).closest('tr').attr('data-blueprint-id'), 10);
            if (Number.isInteger(id)) {
                loadDetail(id);
            }
        });

        $('#blueprintPagination').on('click', 'a[data-page]', function (e) {
            e.preventDefault();
            const page = parseInt($(this).attr('data-page'), 10);
            if (Number.isInteger(page)) {
                loadList(page);
            }
        });
    }

    function renderNoticeFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const flashSuccess = sessionStorage.getItem('examBlueprintFlashSuccess');
        const flashWarningsRaw = sessionStorage.getItem('examBlueprintFlashWarnings');

        if (params.get('created') === '1') {
            $('#pageNotice')
                .text(flashSuccess || 'Tạo ma trận đề thành công.')
                .removeClass('d-none');
        }

        if (flashWarningsRaw) {
            try {
                const warnings = JSON.parse(flashWarningsRaw);
                if (Array.isArray(warnings) && warnings.length > 0) {
                    $('#pageError')
                        .removeClass('d-none alert-danger')
                        .addClass('alert-warning')
                        .html(`<strong>Cảnh báo khi lưu nháp:</strong><ul class="mb-0 ps-3 mt-1">${warnings.map(w => `<li>${escapeHtml(w)}</li>`).join('')}</ul>`);
                }
            } catch (e) {
                console.warn('Cannot parse flash warnings', e);
            }
        }

        sessionStorage.removeItem('examBlueprintFlashSuccess');
        sessionStorage.removeItem('examBlueprintFlashWarnings');
    }

    function loadSubjects() {
        return apiClient.get('/api/exam-blueprints/subjects')
            .then(function (subjects) {
                const $select = $('#filterSubject');
                $select.find('option:not(:first)').remove();
                (subjects || []).forEach(function (subject) {
                    const text = subject.code ? `${subject.code} - ${subject.name}` : subject.name;
                    $select.append($('<option></option>').val(subject.subjectId).text(text));
                });
            })
            .catch(function (error) {
                console.error(error);
                showPageError(resolveApiError(error));
            });
    }

    function loadList(page) {
        state.page = page;
        $('#pageError').addClass('d-none').text('');

        const params = new URLSearchParams();
        params.set('page', String(page));
        params.set('pageSize', String(state.pageSize));

        const keyword = ($('#filterKeyword').val() || '').toString().trim();
        const subjectId = ($('#filterSubject').val() || '').toString();
        if (keyword) params.set('keyword', keyword);
        if (subjectId) params.set('subjectId', subjectId);

        $('#blueprintTableBody').html('<tr><td colspan="6" class="text-center text-muted py-4">Đang tải dữ liệu...</td></tr>');

        apiClient.get(`/api/exam-blueprints?${params.toString()}`)
            .then(function (response) {
                const items = response.items || [];
                renderTable(items);
                renderPagination(response.page || 1, response.totalPages || 0);

                if (items.length === 0) {
                    state.selectedId = null;
                    renderEmptyDetail();
                    return;
                }

                const firstId = items[0].examBlueprintId;
                const nextId = items.some(i => i.examBlueprintId === state.selectedId) ? state.selectedId : firstId;
                loadDetail(nextId);
            })
            .catch(function (error) {
                console.error(error);
                $('#blueprintTableBody').html('<tr><td colspan="6" class="text-center text-danger py-4">Không thể tải danh sách ma trận đề.</td></tr>');
                renderEmptyDetail('Không thể tải chi tiết do lỗi danh sách.');
                renderPagination(1, 0);
                showPageError(resolveApiError(error));
            });
    }

    function renderTable(items) {
        if (!items.length) {
            $('#blueprintTableBody').html('<tr><td colspan="6" class="text-center text-muted py-4">Chưa có ma trận đề nào.</td></tr>');
            return;
        }

        const html = items.map(function (item) {
            const subjectText = item.subjectCode || item.subjectName || '';
            return `
                <tr data-blueprint-id="${item.examBlueprintId}" class="${item.examBlueprintId === state.selectedId ? 'table-active' : ''}">
                    <td>${escapeHtml(item.name || '')}</td>
                    <td>${item.totalQuestions ?? 0}</td>
                    <td>${escapeHtml(subjectText)}</td>
                    <td><span class="badge ${getStatusClass(item.status)}">${escapeHtml(item.statusLabel || '')}</span></td>
                    <td>${escapeHtml(formatDate(item.updatedAtUtc))}</td>
                    <td>
                        <div class="d-flex flex-wrap gap-1">
                            <button type="button" class="btn btn-sm btn-outline-secondary" data-action="view-blueprint">Xem</button>
                            <button type="button" class="btn btn-sm btn-outline-secondary" disabled title="Sẽ triển khai sau">Sửa</button>
                            <button type="button" class="btn btn-sm btn-outline-secondary" disabled title="Sẽ triển khai sau">Lưu trữ</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');

        $('#blueprintTableBody').html(html);
    }

    function renderPagination(page, totalPages) {
        const $pagination = $('#blueprintPagination');
        if (!totalPages || totalPages <= 1) {
            $pagination.empty();
            return;
        }

        let html = '';
        html += page > 1
            ? `<li class="page-item"><a class="page-link" href="#" data-page="${page - 1}">Trước</a></li>`
            : '<li class="page-item disabled"><span class="page-link">Trước</span></li>';

        for (let i = 1; i <= totalPages; i++) {
            if (i === page) {
                html += `<li class="page-item active"><span class="page-link">${i}</span></li>`;
            } else {
                html += `<li class="page-item"><a class="page-link" href="#" data-page="${i}">${i}</a></li>`;
            }
        }

        html += page < totalPages
            ? `<li class="page-item"><a class="page-link" href="#" data-page="${page + 1}">Sau</a></li>`
            : '<li class="page-item disabled"><span class="page-link">Sau</span></li>';

        $pagination.html(html);
    }

    function loadDetail(id) {
        state.selectedId = id;
        $('#blueprintTableBody tr').removeClass('table-active');
        $(`#blueprintTableBody tr[data-blueprint-id="${id}"]`).addClass('table-active');

        apiClient.get(`/api/exam-blueprints/${id}`)
            .then(function (detail) {
                $('#blueprintInfoName').text(detail.name || '--');
                $('#blueprintInfoSubject').text(detail.subjectCode || detail.subjectName || '--');
                $('#blueprintInfoUpdated').text(formatDate(detail.updatedAtUtc));
                $('#blueprintInfoQuestionCount').text(`${detail.totalQuestions ?? 0} câu`);

                const rows = detail.rows || [];
                if (!rows.length) {
                    $('#blueprintMatrixBody').html('<tr><td colspan="3" class="text-center text-muted py-3">Ma trận đề chưa có dòng nào.</td></tr>');
                    return;
                }

                $('#blueprintMatrixBody').html(rows.map(function (row) {
                    return `
                        <tr>
                            <td>${escapeHtml(row.chapterName || '')}</td>
                            <td>${escapeHtml(row.difficultyLabel || '')}</td>
                            <td>${row.totalQuestions ?? 0}</td>
                        </tr>
                    `;
                }).join(''));
            })
            .catch(function (error) {
                console.error(error);
                renderEmptyDetail('Không thể tải chi tiết ma trận đề.');
                showPageError(resolveApiError(error));
            });
    }

    function renderEmptyDetail(message) {
        $('#blueprintInfoName').text('Chưa chọn');
        $('#blueprintInfoSubject').text('--');
        $('#blueprintInfoUpdated').text('--');
        $('#blueprintInfoQuestionCount').text('--');
        $('#blueprintMatrixBody').html(`<tr><td colspan="3" class="text-center text-muted py-3">${escapeHtml(message || 'Chọn một ma trận đề để xem chi tiết.')}</td></tr>`);
    }

    function getStatusClass(status) {
        switch (status) {
            case 0: return 'badge-status badge-draft';
            case 1: return 'badge-status badge-published';
            case 2: return 'badge-status badge-inuse';
            case 3: return 'badge-status badge-archived';
            default: return 'text-bg-secondary';
        }
    }

    function formatDate(value) {
        if (!value) return '--';
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return '--';
        return d.toLocaleDateString('vi-VN');
    }

    function showPageError(message) {
        $('#pageError')
            .removeClass('d-none alert-warning')
            .addClass('alert-danger')
            .text(message || 'Đã xảy ra lỗi.');
    }

    function resolveApiError(error) {
        return error?.xhr?.responseJSON?.message || error?.message || 'Đã có lỗi xảy ra từ máy chủ.';
    }

    function escapeHtml(value) {
        return $('<div/>').text(value ?? '').html();
    }
});
