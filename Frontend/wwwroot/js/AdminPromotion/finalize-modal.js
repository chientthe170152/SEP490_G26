window.openFinalizeModal = function() {
    const rid = window.REQUEST_ID;
    if (!rid) return;

    let app = 0, rej = 0;
    window.stagedDecisions.forEach(v => {
        if (v.decision === 'approve') app++;
        if (v.decision === 'reject') rej++;
    });

    document.getElementById('finalizeModalTitle').textContent = `Xác nhận hoàn tất yêu cầu PR-${rid}`;
    document.getElementById('finalizeModalBody').innerHTML = `Sau khi xác nhận, hệ thống sẽ áp dụng <strong class="text-success">${app} duyệt</strong> và <strong class="text-danger">${rej} từ chối</strong>, đóng yêu cầu và thông báo cho giáo viên.`;

    openModal('finalizeConfirmModal');
};

document.addEventListener('DOMContentLoaded', () => {
    const btn = document.getElementById('btnConfirmFinalize');
    if (!btn) return;

    btn.addEventListener('click', async () => {
        const rid = window.REQUEST_ID;
        const decisions = [];
        for (const [qid, dec] of window.stagedDecisions) {
            decisions.push({
                questionId: qid,
                decision: dec.decision,
                rejectionReason: dec.decision === 'reject' ? dec.rejectionReason : null,
            });
        }

        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';

        try {
            const res = await apiClient.post(`/api/admin/promotion-requests/${rid}/finalize`, {
                concurrencyStamp: window.requestConcurrencyStamp,
                decisions: decisions
            });

            closeModal('finalizeConfirmModal');
            if (typeof showToast === 'function') showToast('Đã hoàn tất yêu cầu PR-' + rid, 'success');
            
            // Tải lại chi tiết để hiển thị trạng thái Resolved
            if (window.reloadDetail) await window.reloadDetail();

        } catch (err) {
            closeModal('finalizeConfirmModal');
            const code = err?.xhr?.responseJSON?.code || err?.xhr?.responseJSON?.title;
            const map = {
                'QPR_REJECTION_REASON_REQUIRED': 'Câu từ chối phải có lý do (bắt buộc).',
                'QPR_FINALIZE_INCOMPLETE':       'Còn câu chưa có quyết định. Vui lòng xử lý hết.',
                'QPR_FINALIZE_UNKNOWN_ITEM':     'Có câu trong payload không thuộc yêu cầu này.',
                'QPR_FINALIZE_DUPLICATE_ITEM':   'Có câu xuất hiện 2 lần trong payload — kiểm tra lại.',
                'QPR_FINALIZE_INVALID_DECISION': 'Giá trị quyết định không hợp lệ.',
                'QPR_QUESTION_NOT_IN_SOURCE':    'Có câu đã rời bank gốc trong lúc bạn rà soát. Tải lại trang.',
                'QPR_ALREADY_RESOLVED':          'Yêu cầu này đã được hoàn tất bởi admin khác.',
                'QPR_ALREADY_WITHDRAWN':         'Giáo viên đã rút yêu cầu này.',
                'QPR_CONCURRENT_UPDATE':         'Có người vừa cập nhật yêu cầu. Tải lại để xem trạng thái mới.',
            };
            
            const fallbackMsg = map[code];
            if (fallbackMsg) {
                if (window.AdminUI) window.AdminUI.showNotice('error', 'Lỗi', fallbackMsg);
                else alert(fallbackMsg);
            } else {
                if (window.AdminUI) window.AdminUI.showError(err);
                else alert('Có lỗi xảy ra. Vui lòng thử lại.');
            }
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-check-lg"></i> Xác nhận hoàn tất';
        }
    });
});
