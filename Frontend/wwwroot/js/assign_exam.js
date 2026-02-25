(() => {
  const toArray = (value) => Array.from(value || []);

  const bindInteractiveRows = () => {
    toArray(document.querySelectorAll('.interactive-row')).forEach((row) => {
      if (!row.hasAttribute('tabindex')) {
        row.setAttribute('tabindex', '0');
      }
      row.addEventListener('focus', () => row.classList.add('is-active'));
      row.addEventListener('blur', () => row.classList.remove('is-active'));
      row.addEventListener('mouseenter', () => row.classList.add('is-active'));
      row.addEventListener('mouseleave', () => row.classList.remove('is-active'));
    });
  };

  const formatDuration = (totalSeconds) => {
    const sec = Math.max(0, totalSeconds);
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    const s = sec % 60;
    if (h > 0) {
      return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  };

  const bindCountdowns = () => {
    toArray(document.querySelectorAll('[data-countdown]')).forEach((node) => {
      let remain = Number(node.getAttribute('data-countdown'));
      if (!Number.isFinite(remain) || remain < 0) {
        return;
      }

      const render = () => {
        node.textContent = formatDuration(remain);
        node.classList.remove('is-warning', 'is-danger');
        if (remain <= 300) {
          node.classList.add('is-danger');
        } else if (remain <= 900) {
          node.classList.add('is-warning');
        }
      };

      render();
      const timer = window.setInterval(() => {
        remain = Math.max(0, remain - 1);
        render();
        if (remain <= 0) {
          window.clearInterval(timer);
        }
      }, 1000);
    });
  };

  const isElementVisible = (node) => {
    if (!node) {
      return false;
    }

    if (node.closest('.d-none, [hidden]')) {
      return false;
    }

    const style = window.getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden') {
      return false;
    }

    return node.getClientRects().length > 0;
  };

  const getMathFieldValue = (field) => {
    if (!field) {
      return '';
    }

    if (typeof field.getValue === 'function') {
      const latex = field.getValue('latex');
      return typeof latex === 'string' ? latex : '';
    }

    if (typeof field.value === 'string') {
      return field.value;
    }

    return field.textContent || '';
  };

  const hasPromptAnswer = (field) => {
    if (!field || typeof field.getPrompts !== 'function' || typeof field.getPromptValue !== 'function') {
      return null;
    }

    const prompts = field.getPrompts();
    if (!Array.isArray(prompts) || prompts.length === 0) {
      return null;
    }

    return prompts.some((promptId) => {
      const value = field.getPromptValue(promptId);
      return typeof value === 'string' ? value.trim().length > 0 : false;
    });
  };

  const isAnswered = (questionNode) => {
    const hasChecked = toArray(questionNode.querySelectorAll('input[type="radio"], input[type="checkbox"]'))
      .some((input) => !input.disabled && isElementVisible(input) && input.checked);
    if (hasChecked) {
      return true;
    }

    const hasText = toArray(questionNode.querySelectorAll('input[type="text"], input[type="number"], textarea'))
      .some((input) => !input.disabled && isElementVisible(input) && input.value.trim().length > 0);
    if (hasText) {
      return true;
    }

    const hasMathAnswer = toArray(questionNode.querySelectorAll('math-field[data-answer-math]'))
      .some((field) => {
        if (field.hasAttribute('disabled') || !isElementVisible(field)) {
          return false;
        }

        const promptAnswer = hasPromptAnswer(field);
        if (promptAnswer !== null) {
          return promptAnswer;
        }

        return getMathFieldValue(field).trim().length > 0;
      });
    return hasMathAnswer;
  };

  const bindValidateUnanswered = () => {
    const btn = document.querySelector('[data-action="validate-unanswered"]');
    if (!btn) {
      return;
    }

    btn.addEventListener('click', () => {
      const questions = toArray(document.querySelectorAll('.question-block[data-question-id]'));
      let unanswered = 0;

      questions.forEach((question) => {
        if (isAnswered(question)) {
          question.classList.remove('border-danger');
          return;
        }
        unanswered += 1;
        question.classList.add('border-danger');
      });

      const output = document.querySelector('[data-unanswered-output]');
      if (output) {
        output.textContent = unanswered === 0
          ? 'Tat ca cau hoi da co cau tra loi.'
          : `Con ${unanswered} cau chua tra loi.`;
      }
    });
  };

  const reindexAnswers = (list) => {
    toArray(list.querySelectorAll('[data-answer-item]')).forEach((item, idx) => {
      const label = item.querySelector('[data-answer-index]');
      if (label) {
        label.textContent = String.fromCharCode(65 + idx);
      }
    });
  };

  const bindAnswerRepeater = () => {
    document.addEventListener('click', (event) => {
      const addBtn = event.target.closest('[data-add-answer]');
      if (addBtn) {
        const listId = addBtn.getAttribute('data-add-answer');
        const list = document.getElementById(listId);
        if (!list) {
          return;
        }

        const template = list.querySelector('[data-answer-template]');
        if (!template) {
          return;
        }

        const clone = template.cloneNode(true);
        clone.removeAttribute('data-answer-template');
        clone.setAttribute('data-answer-item', '');
        clone.classList.remove('d-none');

        toArray(clone.querySelectorAll('input[type="text"], input[type="number"], textarea')).forEach((input) => {
          input.value = '';
        });

        toArray(clone.querySelectorAll('input[type="radio"], input[type="checkbox"]')).forEach((input) => {
          input.checked = false;
        });

        list.appendChild(clone);
        reindexAnswers(list);
        return;
      }

      const removeBtn = event.target.closest('[data-remove-answer]');
      if (!removeBtn) {
        return;
      }

      const list = removeBtn.closest('[data-answer-list]');
      const row = removeBtn.closest('[data-answer-item]');
      if (!list || !row) {
        return;
      }

      const currentRows = toArray(list.querySelectorAll('[data-answer-item]'));
      if (currentRows.length <= 2) {
        return;
      }

      row.remove();
      reindexAnswers(list);
    });

    toArray(document.querySelectorAll('[data-answer-list]')).forEach((list) => reindexAnswers(list));
  };

  const recalcMatrixTotals = (list) => {
    const panel = list.closest('.panel-card, .form-section, .soft-card') || list.parentElement;
    let totalQuestions = 0;
    let totalPoints = 0;

    toArray(list.querySelectorAll('[data-matrix-item]')).forEach((row) => {
      const countInput = row.querySelector('.matrix-count');
      const pointInput = row.querySelector('.matrix-point');
      const count = Number(countInput ? countInput.value : 0);
      const point = Number(pointInput ? pointInput.value : 0);

      totalQuestions += Number.isFinite(count) ? count : 0;
      totalPoints += Number.isFinite(point) ? point : 0;
    });

    const totalCountNode = panel ? panel.querySelector('[data-total-count]') : null;
    const totalPointNode = panel ? panel.querySelector('[data-total-point]') : null;

    if (totalCountNode) {
      totalCountNode.textContent = String(totalQuestions);
    }

    if (totalPointNode) {
      totalPointNode.textContent = `${totalPoints.toFixed(1)}`;
    }
  };

  const bindMatrixRepeater = () => {
    toArray(document.querySelectorAll('[data-add-matrix]')).forEach((btn) => {
      btn.addEventListener('click', () => {
        const listId = btn.getAttribute('data-add-matrix');
        const list = document.getElementById(listId);
        if (!list) {
          return;
        }

        const template = list.querySelector('[data-matrix-template]');
        if (!template) {
          return;
        }

        const clone = template.cloneNode(true);
        clone.removeAttribute('data-matrix-template');
        clone.setAttribute('data-matrix-item', '');
        clone.classList.remove('d-none');

        toArray(clone.querySelectorAll('input')).forEach((input) => {
          input.value = input.classList.contains('matrix-point') ? '1' : '';
        });

        list.appendChild(clone);
        recalcMatrixTotals(list);
      });
    });

    document.addEventListener('click', (event) => {
      const removeBtn = event.target.closest('[data-remove-matrix]');
      if (!removeBtn) {
        return;
      }

      const list = removeBtn.closest('[data-matrix-list]');
      const row = removeBtn.closest('[data-matrix-item]');
      if (!list || !row) {
        return;
      }

      const rows = toArray(list.querySelectorAll('[data-matrix-item]'));
      if (rows.length <= 1) {
        return;
      }

      row.remove();
      recalcMatrixTotals(list);
    });

    document.addEventListener('input', (event) => {
      if (!event.target.closest('.matrix-count, .matrix-point')) {
        return;
      }

      const list = event.target.closest('[data-matrix-list]');
      if (list) {
        recalcMatrixTotals(list);
      }
    });

    toArray(document.querySelectorAll('[data-matrix-list]')).forEach((list) => recalcMatrixTotals(list));
  };

  const bindImportForms = () => {
    toArray(document.querySelectorAll('[data-import-form]')).forEach((form) => {
      form.addEventListener('submit', (event) => {
        event.preventDefault();

        const fileInput = form.querySelector('[data-import-file]');
        const result = form.querySelector('[data-import-result]');
        if (!fileInput || !result) {
          return;
        }

        const allowedExt = (form.getAttribute('data-allowed-ext') || '')
          .split(',')
          .map((ext) => ext.trim().toLowerCase())
          .filter(Boolean);

        const selectedName = fileInput.files && fileInput.files[0] ? fileInput.files[0].name : '';
        const lowerName = selectedName.toLowerCase();
        const isAllowed = selectedName.length > 0 && allowedExt.some((ext) => lowerName.endsWith(ext));

        result.classList.add('show', 'alert');
        result.classList.remove('alert-danger', 'alert-success');

        if (!isAllowed) {
          result.classList.add('alert-danger');
          result.textContent = `Tep khong hop le. Chi chap nhan: ${allowedExt.join(', ') || 'khong xac dinh'}.`;
          return;
        }

        result.classList.add('alert-success');
        result.textContent = `Nhap du lieu mo phong thanh cong: ${selectedName}.`;
      });
    });
  };

  bindInteractiveRows();
  bindCountdowns();
  bindValidateUnanswered();
  bindAnswerRepeater();
  bindMatrixRepeater();
  bindImportForms();
})();

(() => {
  const publicToggle = document.getElementById('publicExamToggle');
  const classSelectionBlock = document.getElementById('classSelectionBlock');
  const classSection = document.getElementById('classSelectionSection');
  const classSearchInput = document.getElementById('classSearchInput');
  const classSubjectFilter = document.getElementById('classSubjectFilter');
  const classSemesterFilter = document.getElementById('classSemesterFilter');
  const classTable = document.getElementById('classSelectionTable');
  const selectedCount = document.getElementById('selectedClassCount');
  const classPaginationPrev = document.getElementById('classPaginationPrev');
  const classPaginationNext = document.getElementById('classPaginationNext');
  const classPageButtons = Array.from(document.querySelectorAll('[data-class-page]'));
  const classPageItems = Array.from(document.querySelectorAll('[data-page-item]'));
  const classPaginationSummary = document.getElementById('classPaginationSummary');

  const blueprintSearchInput = document.getElementById('blueprintSearchInput');
  const blueprintSubjectFilter = document.getElementById('blueprintSubjectFilter');
  const blueprintListTable = document.getElementById('blueprintListTable');
  const blueprintPaginationPrev = document.getElementById('blueprintPaginationPrev');
  const blueprintPaginationNext = document.getElementById('blueprintPaginationNext');
  const blueprintPageButtons = Array.from(document.querySelectorAll('[data-blueprint-page]'));
  const blueprintPageItems = Array.from(document.querySelectorAll('[data-blueprint-page-item]'));
  const blueprintFilterSummary = document.getElementById('blueprintFilterSummary');
  const selectedBlueprintName = document.getElementById('selectedBlueprintName');
  const blueprintInfoName = document.getElementById('blueprintInfoName');
  const blueprintInfoUpdated = document.getElementById('blueprintInfoUpdated');
  const blueprintInfoSubject = document.getElementById('blueprintInfoSubject');
  const blueprintInfoQuestionCount = document.getElementById('blueprintInfoQuestionCount');
  const blueprintMatrixBody = document.getElementById('blueprintMatrixBody');

  const examGenerationBlueprint = document.getElementById('examGenerationBlueprint');
  const examGenerationManual = document.getElementById('examGenerationManual');
  const blueprintModeSection = document.getElementById('blueprintModeSection');
  const manualModeSection = document.getElementById('manualModeSection');
  const examGenerationModeDivider = document.getElementById('examGenerationModeDivider');
  const publicSubjectSection = document.getElementById('publicSubjectSection');
  const publicSubjectFilter = document.getElementById('publicSubjectFilter');

  const manualQuestionSubjectFilter = document.getElementById('manualQuestionSubjectFilter');
  const manualQuestionChapterFilter = document.getElementById('manualQuestionChapterFilter');
  const manualQuestionLevelFilter = document.getElementById('manualQuestionLevelFilter');
  const manualQuestionSearchInput = document.getElementById('manualQuestionSearchInput');
  const manualQuestionTableBody = document.getElementById('manualQuestionTableBody');
  const manualQuestionSummary = document.getElementById('manualQuestionSummary');
  const manualQuestionClearButton = document.getElementById('manualQuestionClearButton');

  if (!publicToggle || !classTable || !classSection) {
    return;
  }

  const classPageSize = 5;
  const blueprintPageSize = 2;
  let classCurrentPage = 1;
  let blueprintCurrentPage = 1;
  const manualSelectedQuestionIds = new Set();

  const normalizeText = (value = '') => value.toLowerCase().trim();

  const manualQuestionData = [
    { id: 'MAE-Q001', subject: 'MAE', chapter: 'Limits', level: 'Nhận biết', content: 'Tính lim x->0 (sinx/x).' },
    { id: 'MAE-Q002', subject: 'MAE', chapter: 'Limits', level: 'Thông hiểu', content: 'Tính lim x->infinity ((2x+1)/(x-3)).' },
    { id: 'MAE-Q010', subject: 'MAE', chapter: 'Derivatives', level: 'Nhận biết', content: 'Tính đạo hàm bậc nhất của hàm đa thức.' },
    { id: 'MAE-Q020', subject: 'MAE', chapter: 'Integrals', level: 'Vận dụng', content: 'Tính tích phân xác định bằng đổi biến.' },
    { id: 'MAD-Q003', subject: 'MAD', chapter: 'Data Types', level: 'Thông hiểu', content: 'Phân biệt value type và reference type.' },
    { id: 'MAD-Q015', subject: 'MAD', chapter: 'OOP', level: 'Vận dụng', content: 'Phân tích ví dụ kế thừa và đa hình.' },
    { id: 'MAS-Q002', subject: 'MAS', chapter: 'Probability Basics', level: 'Nhận biết', content: 'Tính xác suất biến cố độc lập.' },
    { id: 'MAS-Q020', subject: 'MAS', chapter: 'Hypothesis Testing', level: 'Vận dụng cao', content: 'Thực hiện z-test cho mẫu lớn.' }
  ];

  const blueprintData = {
    pt1: { name: 'pt1', subject: 'MAE', updated: '12/02/2026', questionCount: 18, matrix: [
      { chapter: 'Chương 1: Limits', level: 'Nhận biết', count: 6 },
      { chapter: 'Chương 2: Derivatives', level: 'Thông hiểu', count: 6 },
      { chapter: 'Chương 3: Integrals', level: 'Vận dụng', count: 5 },
      { chapter: 'Chương 4: Applications', level: 'Vận dụng cao', count: 3 }
    ] },
    pt2: { name: 'pt2', subject: 'MAE', updated: '14/02/2026', questionCount: 16, matrix: [
      { chapter: 'Chương 1: Limits', level: 'Nhận biết', count: 5 },
      { chapter: 'Chương 2: Derivatives', level: 'Thông hiểu', count: 5 },
      { chapter: 'Chương 3: Integrals', level: 'Vận dụng', count: 4 },
      { chapter: 'Chương 4: Applications', level: 'Vận dụng cao', count: 2 }
    ] },
    final_exam: { name: 'final exam', subject: 'MAD', updated: '10/02/2026', questionCount: 24, matrix: [
      { chapter: 'Chương 1: Data Types', level: 'Nhận biết', count: 8 },
      { chapter: 'Chương 2: OOP', level: 'Thông hiểu', count: 7 },
      { chapter: 'Chương 3: Exception Handling', level: 'Vận dụng', count: 6 },
      { chapter: 'Chương 4: Project Scenario', level: 'Vận dụng cao', count: 3 }
    ] },
    retake: { name: 'retake', subject: 'MAS', updated: '08/02/2026', questionCount: 14, matrix: [
      { chapter: 'Chương 1: Probability Basics', level: 'Nhận biết', count: 4 },
      { chapter: 'Chương 2: Distributions', level: 'Thông hiểu', count: 4 },
      { chapter: 'Chương 3: Hypothesis Testing', level: 'Vận dụng', count: 4 },
      { chapter: 'Chương 4: Statistical Cases', level: 'Vận dụng cao', count: 2 }
    ] }
  };

  const classRows = () => Array.from(classTable.querySelectorAll('tbody tr'));
  const classSelectors = () => Array.from(classTable.querySelectorAll('.class-item'));
  const blueprintRows = () => blueprintListTable ? Array.from(blueprintListTable.querySelectorAll('tbody tr')) : [];
  const blueprintSelectors = () => blueprintListTable ? Array.from(blueprintListTable.querySelectorAll('.blueprint-item')) : [];

  const updateSelectedClass = () => {
    if (!selectedCount) return;
    const selectedInput = classSelectors().find((item) => item.checked);
    const selectedRow = selectedInput ? selectedInput.closest('tr') : null;
    const selectedCode = selectedRow ? selectedRow.getAttribute('data-class-code') || '' : '';
    selectedCount.textContent = selectedCode ? `Đã chọn: ${selectedCode}` : 'Chưa chọn lớp học';
  };

  const applyPublicMode = () => {
    const hideClass = Boolean(publicToggle.checked);
    const block = classSelectionBlock || classSection;
    if (!block) return;
    block.classList.toggle('d-none', hideClass);
    block.style.display = hideClass ? 'none' : '';
    block.setAttribute('aria-hidden', hideClass ? 'true' : 'false');

    if (publicSubjectSection) {
      publicSubjectSection.classList.toggle('d-none', !hideClass);
    }

    // In public mode, centralize subject choice at publicSubjectFilter.
    if (blueprintSubjectFilter) {
      blueprintSubjectFilter.disabled = hideClass;
    }
    if (manualQuestionSubjectFilter) {
      manualQuestionSubjectFilter.disabled = hideClass;
    }

    syncPublicSubjectFilters();
  };

  const syncPublicSubjectFilters = () => {
    const isPublic = Boolean(publicToggle.checked);
    if (!isPublic) {
      return;
    }

    const value = publicSubjectFilter ? publicSubjectFilter.value : '';
    if (blueprintSubjectFilter) {
      blueprintSubjectFilter.value = value;
    }
    if (manualQuestionSubjectFilter) {
      manualQuestionSubjectFilter.value = value;
    }

    renderBlueprintTable(true);
    syncManualChapterOptions();
    renderManualTable();
  };

  const applyExamGenerationMode = () => {
    const useManual = Boolean(examGenerationManual && examGenerationManual.checked);
    if (blueprintModeSection) {
      blueprintModeSection.hidden = useManual;
      blueprintModeSection.classList.toggle('d-none', useManual);
    }
    if (manualModeSection) {
      manualModeSection.hidden = !useManual;
      manualModeSection.classList.toggle('d-none', !useManual);
    }
    if (examGenerationModeDivider) {
      examGenerationModeDivider.classList.add('d-none');
    }
  };

  const getFilteredClassRows = () => {
    const keyword = normalizeText(classSearchInput ? classSearchInput.value : '');
    const subject = normalizeText(classSubjectFilter ? classSubjectFilter.value : '');
    const semester = normalizeText(classSemesterFilter ? classSemesterFilter.value : '');
    return classRows().filter((row) => {
      const code = normalizeText(row.getAttribute('data-class-code') || '');
      const rowSubject = normalizeText(row.getAttribute('data-subject') || '');
      const rowSemester = normalizeText(row.getAttribute('data-semester') || '');
      return (keyword.length === 0 || code.includes(keyword)) &&
        (subject.length === 0 || rowSubject === subject) &&
        (semester.length === 0 || rowSemester === semester);
    });
  };

  const renderClassTable = (resetPage = false) => {
    const filtered = getFilteredClassRows();
    const totalRows = filtered.length;
    const totalPages = Math.max(1, Math.ceil(totalRows / classPageSize));
    if (resetPage) classCurrentPage = 1;
    classCurrentPage = Math.min(classCurrentPage, totalPages);
    const start = totalRows === 0 ? 0 : (classCurrentPage - 1) * classPageSize;
    const end = Math.min(start + classPageSize, totalRows);
    const visible = filtered.slice(start, end);

    classRows().forEach((row) => row.classList.add('d-none'));
    visible.forEach((row) => row.classList.remove('d-none'));

    if (classPaginationSummary) {
      classPaginationSummary.textContent = totalRows === 0
        ? 'Không có lớp học phù hợp bộ lọc.'
        : `Hiển thị ${start + 1}-${end} trên ${totalRows} lớp`;
    }

    classPageButtons.forEach((button, idx) => {
      const pageNumber = idx + 1;
      const pageItem = classPageItems[idx];
      const enabled = totalRows > 0 && pageNumber <= totalPages;
      const active = enabled && pageNumber === classCurrentPage;
      button.disabled = !enabled;
      if (pageItem) {
        pageItem.classList.toggle('active', active);
        pageItem.classList.toggle('disabled', !enabled);
      }
    });

    if (classPaginationPrev) classPaginationPrev.disabled = totalRows === 0 || classCurrentPage <= 1;
    if (classPaginationNext) classPaginationNext.disabled = totalRows === 0 || classCurrentPage >= totalPages;
  };

  const renderBlueprintMatrix = (matrixRows) => {
    if (!blueprintMatrixBody) return;
    if (!matrixRows || matrixRows.length === 0) {
      blueprintMatrixBody.innerHTML = '<tr><td colspan="3" class="text-center text-muted">Không có dữ liệu ma trận đề.</td></tr>';
      return;
    }
    blueprintMatrixBody.innerHTML = matrixRows.map((x) => `
      <tr>
        <td>${x.chapter}</td>
        <td>${x.level}</td>
        <td>${x.count}</td>
      </tr>
    `).join('');
  };

  const updateBlueprintDetail = () => {
    const selectedInput = blueprintSelectors().find((item) => item.checked && !item.closest('tr').classList.contains('d-none'));
    const row = selectedInput ? selectedInput.closest('tr') : null;
    const id = row ? row.getAttribute('data-blueprint-id') || '' : '';
    const detail = blueprintData[id];
    if (!detail) {
      if (selectedBlueprintName) selectedBlueprintName.textContent = 'Không có ma trận đề phù hợp';
      if (blueprintInfoName) blueprintInfoName.textContent = '-';
      if (blueprintInfoUpdated) blueprintInfoUpdated.textContent = '-';
      if (blueprintInfoSubject) blueprintInfoSubject.textContent = '-';
      if (blueprintInfoQuestionCount) blueprintInfoQuestionCount.textContent = '0 câu';
      renderBlueprintMatrix([]);
      return;
    }
    if (selectedBlueprintName) selectedBlueprintName.textContent = `Đang chọn: ${detail.name}`;
    if (blueprintInfoName) blueprintInfoName.textContent = detail.name;
    if (blueprintInfoUpdated) blueprintInfoUpdated.textContent = detail.updated;
    if (blueprintInfoSubject) blueprintInfoSubject.textContent = detail.subject;
    if (blueprintInfoQuestionCount) blueprintInfoQuestionCount.textContent = `${detail.questionCount} câu`;
    renderBlueprintMatrix(detail.matrix);
  };

  const getFilteredBlueprintRows = () => {
    const keyword = normalizeText(blueprintSearchInput ? blueprintSearchInput.value : '');
    const subject = normalizeText(blueprintSubjectFilter ? blueprintSubjectFilter.value : '');
    return blueprintRows().filter((row) => {
      const name = normalizeText(row.getAttribute('data-blueprint-name') || '');
      const rowSubject = normalizeText(row.getAttribute('data-subject') || '');
      return (keyword.length === 0 || name.includes(keyword)) &&
        (subject.length === 0 || rowSubject === subject);
    });
  };

  const renderBlueprintTable = (resetPage = false) => {
    if (!blueprintListTable) return;
    const filtered = getFilteredBlueprintRows();
    const totalRows = filtered.length;
    const totalPages = Math.max(1, Math.ceil(totalRows / blueprintPageSize));
    if (resetPage) blueprintCurrentPage = 1;
    blueprintCurrentPage = Math.min(blueprintCurrentPage, totalPages);
    const start = totalRows === 0 ? 0 : (blueprintCurrentPage - 1) * blueprintPageSize;
    const end = Math.min(start + blueprintPageSize, totalRows);
    const visible = filtered.slice(start, end);

    blueprintRows().forEach((row) => row.classList.add('d-none'));
    visible.forEach((row) => row.classList.remove('d-none'));

    if (blueprintFilterSummary) {
      blueprintFilterSummary.textContent = totalRows === 0
        ? 'Không có ma trận đề phù hợp bộ lọc.'
        : `Hiển thị ${start + 1}-${end} trên ${totalRows} ma trận đề.`;
    }

    blueprintPageButtons.forEach((button, idx) => {
      const pageNumber = idx + 1;
      const pageItem = blueprintPageItems[idx];
      const enabled = totalRows > 0 && pageNumber <= totalPages;
      const active = enabled && pageNumber === blueprintCurrentPage;
      button.disabled = !enabled;
      if (pageItem) {
        pageItem.classList.toggle('active', active);
        pageItem.classList.toggle('disabled', !enabled);
      }
    });

    if (blueprintPaginationPrev) blueprintPaginationPrev.disabled = totalRows === 0 || blueprintCurrentPage <= 1;
    if (blueprintPaginationNext) blueprintPaginationNext.disabled = totalRows === 0 || blueprintCurrentPage >= totalPages;

    const selectedVisible = blueprintSelectors().some((item) => item.checked && !item.closest('tr').classList.contains('d-none'));
    if (!selectedVisible) {
      const firstVisible = visible[0];
      if (firstVisible) {
        const firstInput = firstVisible.querySelector('.blueprint-item');
        if (firstInput) firstInput.checked = true;
      }
    }

    updateBlueprintDetail();
  };

  const updateManualSummary = () => {
    if (!manualQuestionSummary) return;
    manualQuestionSummary.textContent = `Đã chọn ${manualSelectedQuestionIds.size} câu hỏi thủ công.`;
  };

  const syncManualChapterOptions = () => {
    if (!manualQuestionChapterFilter || !manualQuestionSubjectFilter) return;
    const subject = normalizeText(manualQuestionSubjectFilter.value);
    const chapters = new Set();
    manualQuestionData.forEach((item) => {
      if (subject.length === 0 || normalizeText(item.subject) === subject) {
        chapters.add(item.chapter);
      }
    });
    const current = manualQuestionChapterFilter.value;
    manualQuestionChapterFilter.innerHTML = '<option value="">Tất cả chương</option>';
    Array.from(chapters).sort((a, b) => a.localeCompare(b)).forEach((chapter) => {
      const option = document.createElement('option');
      option.value = chapter;
      option.textContent = chapter;
      manualQuestionChapterFilter.appendChild(option);
    });
    if (current && chapters.has(current)) manualQuestionChapterFilter.value = current;
  };

  const getFilteredManualQuestions = () => {
    const subject = normalizeText(manualQuestionSubjectFilter ? manualQuestionSubjectFilter.value : '');
    const chapter = normalizeText(manualQuestionChapterFilter ? manualQuestionChapterFilter.value : '');
    const level = normalizeText(manualQuestionLevelFilter ? manualQuestionLevelFilter.value : '');
    const keyword = normalizeText(manualQuestionSearchInput ? manualQuestionSearchInput.value : '');
    return manualQuestionData.filter((item) => {
      const idx = normalizeText(`${item.id} ${item.subject} ${item.chapter} ${item.level} ${item.content}`);
      return (subject.length === 0 || normalizeText(item.subject) === subject) &&
        (chapter.length === 0 || normalizeText(item.chapter) === chapter) &&
        (level.length === 0 || normalizeText(item.level) === level) &&
        (keyword.length === 0 || idx.includes(keyword));
    });
  };

  const renderManualTable = () => {
    if (!manualQuestionTableBody) return;
    const filtered = getFilteredManualQuestions();
    if (filtered.length === 0) {
      manualQuestionTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Không có câu hỏi phù hợp bộ lọc.</td></tr>';
      updateManualSummary();
      return;
    }
    manualQuestionTableBody.innerHTML = filtered.map((item) => `
      <tr>
        <td><input class="form-check-input manual-question-item" type="checkbox" data-question-id="${item.id}" ${manualSelectedQuestionIds.has(item.id) ? 'checked' : ''}></td>
        <td>${item.id}</td>
        <td>${item.subject}</td>
        <td>${item.chapter}</td>
        <td>${item.level}</td>
        <td>${item.content}</td>
      </tr>
    `).join('');

    manualQuestionTableBody.querySelectorAll('.manual-question-item').forEach((checkbox) => {
      checkbox.addEventListener('change', (event) => {
        const id = event.target.getAttribute('data-question-id');
        if (!id) return;
        if (event.target.checked) manualSelectedQuestionIds.add(id);
        else manualSelectedQuestionIds.delete(id);
        updateManualSummary();
      });
    });
    updateManualSummary();
  };

  classSelectors().forEach((item) => item.addEventListener('change', updateSelectedClass));
  if (classSearchInput) classSearchInput.addEventListener('input', () => renderClassTable(true));
  if (classSubjectFilter) classSubjectFilter.addEventListener('change', () => renderClassTable(true));
  if (classSemesterFilter) classSemesterFilter.addEventListener('change', () => renderClassTable(true));
  if (classPaginationPrev) classPaginationPrev.addEventListener('click', () => { if (classCurrentPage > 1) { classCurrentPage -= 1; renderClassTable(); } });
  if (classPaginationNext) classPaginationNext.addEventListener('click', () => { classCurrentPage += 1; renderClassTable(); });
  classPageButtons.forEach((button, idx) => button.addEventListener('click', () => { classCurrentPage = idx + 1; renderClassTable(); }));

  blueprintSelectors().forEach((item) => item.addEventListener('change', updateBlueprintDetail));
  if (blueprintSearchInput) blueprintSearchInput.addEventListener('input', () => renderBlueprintTable(true));
  if (blueprintSubjectFilter) blueprintSubjectFilter.addEventListener('change', () => renderBlueprintTable(true));
  if (blueprintPaginationPrev) blueprintPaginationPrev.addEventListener('click', () => { if (blueprintCurrentPage > 1) { blueprintCurrentPage -= 1; renderBlueprintTable(); } });
  if (blueprintPaginationNext) blueprintPaginationNext.addEventListener('click', () => { blueprintCurrentPage += 1; renderBlueprintTable(); });
  blueprintPageButtons.forEach((button, idx) => button.addEventListener('click', () => { blueprintCurrentPage = idx + 1; renderBlueprintTable(); }));

  publicToggle.addEventListener('change', applyPublicMode);
  publicToggle.addEventListener('input', applyPublicMode);
  if (examGenerationBlueprint) examGenerationBlueprint.addEventListener('change', applyExamGenerationMode);
  if (examGenerationManual) examGenerationManual.addEventListener('change', applyExamGenerationMode);

  if (manualQuestionSubjectFilter) manualQuestionSubjectFilter.addEventListener('change', () => { syncManualChapterOptions(); renderManualTable(); });
  if (manualQuestionChapterFilter) manualQuestionChapterFilter.addEventListener('change', renderManualTable);
  if (manualQuestionLevelFilter) manualQuestionLevelFilter.addEventListener('change', renderManualTable);
  if (manualQuestionSearchInput) manualQuestionSearchInput.addEventListener('input', renderManualTable);
  if (manualQuestionClearButton) manualQuestionClearButton.addEventListener('click', () => { manualSelectedQuestionIds.clear(); renderManualTable(); });
  if (publicSubjectFilter) publicSubjectFilter.addEventListener('change', syncPublicSubjectFilters);

  updateSelectedClass();
  applyPublicMode();
  applyExamGenerationMode();
  renderClassTable(true);
  renderBlueprintTable(true);
  syncManualChapterOptions();
  renderManualTable();
})();
