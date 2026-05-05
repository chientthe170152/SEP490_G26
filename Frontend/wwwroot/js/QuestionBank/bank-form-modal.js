document.addEventListener('DOMContentLoaded', () => {
    const API = API_BASE_URL; // Assuming defined globally in _Layout
    const form = document.getElementById('createBankForm');
    if (!form) return;
    
    // Clear form on close
    const modalEl = document.getElementById('createBankModal');
    if (modalEl) {
        modalEl.addEventListener('hidden.bs.modal', () => {
            form.reset();
            document.getElementById('bankIdInput').value = '0';
            document.getElementById('bankModalTitle').textContent = 'Tạo ngân hàng mới';
            document.getElementById('btnSubmitBank').textContent = 'Tạo ngân hàng';
        });
    }

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const id = parseInt(document.getElementById('bankIdInput').value) || 0;
        const name = document.getElementById('bankNameInput').value;
        const subjectId = parseInt(document.getElementById('bankSubjectSelect').value);
        const purpose = document.querySelector('input[name="kind"]:checked').value;
        const desc = document.getElementById('bankDescInput').value;
        
        const payload = {
            name: name,
            subjectId: subjectId,
            questionPurpose: parseInt(purpose),
            description: desc
        };
        
        try {
            let res;
            if (id > 0) {
                // Update
                res = await fetch(`${API}/api/question-banks/${id}`, {
                    method: 'PUT',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    },
                    body: JSON.stringify(payload)
                });
            } else {
                // Create
                res = await fetch(`${API}/api/question-banks/personal`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    },
                    body: JSON.stringify(payload)
                });
            }
            
            const data = await res.json();
            if (res.ok) {
                Toastify({ text: id > 0 ? "Đã cập nhật ngân hàng" : "Đã tạo ngân hàng mới", backgroundColor: "#16a34a" }).showToast();
                bootstrap.Modal.getInstance(modalEl).hide();
                window.dispatchEvent(new Event('bankSaved'));
            } else {
                Toastify({ text: data.message || "Lỗi lưu ngân hàng", backgroundColor: "#ea580c" }).showToast();
            }
        } catch(err) {
            console.error(err);
        }
    });
});
