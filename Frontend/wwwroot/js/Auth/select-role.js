let selectedRoleId = null;

function selectRole(element) {
    // Remove selected class from all cards
    $('.role-card').removeClass('selected');

    // Add selected class to the clicked card
    $(element).addClass('selected');

    // Get the role ID
    selectedRoleId = $(element).data('role');

    // Enable continue button
    $('#btnContinue').removeClass('disabled').addClass('active').prop('disabled', false);

    // Hide error if visible
    $('#roleError').hide();
}

function continueToRegister() {
    if (!selectedRoleId) {
        $('#roleError').show();
        return;
    }

    // Save to local storage
    localStorage.setItem('pendingRegistrationRole', selectedRoleId);

    // Redirect to register form
    window.location.href = '/Auth/Register';
}

$(document).ready(function () {
    // Check if role is already selected when going back
    var existingRole = localStorage.getItem('pendingRegistrationRole');
    if (existingRole) {
        $(`.role-card[data-role="${existingRole}"]`).click();
    }
});
