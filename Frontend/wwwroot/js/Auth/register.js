$(document).ready(function () {
    // Populate readonly email field from LocalStorage (saved during Google Login)
    const tempEmail = localStorage.getItem('tempGoogleEmail');
    if (tempEmail) {
        $('#Email').val(tempEmail);
    } else {
        // If there's no temp email, something is wrong (direct access). Redirect to login.
        window.location.href = '/Auth/Login';
    }

    // Handle form submission
    $('#registerForm').on('submit', function (e) {
        e.preventDefault();

        $('#formError').text('');

        const idToken = localStorage.getItem('tempGoogleToken');
        const roleId = $('#RoleId').val();

        if (!roleId) {
            alert("Vui lòng chọn vai trò của bạn");
            return;
        }

        if (!idToken) {
            alert("Phiên đăng nhập không hợp lệ, vui lòng đăng nhập lại Google.");
            window.location.href = '/Auth/Login';
            return;
        }

        const requestData = {
            IdToken: idToken,
            RoleId: parseInt(roleId)
        };

        $.ajax({
            url: API_BASE_URL + "/api/auth/google-register",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(requestData),
            success: function (response) {
                if (response.token) {
                    // Successful registration & login
                    setToken(response.token);

                    // Cleanup temporary variables
                    localStorage.removeItem('tempGoogleToken');
                    localStorage.removeItem('tempGoogleEmail');

                    window.location.href = '/';
                }
            },
            error: function (xhr) {
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    $('#formError').text(xhr.responseJSON.message);
                } else {
                    $('#formError').text("Đăng ký không thành công. Vui lòng thử lại.");
                }
            }
        });
    });
});
