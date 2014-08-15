(function ($, document, window) {

    function loadUser(page) {

        var userid = getParameterByName("userId");

        ApiClient.getUser(userid).done(function (user) {

            Dashboard.setPageTitle(user.Name);

            if (user.HasConfiguredPassword) {
                $('#btnResetPassword', page).show();
                $('#fldCurrentPassword', page).show();
                $('.formheader', page).hide();
                $('.localAccessSection', page).show();
            } else {
                $('#btnResetPassword', page).hide();
                $('#fldCurrentPassword', page).hide();
                $('.formheader', page).show();
                $('.localAccessSection', page).hide();
            }

            $('#chkEnableLocalAccessWithoutPassword', page).checked(user.Configuration.EnableLocalPassword).checkboxradio('refresh');
        });

        $('#txtCurrentPassword', page).val('');
        $('#txtNewPassword', page).val('');
        $('#txtNewPasswordConfirm', page).val('');
    }

    function save(page) {

        var userId = getParameterByName("userId");

        ApiClient.getUser(userId).done(function (user) {

            user.Configuration.EnableLocalPassword = $('#chkEnableLocalAccessWithoutPassword', page).checked();

            ApiClient.updateUser(user).done(function() {

                Dashboard.hideLoadingMsg();

                Dashboard.alert(Globalize.translate('MessageSettingsSaved'));
                loadUser(page);
            });
        });
    }

    function savePassword(page) {

        var userId = getParameterByName("userId");

        var currentPassword = $('#txtCurrentPassword', page).val();
        var newPassword = $('#txtNewPassword', page).val();

        ApiClient.updateUserPassword(userId, currentPassword, newPassword).done(function () {

            Dashboard.hideLoadingMsg();

            Dashboard.alert(Globalize.translate('PasswordSaved'));
            loadUser(page);

        });

    }

    function updatePasswordPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $.mobile.activePage;

            if ($('#txtNewPassword', page).val() != $('#txtNewPasswordConfirm', page).val()) {

                Dashboard.showError(Globalize.translate('PasswordMatchError'));
                return false;
            }

            Dashboard.showLoadingMsg();

            savePassword(page);

            // Disable default form submission
            return false;

        };

        self.onLocalAccessSubmit = function () {

            var page = $.mobile.activePage;

            Dashboard.showLoadingMsg();

            save(page);

            // Disable default form submission
            return false;

        };

        self.resetPassword = function () {

            var msg = Globalize.translate('PasswordResetConfirmation');

            var page = $.mobile.activePage;

            Dashboard.confirm(msg, Globalize.translate('PasswordResetHeader'), function (result) {

                if (result) {
                    var userId = getParameterByName("userId");

                    Dashboard.showLoadingMsg();

                    ApiClient.resetUserPassword(userId).done(function () {

                        Dashboard.hideLoadingMsg();

                        Dashboard.alert({
                            message: Globalize.translate('PasswordResetComplete'),
                            title: Globalize.translate('PasswordResetHeader')
                        });

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