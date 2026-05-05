window.AdminUI = (function () {
    const ERROR_MAP = {
        // Curriculum
        'SEMESTER_CODE_DUPLICATE':     'Mã kỳ học đã tồn tại.',
        'SEMESTER_DATE_INVALID':       'Ngày kết thúc phải sau ngày bắt đầu.',
        'SEMESTER_NOT_FOUND':          'Không tìm thấy kỳ học.',
        'SEMESTER_ALREADY_CLOSED':     'Kỳ học này đã được đóng.',
        'SEMESTER_CONCURRENT_UPDATE':  'Dữ liệu đã thay đổi. Vui lòng tải lại trang.',
        'SEMESTER_CLOSED':             'Kỳ học đã đóng, không thể tạo lớp mới.',
        'SUBJECT_CODE_DUPLICATE':      'Mã môn học đã tồn tại.',
        'SUBJECT_NOT_FOUND':           'Không tìm thấy môn học.',
        'SUBJECT_ALREADY_CLOSED':      'Môn học này đã được đóng.',
        'SUBJECT_CONCURRENT_UPDATE':   'Dữ liệu đã thay đổi. Vui lòng tải lại trang.',
        'CHAPTER_NAME_DUPLICATE':      'Tên chương đã tồn tại trong môn này.',
        'CHAPTER_NOT_FOUND':           'Không tìm thấy chương.',
        'CHAPTER_ALREADY_DELETED':     'Chương đã được xoá.',
        'CHAPTER_CONCURRENT_UPDATE':   'Dữ liệu đã thay đổi. Vui lòng tải lại trang.',
        'EXAM_TIME_OUT_OF_SEMESTER':   'Thời gian đề thi phải nằm trong khoảng kỳ học.',
        
        // Users
        'ADMIN_USER_EMAIL_EXISTS':     'Email đã tồn tại.',
        'ADMIN_USER_INVALID_ROLE':     'Chỉ tạo được tài khoản Giáo viên hoặc Học sinh.',
        'ADMIN_USER_EMAIL_SEND_FAILED':'Không gửi được email mật khẩu — kiểm tra cấu hình SMTP và thử lại.',
        'ADMIN_USER_CANNOT_LOCK_SELF': 'Không thể khóa chính tài khoản của bạn.',
        'ADMIN_USER_CANNOT_MODIFY_ADMIN':'Không thể thao tác trên tài khoản admin khác.',
        'ADMIN_USER_NOT_FOUND':        'Không tìm thấy tài khoản.',
        'VALIDATION':                  'Dữ liệu không hợp lệ — kiểm tra lại các trường đã nhập.'
    };

    const FIELD_MAP = {
        'ADMIN_USER_EMAIL_EXISTS': 'emailError'
    };

    function showNotice(kind, title, message) {
        const stage = document.getElementById('modal-stage-notice');
        if (!stage) {
            // Fallback to toast if modal not found
            if (typeof showToast === 'function') {
                showToast(message, kind);
            } else {
                alert(`${title}: ${message}`);
            }
            return;
        }
        
        const modal = stage.querySelector('.modal');
        modal.classList.remove('modal-success', 'modal-error');
        modal.classList.add(kind === 'error' ? 'modal-error' : 'modal-success');
        
        document.getElementById('notice-title').textContent = title;
        document.getElementById('notice-message').textContent = message;
        
        if (typeof openModal === 'function') {
            openModal('modal-stage-notice');
        } else {
            stage.classList.remove('hidden');
        }
    }

    function translateError(code, fallback) {
        return ERROR_MAP[code] || fallback || 'Có lỗi xảy ra. Vui lòng thử lại.';
    }

    function showError(err, fallback) {
        const code = err?.xhr?.responseJSON?.code;
        const msg = ERROR_MAP[code] ?? err?.message ?? fallback;
        const field = FIELD_MAP[code];
        
        if (field) {
            const el = document.getElementById(field);
            if (el) {
                el.textContent = msg;
                el.classList.add('help-error');
                return;
            }
        }
        showNotice('error', 'Lỗi', msg);
    }

    return { showNotice, translateError, showError };
})();

document.addEventListener('DOMContentLoaded', async () => {
    const badge = document.getElementById('pending-promotion-count');
    const headerBadge = document.getElementById('header-pending-badge');
    if (badge || headerBadge) {
        try {
            // Check pending requests
            const res = await apiClient.get('/api/admin/promotion-requests?status=1&pageSize=1');
            const count = res.totalCount || 0;
            if (count > 0) {
                if (badge) {
                    badge.textContent = count;
                    badge.style.display = 'inline-block';
                }
                if (headerBadge) {
                    headerBadge.textContent = `${count} chờ duyệt`;
                    headerBadge.style.display = 'inline-block';
                }
            }
        } catch (e) {
            console.error('Failed to load pending promotion count', e);
        }
    }
});
