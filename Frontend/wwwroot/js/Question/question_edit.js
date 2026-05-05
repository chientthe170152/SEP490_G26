(() => {
    'use strict';
    const QE = window.QuestionEditor;
    const container = document.getElementById('editQuestionContainer');
    const saveBtn = document.getElementById('saveQuestionBtn');
    const qIdElem = document.getElementById('currentQuestionId');
    const qId = qIdElem?.value;

    if (!container || !qId) {
        return;
    }

    const item = container.querySelector('[data-question-item]');
    if (!item) {
        return;
    }

    let inputTypesData = [];
    let subjectsData = [];

    const submit = async () => {
        if (saveBtn) {
            saveBtn.disabled = true;
        }

        try {
            const payload = QE.collectPayload(item);
            payload.status = "Active";
            await apiClient.put(`/api/questions/${qId}`, payload);
            showToast('Đã cập nhật câu hỏi thành công!');

            setTimeout(() => {
                window.location.href = '/Question';
            }, 1000);
        } catch (err) {
            showToast('Lỗi: ' + (err.message || 'Không thể cập nhật.'), 'error');
            if (saveBtn) {
                saveBtn.disabled = false;
            }
        }
    };

    saveBtn?.addEventListener('click', () => {
        submit();
    });

    (async () => {
        try {
            const metadata = await apiClient.get('/api/questions/metadata');
            inputTypesData = metadata.inputTypes || [];
            subjectsData = metadata.subjects || [];

            const detail = await apiClient.get(`/api/questions/${qId}`);
            if (detail) {
                // Initialize events
                QE.initItem(item, {
                    inputTypesData,
                    subjectsData
                });
                
                // Determine subjectId from the question's bank metadata or the question detail itself.
                // The API for question detail returns SubjectCode and BankName in QuestionSummaryDto or similar.
                const bankNameInput = item.querySelector('[data-bank-name]');
                if (bankNameInput) bankNameInput.value = detail.bankName || 'Ngân hàng cá nhân';

                // Find the subject id from subjectsData based on subject code
                const sub = subjectsData.find(s => s.code === detail.subjectCode);
                const subId = sub ? sub.subjectId : null;
                
                const chapSel = item.querySelector('[data-chapter-select]');
                if (chapSel && sub) {
                    while (chapSel.options.length > 1) chapSel.remove(1);
                    (sub.chapters || []).forEach(c => {
                        chapSel.add(new Option(c.name, c.chapterId));
                    });
                }

                QE.setData(item, detail, {
                    inputTypesData,
                    subjectsData
                });
            }
        } catch (e) {
            console.error(e);
        }
    })();
})();
