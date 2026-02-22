// Global Configuration
const API_BASE_URL = "https://localhost:7167";

// Setup global Ajax defaults
$.ajaxSetup({
    beforeSend: function (xhr) {
        // Automatically attach JWT Token to every request if it exists
        const token = localStorage.getItem('jwtToken');
        if (token) {
            xhr.setRequestHeader('Authorization', 'Bearer ' + token);
        }
    }
});

// Helper functions for auth
function setToken(token) {
    localStorage.setItem('jwtToken', token);
}

function getToken() {
    return localStorage.getItem('jwtToken');
}

function removeToken() {
    localStorage.removeItem('jwtToken');
}

// Global API Client for the team
const apiClient = {
    /**
     * Thực hiện một AJAX request chung
     * @param {string} method - 'GET', 'POST', 'PUT', 'DELETE'
     * @param {string} endpoint - Route API (ví dụ: '/api/auth/login')
     * @param {object} data - Dữ liệu body (cho POST/PUT)
     * @returns {Promise} Trả về Promise để dùng với .then() .catch() hoặc async/await
     */
    request: function (method, endpoint, data = null) {
        return new Promise((resolve, reject) => {
            const ajaxOptions = {
                url: API_BASE_URL + endpoint,
                type: method,
                contentType: "application/json",
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    reject({
                        xhr: xhr,
                        status: status,
                        error: error,
                        message: xhr.responseJSON?.message || "Đã có lỗi xảy ra từ máy chủ."
                    });
                }
            };

            if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
                ajaxOptions.data = JSON.stringify(data);
            }

            $.ajax(ajaxOptions);
        });
    },

    get: function (endpoint) {
        return this.request('GET', endpoint);
    },

    post: function (endpoint, data) {
        return this.request('POST', endpoint, data);
    },

    put: function (endpoint, data) {
        return this.request('PUT', endpoint, data);
    },

    delete: function (endpoint) {
        return this.request('DELETE', endpoint);
    }
};
