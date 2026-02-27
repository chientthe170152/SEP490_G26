$(document).ready(function () {
    const state = {
        chapterOptions: []
    };

    const role = getUserRole();
    if (!(role === 'Teacher' || role === 'Giáo viên' || role === 'Admin' || role === 'Quản trị viên' || role === 'Administrator')) {
        showError(['Bạn không có quyền truy cập màn hình tạo ma trận đề.']);
        $('#btnSaveDraft, #btnPublish, #btnAddRow').prop('disabled', true);
        return;
    }

    bindEvents();
    loadSubjects();
    recalcTotals();
    refreshEmptyHint();

    function bindEvents() {
        $('#btnAddRow').on('click', function () {
            addMatrixRow();
        });

        $('#matrixRowList').on('click', '[data-remove-row]', function () {
            $(this).closest('[data-matrix-item]').remove();
            recalcTotals();
            refreshEmptyHint();
        });

        $('#matrixRowList').on('input change', '.matrix-count, .matrix-chapter, .matrix-difficulty', function () {
            recalcTotals();
        });

        $('#blueprintSubject').on('change', function () {
            const subjectId = parseInt($(this).val(), 10);
            hideMessages();
            if (!Number.isInteger(subjectId) || subjectId <= 0) {
                state.chapterOptions = [];
                refreshAllChapterSelects();
                return;
            }
            loadChapters(subjectId);
        });

        $('#btnSaveDraft').on('click', function () {
            submitCreate(0);
        });

        $('#btnPublish').on('click', function () {
            submitCreate(1);
        });
    }

    function loadSubjects() {
        apiClient.get('/api/exam-blueprints/subjects')
            .then(function (subjects) {
                const $subject = $('#blueprintSubject');
                $subject.find('option:not(:first)').remove();
                (subjects || []).forEach(function (item) {
                    const text = item.code ? `${item.code} - ${item.name}` : item.name;
                    $subject.append($('<option></option>').val(item.subjectId).text(text));
                });
            })
            .catch(function (error) {
                console.error(error);
                showError([resolveApiError(error)]);
            });
    }

    function loadChapters(subjectId) {
        apiClient.get(`/api/exam-blueprints/subjects/${subjectId}/chapters`)
            .then(function (chapters) {
                state.chapterOptions = chapters || [];
                refreshAllChapterSelects();
            })
            .catch(function (error) {
                console.error(error);
                state.chapterOptions = [];
                refreshAllChapterSelects();
                showError([resolveApiError(error)]);
            });
    }

    function addMatrixRow(initial) {
        const template = document.getElementById('matrixRowTemplate');
        if (!template) return;

        const cloned = template.content.cloneNode(true);
        $('#matrixRowList').append(cloned);

        const $row = $('#matrixRowList [data-matrix-item]').last();
        if (initial) {
            $row.find('.matrix-difficulty').val(String(initial.Difficulty ?? 1));
            $row.find('.matrix-count').val(initial.TotalQuestions ?? 0);
        }

        refreshChapterSelect($row.find('.matrix-chapter'), initial?.ChapterId ?? null);
        recalcTotals();
        refreshEmptyHint();
    }

    function refreshAllChapterSelects() {
        $('#matrixRowList .matrix-chapter').each(function () {
            const current = parseInt($(this).val(), 10);
            refreshChapterSelect($(this), Number.isInteger(current) ? current : null);
        });
    }

    function refreshChapterSelect($select, selectedValue) {
        $select.empty();
        $select.append('<option value="">Chọn chương</option>');

        state.chapterOptions.forEach(function (chapter) {
            const text = buildChapterOptionLabel(chapter);
            const $option = $('<option></option>').val(chapter.chapterId).text(text);
            if (selectedValue && Number(chapter.chapterId) === Number(selectedValue)) {
                $option.prop('selected', true);
            }
            $select.append($option);
        });
    }

    function buildChapterOptionLabel(chapter) {
        const availability = toAvailabilityMap(chapter.availabilityByDifficulty);
        return `${chapter.name} (NB:${availability[1]}, TH:${availability[2]}, VD:${availability[3]}, VDC:${availability[4]})`;
    }

    function recalcTotals() {
        let total = 0;
        $('#matrixRowList .matrix-count').each(function () {
            const val = parseInt($(this).val(), 10);
            if (Number.isInteger(val) && val >= 0) {
                total += val;
            }
        });
        $('#computedMatrixTotal').text(total);
    }

    function refreshEmptyHint() {
        $('#emptyMatrixHint').toggleClass('d-none', $('#matrixRowList [data-matrix-item]').length > 0);
    }

    function collectRows() {
        const rows = [];
        const errors = [];

        $('#matrixRowList [data-matrix-item]').each(function (index) {
            const chapterId = parseInt($(this).find('.matrix-chapter').val(), 10);
            const difficulty = parseInt($(this).find('.matrix-difficulty').val(), 10);
            const totalQuestions = parseInt($(this).find('.matrix-count').val(), 10);
            const rowNo = index + 1;

            if (!Number.isInteger(chapterId) || chapterId <= 0) {
                errors.push(`Dòng ${rowNo}: vui lòng chọn chương.`);
            }
            if (!Number.isInteger(difficulty) || difficulty < 1 || difficulty > 4) {
                errors.push(`Dòng ${rowNo}: mức độ không hợp lệ.`);
            }
            if (!Number.isInteger(totalQuestions) || totalQuestions < 0) {
                errors.push(`Dòng ${rowNo}: số câu phải là số nguyên >= 0.`);
            }

            rows.push({
                ChapterId: Number.isInteger(chapterId) ? chapterId : 0,
                Difficulty: Number.isInteger(difficulty) ? difficulty : 0,
                TotalQuestions: Number.isInteger(totalQuestions) ? totalQuestions : -1
            });
        });

        return { rows, errors };
    }

    function validateClient(payload, targetStatus) {
        const errors = [];

        if (!payload.Name || !payload.Name.trim()) {
            errors.push('Tên ma trận đề là bắt buộc.');
        }
        if (!payload.SubjectId || payload.SubjectId <= 0) {
            errors.push('Vui lòng chọn môn học.');
        }
        if (!Number.isInteger(payload.TargetTotalQuestions) || payload.TargetTotalQuestions < 0) {
            errors.push('Tổng số câu mục tiêu phải là số nguyên >= 0.');
        }

        const duplicate = new Set();
        payload.Rows.forEach(function (row, idx) {
            const key = `${row.ChapterId}-${row.Difficulty}`;
            if (duplicate.has(key)) {
                errors.push(`Dòng ${idx + 1}: trùng chương và mức độ.`);
            } else {
                duplicate.add(key);
            }
        });

        if (targetStatus === 1) {
            const rowSum = payload.Rows.reduce((sum, row) => sum + row.TotalQuestions, 0);
            if (payload.Rows.length === 0) {
                errors.push('Xuất bản yêu cầu ít nhất một dòng ma trận.');
            }
            if (payload.TargetTotalQuestions <= 0) {
                errors.push('Xuất bản yêu cầu tổng số câu mục tiêu > 0.');
            }
            if (payload.TargetTotalQuestions !== rowSum) {
                errors.push('Tổng số câu mục tiêu phải bằng tổng số câu từ các dòng ma trận.');
            }
            if (payload.Rows.some(r => r.TotalQuestions <= 0)) {
                errors.push('Xuất bản yêu cầu mỗi dòng có số câu > 0.');
            }

            const chapterAvailability = {};
            state.chapterOptions.forEach(function (chapter) {
                chapterAvailability[chapter.chapterId] = toAvailabilityMap(chapter.availabilityByDifficulty);
            });
            payload.Rows.forEach(function (row, idx) {
                const available = chapterAvailability[row.ChapterId]?.[row.Difficulty] ?? 0;
                if (row.TotalQuestions > available) {
                    errors.push(`Dòng ${idx + 1}: vượt số câu hiện có trong ngân hàng (${available}).`);
                }
            });
        }

        return errors;
    }

    function submitCreate(targetStatus) {
        hideMessages();

        const subjectId = parseInt($('#blueprintSubject').val(), 10);
        const targetTotalQuestions = parseInt($('#blueprintTargetQuestionCount').val(), 10);
        const collected = collectRows();

        const payload = {
            Name: ($('#blueprintName').val() || '').toString(),
            Description: ($('#blueprintDescription').val() || '').toString(),
            SubjectId: Number.isInteger(subjectId) ? subjectId : 0,
            TargetTotalQuestions: Number.isInteger(targetTotalQuestions) ? targetTotalQuestions : -1,
            TargetStatus: targetStatus,
            Rows: collected.rows
        };

        const errors = [...collected.errors, ...validateClient(payload, targetStatus)];
        if (errors.length > 0) {
            showError(errors);
            return;
        }

        setSubmitting(true);
        apiClient.post('/api/exam-blueprints', payload)
            .then(function (response) {
                try {
                    if (response?.message) {
                        sessionStorage.setItem('examBlueprintFlashSuccess', response.message);
                    }
                    if (Array.isArray(response?.warnings) && response.warnings.length > 0) {
                        sessionStorage.setItem(
                            'examBlueprintFlashWarnings',
                            JSON.stringify(response.warnings.map(w => w.message || 'Cảnh báo dữ liệu'))
                        );
                    }
                } catch (e) {
                    console.warn('Cannot persist flash message', e);
                }
                window.location.href = '/ExamBlueprint?created=1';
            })
            .catch(function (error) {
                console.error(error);
                const serverErrors = error?.xhr?.responseJSON?.errors;
                if (Array.isArray(serverErrors) && serverErrors.length > 0) {
                    showError(serverErrors);
                } else {
                    showError([resolveApiError(error)]);
                }
            })
            .finally(function () {
                setSubmitting(false);
            });
    }

    function setSubmitting(isSubmitting) {
        $('#btnAddRow, #btnSaveDraft, #btnPublish').prop('disabled', isSubmitting);
    }

    function showError(messages) {
        $('#createWarnings').addClass('d-none').empty();
        $('#createError')
            .html(`<ul class="mb-0 ps-3">${messages.map(m => `<li>${escapeHtml(m)}</li>`).join('')}</ul>`)
            .removeClass('d-none');
    }

    function hideMessages() {
        $('#createError, #createWarnings').addClass('d-none').empty();
    }

    function resolveApiError(error) {
        return error?.xhr?.responseJSON?.message || error?.message || 'Đã có lỗi xảy ra từ máy chủ.';
    }

    function toAvailabilityMap(list) {
        const map = { 1: 0, 2: 0, 3: 0, 4: 0 };
        (list || []).forEach(function (item) {
            map[item.difficulty] = item.availableQuestions;
        });
        return map;
    }

    function escapeHtml(value) {
        return $('<div/>').text(value ?? '').html();
    }
});
