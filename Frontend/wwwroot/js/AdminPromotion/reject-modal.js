let currentRejectQid = null;

window.openRejectModal = function(qid) {
    currentRejectQid = qid;
    const input = document.getElementById('rejectReasonInput');
    const err = document.getElementById('rejectReasonError');
    const counter = document.getElementById('rejectReasonCounter');
    const btn = document.getElementById('btnConfirmReject');

    err.classList.add('d-none');
    
    // Prefill if already staged
    const prior = window.stagedDecisions.get(qid)?.rejectionReason ?? '';
    input.value = prior;
    counter.textContent = `${prior.length}/500`;
    btn.disabled = prior.trim().length < 10;

    openModal('rejectReasonModal');
};

document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('rejectReasonInput');
    const err = document.getElementById('rejectReasonError');
    const counter = document.getElementById('rejectReasonCounter');
    const btn = document.getElementById('btnConfirmReject');

    if (!input) return;

    input.addEventListener('input', () => {
        const len = input.value.trim().length;
        counter.textContent = `${input.value.length}/500`;
        btn.disabled = len < 10;
        if (len >= 10) err.classList.add('d-none');
    });

    btn.addEventListener('click', () => {
        const val = input.value.trim();
        if (val.length < 10) {
            err.classList.remove('d-none');
            return;
        }

        if (currentRejectQid) {
            window.stagedDecisions.set(currentRejectQid, { decision: 'reject', rejectionReason: val });
            window.rerenderCardState(currentRejectQid, window.currentRequestData.status, window.currentRequestData.items.find(x => x.questionId === currentRejectQid));
            if (typeof updateCommitBar === 'function') window.updateCommitBar(); // trigger UI update
            
            // This assumes detail.js has the main logic and exposes these functions
            // Wait, updateCommitBar is not exported. Let's just rely on the DOM for now, or export it.
            // Actually, in detail.js updateCommitBar is called inside rerenderCardState? No, it's called separately.
            // Let's call a global if exists, otherwise we'll fix it in detail.js.
            // Let me expose updateCommitBar in detail.js in the next chunk, or just call window.undoDecision.
            // detail.js already exposes window.approveItem and window.undoDecision. I should expose updateCommitBar.
            const evt = new CustomEvent('PromotionStateChanged');
            document.dispatchEvent(evt);
        }
        closeModal('rejectReasonModal');
    });
});
