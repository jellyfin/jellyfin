var UpdatePasswordPage = {

    onPageShow: function () {
        UpdatePasswordPage.loadUser();
    },
    
    loadUser: function() {
        var page = $.mobile.activePage;

        var userid = getParameterByName("userId");

        ApiClient.getUser(userid).done(function (user) {

            Dashboard.setPageTitle(user.Name);

            if (user.HasPassword) {
                $('#btnResetPassword', page).show();
            } else {
                $('#btnResetPassword', page).hide();
            }

        });

        $('#txtCurrentPassword', page).val('');
        $('#txtNewPassword', page).val('');
        $('#txtNewPasswordConfirm', page).val('');
    },

    save: function () {

        var userId = getParameterByName("userId");

        var page = $($.mobile.activePage);
        var currentPassword = $('#txtCurrentPassword', page).val();
        var newPassword = $('#txtNewPassword', page).val();

        ApiClient.updateUserPassword(userId, currentPassword, newPassword).done(UpdatePasswordPage.saveComplete);
    },

    saveComplete: function () {

        Dashboard.hideLoadingMsg();

        Dashboard.alert("Password saved.");
        UpdatePasswordPage.loadUser();
    },

    resetPassword: function () {

        var msg = "Are you sure you wish to reset the password?";

        Dashboard.confirm(msg, "Password Reset", function (result) {

            if (result) {
                var userId = getParameterByName("userId");

                Dashboard.showLoadingMsg();

                ApiClient.resetUserPassword(userId).done(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.alert("The password has been reset.");
                    UpdatePasswordPage.loadUser();

                });
            }
        });
    },

    onSubmit: function () {
        var page = $($.mobile.activePage);

        if ($('#txtNewPassword', page).val() != $('#txtNewPasswordConfirm', page).val()) {

            Dashboard.showError("Password and password confirmation must match.");
            return false;
        }

        Dashboard.showLoadingMsg();

        UpdatePasswordPage.save();

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#updatePasswordPage", UpdatePasswordPage.onPageShow);