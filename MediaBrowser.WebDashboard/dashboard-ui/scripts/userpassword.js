(function ($, document, window) {

    function loadUser(page) {

        var userid = getParameterByName("userId");

        ApiClient.getUser(userid).done(function (user) {

            Dashboard.setPageTitle(user.Name);

            if (user.HasPassword) {
                $('#btnResetPassword', page).show();
                $('#fldCurrentPassword', page).show();
                $('.formheader', page).hide();
            } else {
                $('#btnResetPassword', page).hide();
                $('#fldCurrentPassword', page).hide();
                $('.formheader', page).html('Create Password').show();
            }

        });

        $('#txtCurrentPassword', page).val('');
        $('#txtNewPassword', page).val('');
        $('#txtNewPasswordConfirm', page).val('');
    }

    function save(page) {

        var userId = getParameterByName("userId");

        var currentPassword = $('#txtCurrentPassword', page).val();
        var newPassword = $('#txtNewPassword', page).val();

        ApiClient.updateUserPassword(userId, currentPassword, newPassword).done(function () {

            Dashboard.hideLoadingMsg();

            Dashboard.alert("Password saved.");
            loadUser(page);

        });

    }

    function updatePasswordPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $.mobile.activePage;

            if ($('#txtNewPassword', page).val() != $('#txtNewPasswordConfirm', page).val()) {

                Dashboard.showError("Password and password confirmation must match.");
                return false;
            }

            Dashboard.showLoadingMsg();

            save(page);

            // Disable default form submission
            return false;

        };

        self.resetPassword = function () {

            var msg = "Are you sure you wish to reset the password?";

            var page = $.mobile.activePage;

            Dashboard.confirm(msg, "Password Reset", function (result) {

                if (result) {
                    var userId = getParameterByName("userId");

                    Dashboard.showLoadingMsg();

                    ApiClient.resetUserPassword(userId).done(function () {

                        Dashboard.hideLoadingMsg();
                        Dashboard.alert("The password has been reset.");
                        loadUser(page);

                    });
                }
            });

        };
    }

    window.UpdatePasswordPage = new updatePasswordPage();

    $(document).on('pagebeforeshow', "#updatePasswordPage", function () {

        var page = this;

        Dashboard.getCurrentUser().done(function (loggedInUser) {

            if (loggedInUser.Configuration.IsAdministrator) {
                $('#lnkParentalControl', page).show();
            } else {
                $('#lnkParentalControl', page).hide();
            }
        });

    }).on('pageshow', "#updatePasswordPage", function () {

        var page = this;

        loadUser(page);

    });

})(jQuery, document, window);