define([], function () {

    function loadUser(page, params) {

        var userid = params.userId;

        ApiClient.getUser(userid).then(function (user) {

            Dashboard.getCurrentUser().then(function (loggedInUser) {

                LibraryMenu.setTitle(user.Name);

                var showPasswordSection = true;
                var showLocalAccessSection = false;
                if (user.ConnectLinkType == 'Guest') {
                    page.querySelector('.localAccessSection').classList.add('hide');
                    showPasswordSection = false;
                }
                else if (user.HasConfiguredPassword) {
                    page.querySelector('#btnResetPassword').classList.remove('hide');
                    page.querySelector('#fldCurrentPassword').classList.remove('hide');
                    showLocalAccessSection = true;
                } else {
                    page.querySelector('#btnResetPassword').classList.add('hide');
                    page.querySelector('#fldCurrentPassword').classList.add('hide');
                }

                if (showPasswordSection && (loggedInUser.Policy.IsAdministrator || user.Policy.EnableUserPreferenceAccess)) {
                    page.querySelector('.passwordSection').classList.remove('hide');
                } else {
                    page.querySelector('.passwordSection').classList.add('hide');
                }

                if (showLocalAccessSection && (loggedInUser.Policy.IsAdministrator || user.Policy.EnableUserPreferenceAccess)) {
                    page.querySelector('.localAccessSection').classList.remove('hide');
                } else {
                    page.querySelector('.localAccessSection').classList.add('hide');
                }

                var txtEasyPassword = page.querySelector('#txtEasyPassword');
                txtEasyPassword.value = '';

                if (user.HasConfiguredEasyPassword) {
                    txtEasyPassword.placeholder = '******';
                    page.querySelector('#btnResetEasyPassword').classList.remove('hide');
                } else {
                    txtEasyPassword.removeAttribute('placeholder');
                    txtEasyPassword.placeholder = '';
                    page.querySelector('#btnResetEasyPassword').classList.add('hide');
                }

                page.querySelector('.chkEnableLocalEasyPassword').checked = user.Configuration.EnableLocalPassword;
            });
        });

        page.querySelector('#txtCurrentPassword').value = '';
        page.querySelector('#txtNewPassword').value = '';
        page.querySelector('#txtNewPasswordConfirm').value = '';
    }

    return function (view, params) {

        function saveEasyPassword() {

            var userId = params.userId;

            var easyPassword = view.querySelector('#txtEasyPassword').value;

            if (easyPassword) {

                ApiClient.updateEasyPassword(userId, easyPassword).then(function () {

                    onEasyPasswordSaved(userId);

                });

            } else {
                onEasyPasswordSaved(userId);
            }
        }

        function onEasyPasswordSaved(userId) {

            ApiClient.getUser(userId).then(function (user) {

                user.Configuration.EnableLocalPassword = view.querySelector('.chkEnableLocalEasyPassword').checked;

                ApiClient.updateUserConfiguration(user.Id, user.Configuration).then(function () {

                    Dashboard.hideLoadingMsg();

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageSettingsSaved'));
                    });
                    loadUser(view, params);
                });
            });
        }

        function savePassword() {

            var userId = getParameterByName("userId");

            var currentPassword = view.querySelector('#txtCurrentPassword').value;
            var newPassword = view.querySelector('#txtNewPassword').value;

            ApiClient.updateUserPassword(userId, currentPassword, newPassword).then(function () {

                Dashboard.hideLoadingMsg();

                require(['toast'], function (toast) {
                    toast(Globalize.translate('PasswordSaved'));
                });
                loadUser(view, params);

            }, function () {

                Dashboard.hideLoadingMsg();

                Dashboard.alert({
                    title: Globalize.translate('HeaderLoginFailure'),
                    message: Globalize.translate('MessageInvalidUser')
                });

            });

        }

        function onSubmit(e) {

            var form = this;

            if (form.querySelector('#txtNewPassword').value != form.querySelector('#txtNewPasswordConfirm').value) {

                require(['toast'], function (toast) {
                    toast(Globalize.translate('PasswordMatchError'));
                });
            } else {

                Dashboard.showLoadingMsg();
                savePassword();
            }

            // Disable default form submission
            e.preventDefault();
            return false;
        }

        function onLocalAccessSubmit(e) {

            Dashboard.showLoadingMsg();

            saveEasyPassword();

            // Disable default form submission
            e.preventDefault();
            return false;
        }

        function resetPassword() {

            var msg = Globalize.translate('PasswordResetConfirmation');

            require(['confirm'], function (confirm) {

                confirm(msg, Globalize.translate('PasswordResetHeader')).then(function () {

                    var userId = getParameterByName("userId");

                    Dashboard.showLoadingMsg();

                    ApiClient.resetUserPassword(userId).then(function () {

                        Dashboard.hideLoadingMsg();

                        Dashboard.alert({
                            message: Globalize.translate('PasswordResetComplete'),
                            title: Globalize.translate('PasswordResetHeader')
                        });

                        loadUser(view, params);

                    });
                });
            });
        }

        function resetEasyPassword() {

            var msg = Globalize.translate('PinCodeResetConfirmation');

            require(['confirm'], function (confirm) {

                confirm(msg, Globalize.translate('HeaderPinCodeReset')).then(function () {

                    var userId = getParameterByName("userId");

                    Dashboard.showLoadingMsg();

                    ApiClient.resetEasyPassword(userId).then(function () {

                        Dashboard.hideLoadingMsg();

                        Dashboard.alert({
                            message: Globalize.translate('PinCodeResetComplete'),
                            title: Globalize.translate('HeaderPinCodeReset')
                        });

                        loadUser(view, params);

                    });
                });
            });
        }

        view.querySelector('.updatePasswordForm').addEventListener('submit', onSubmit);
        view.querySelector('.localAccessForm').addEventListener('submit', onLocalAccessSubmit);

        view.querySelector('#btnResetEasyPassword').addEventListener('click', resetEasyPassword);
        view.querySelector('#btnResetPassword').addEventListener('click', resetPassword);

        view.addEventListener('viewshow', function () {
            loadUser(view, params);
        });
    };
});