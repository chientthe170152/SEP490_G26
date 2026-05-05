document.addEventListener('DOMContentLoaded', () => {
    const API = API_BASE_URL;
    const btnPromote = document.getElementById('btnPromote');
    const modalEl = document.getElementById('promoteModal');
    if (!btnPromote || !modalEl) return;
    
    const countBadge = document.getElementById('promoteCountBadge');
    const subjName = document.getElementById('promoteSubjectName');
    const targetContainer = document.getElementById('promoteTargetsContainer');
    const noteInput = document.getElementById('promoteNote');
    const counter = document.getElementById('promoteCounter');
    const btnSubmit = document.getElementById('btnSubmitPromote');
    
    let targetBanks = [];
    
    btnPromote.addEventListener('click', async () => {
        const selected = window.selectedQuestionsForPromote || [];
        const bank = window.currentBankContext;
        if (!bank || selected.length === 0) return;
        
        countBadge.textContent = selected.length;
        subjName.textContent = bank.subjectName;
        noteInput.value = '';
        counter.textContent = '0';
        btnSubmit.disabled = true;
        
        // Fetch shared banks for this subject
        targetContainer.innerHTML = '<div class="text-muted small py-2">Đang tải kho chung...</div>';
        new bootstrap.Modal(modalEl).show();
        
        try {
            const res = await fetch(`${API}/api/question-banks?ownerType=2&subjectId=${bank.subjectId}&pageSize=10`);
            const data = await res.json();
            targetBanks = data.items || data.data?.items || [];
            
            renderTargets(bank.questionPurpose);
            
        } catch(e) {
            console.error(e);
            targetContainer.innerHTML = '<div class="text-danger small py-2">Lỗi tải danh sách kho chung.</div>';
        }
    });
    
    noteInput.addEventListener('input', () => {
        counter.textContent = noteInput.value.length;
    });
    
    function renderTargets(sourcePurpose) {
        targetContainer.innerHTML = '';
        if (targetBanks.length === 0) {
            targetContainer.innerHTML = '<div class="alert alert-warning small">Không tìm thấy kho chung nào cho môn học này.</div>';
            return;
        }
        
        targetBanks.forEach((tb, i) => {
            const isMatch = tb.questionPurpose === sourcePurpose;
            const purposeText = tb.questionPurpose === 1 ? 'Kiểm tra' : 'Luyện tập';
            const isDefault = isMatch;
            
            const label = document.createElement('label');
            label.className = `target-radio-card ${isDefault ? 'selected' : ''}`;
            
            const inputHtml = `<input type="radio" name="targetBank" value="${tb.questionBankId}" ${isDefault ? 'checked' : ''} hidden>`;
            
            let descHtml = isMatch 
                ? `Kho mặc định khớp loại <strong>${purposeText}</strong> của bank gốc. Khuyến nghị chọn.`
                : `Khác loại với bank nguồn — chỉ chọn nếu chủ ý.`;
                
            label.innerHTML = `
                ${inputHtml}
                <div class="target-radio-content">
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <div class="name">${escapeHtml(tb.name)}</div>
                        <span class="count">${tb.questionCount} câu</span>
                    </div>
                    <div class="desc">${descHtml}</div>
                </div>
            `;
            
            label.addEventListener('click', () => {
                document.querySelectorAll('.target-radio-card').forEach(c => c.classList.remove('selected'));
                label.classList.add('selected');
                label.querySelector('input').checked = true;
                checkSubmit();
            });
            
            targetContainer.appendChild(label);
        });
        
        checkSubmit();
    }
    
    function checkSubmit() {
        const selected = document.querySelector('input[name="targetBank"]:checked');
        btnSubmit.disabled = !selected;
    }
    
    btnSubmit.addEventListener('click', async () => {
        const sourceBankId = window.currentBankContext.questionBankId;
        const targetBankId = parseInt(document.querySelector('input[name="targetBank"]:checked').value);
        const qIds = window.selectedQuestionsForPromote || [];
        const note = noteInput.value;
        
        const payload = {
            sourcePersonalBankId: sourceBankId,
            targetSharedBankId: targetBankId,
            questionIds: qIds,
            note: note
        };
        
        btnSubmit.disabled = true;
        try {
            const res = await fetch(`${API}/api/promotion-requests`, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify(payload)
            });
            
            const data = await res.json();
            if (res.ok) {
                Toastify({ text: "Đã gửi đề xuất thành công, vui lòng chờ admin duyệt.", backgroundColor: "#16a34a" }).showToast();
                bootstrap.Modal.getInstance(modalEl).hide();
                window.dispatchEvent(new Event('promotionSuccess'));
            } else {
                Toastify({ text: data.message || "Lỗi gửi đề xuất", backgroundColor: "#ea580c" }).showToast();
                btnSubmit.disabled = false;
            }
        } catch(e) {
            console.error(e);
            btnSubmit.disabled = false;
        }
    });
    
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.innerText = text;
        return div.innerHTML;
    }
});
