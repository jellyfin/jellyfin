define(['jQuery'], function ($) {

    var currentFile;

    function reloadUser(page) {

        var userId = getParameterByName("userId");

        Dashboard.showLoadingMsg();

        ApiClient.getUser(userId).then(function (user) {

            $('.username', page).html(user.Name);
            $('#uploadUserImage', page).val('').trigger('change');

            Dashboard.setPageTitle(user.Name);

            var imageUrl;

            if (user.PrimaryImageTag) {

                imageUrl = ApiClient.getUserImageUrl(user.Id, {
                    height: 200,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

            } else {
                imageUrl = "css/images/logindefault.png";
            }

            $('#fldImage', page).show().html('').html("<img width='140px' src='" + imageUrl + "' />");

            var showImageEditing = false;

            if (user.ConnectLinkType == 'Guest') {

                $('.connectMessage', page).show();
            }
            else if (user.PrimaryImageTag) {

                $('#headerUploadNewImage', page).show();
                showImageEditing = true;
                $('.connectMessage', page).hide();

            } else {
                showImageEditing = true;
                $('#headerUploadNewImage', page).show();
                $('.connectMessage', page).hide();
            }

            Dashboard.getCurrentUser().then(function (loggedInUser) {

                if (showImageEditing && AppInfo.supportsFileInput && (loggedInUser.Policy.IsAdministrator || user.Policy.EnableUserPreferenceAccess)) {
                    $('.newImageForm', page).show();
                    $('#btnDeleteImage', page).removeClass('hide');
                } else {
                    $('.newImageForm', page).hide();
                    $('#btnDeleteImage', page).addClass('hide');
                }
            });

            Dashboard.hideLoadingMsg();
        });

    }

    function processImageChangeResult() {

        Dashboard.hideLoadingMsg();

        var page = $($.mobile.activePage)[0];

        reloadUser(page);
    }

    function onFileReaderError(evt) {

        Dashboard.hideLoadingMsg();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                require(['toast'], function (toast) {
                    toast(Globalize.translate('FileNotFound'));
                });
                break;
            case evt.target.error.NOT_READABLE_ERR:
                require(['toast'], function (toast) {
                    toast(Globalize.translate('FileReadError'));
                });
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('FileReadError'));
                    });
                    break;
                }
        };
    }

    function onFileReaderOnloadStart(evt) {

        $('#fldUpload', $.mobile.activePage).hide();
    }

    function onFileReaderAbort(evt) {

        Dashboard.hideLoadingMsg();
        require(['toast'], function (toast) {
            toast(Globalize.translate('FileReadCancelled'));
        });
    }

    function setFiles(page, files) {

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            $('#userImageOutput', page).html('');
            $('#fldUpload', page).hide();
            currentFile = null;
            return;
        }

        currentFile = file;

        var reader = new FileReader();

        reader.onerror = onFileReaderError;
        reader.onloadstart = onFileReaderOnloadStart;
        reader.onabort = onFileReaderAbort;

        // Closure to capture the file information.
        reader.onload = function (e) {

            // Render thumbnail.
            var html = ['<img style="max-width:500px;max-height:200px;" src="', e.target.result, '" title="', escape(file.name), '"/>'].join('');

            $('#userImageOutput', page).html(html);
            $('#fldUpload', page).show();
        };

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function onImageDrop(e) {

        e.preventDefault();

        setFiles($.mobile.activePage, e.originalEvent.dataTransfer.files);

        return false;
    }

    function onImageDragOver(e) {

        e.preventDefault();

        e.originalEvent.dataTransfer.dropEffect = 'Copy';

        return false;
    }

    function myProfilePage() {

        var self = this;

        self.onImageSubmit = function () {

            var file = currentFile;

            if (!file) {
                return false;
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                return false;
            }

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.uploadUserImage(userId, 'Primary', file).then(processImageChangeResult);

            return false;
        };
    }

    window.MyProfilePage = new myProfilePage();

    $(document).on('pageinit', "#userImagePage", function () {

        var page = this;

        reloadUser(page);

        $("#userImageDropZone", page).on('dragover', onImageDragOver).on('drop', onImageDrop);

        $('#btnDeleteImage', page).on('click', function () {

            require(['confirm'], function (confirm) {

                confirm(Globalize.translate('DeleteImageConfirmation'), Globalize.translate('DeleteImage')).then(function () {

                    Dashboard.showLoadingMsg();

                    var userId = getParameterByName("userId");

                    ApiClient.deleteUserImage(userId, "primary").then(processImageChangeResult);
                });
            });
        });

        $('.newImageForm').off('submit', MyProfilePage.onImageSubmit).on('submit', MyProfilePage.onImageSubmit);

        page.querySelector('#uploadUserImage').addEventListener('change', function (e) {
            setFiles(page, e.target.files);
        });
    });

    function loadUser(page) {

        var userid = getParameterByName("userId");

        ApiClient.getUser(userid).then(function (user) {

            Dashboard.getCurrentUser().then(function (loggedInUser) {

                Dashboard.setPageTitle(user.Name);

                var showPasswordSection = true;
                var showLocalAccessSection = false;
                if (user.ConnectLinkType == 'Guest') {
                    $('.localAccessSection', page).hide();
                    showPasswordSection = false;
                }
                else if (user.HasConfiguredPassword) {
                    $('#btnResetPassword', page).show();
                    $('#fldCurrentPassword', page).show();
                    showLocalAccessSection = true;
                } else {
                    $('#btnResetPassword', page).hide();
                    $('#fldCurrentPassword', page).hide();
                }

                if (showPasswordSection && (loggedInUser.Policy.IsAdministrator || user.Policy.EnableUserPreferenceAccess)) {
                    $('.passwordSection', page).show();
                } else {
                    $('.passwordSection', page).hide();
                }

                if (showLocalAccessSection && (loggedInUser.Policy.IsAdministrator || user.Policy.EnableUserPreferenceAccess)) {
                    $('.localAccessSection', page).show();
                } else {
                    $('.localAccessSection', page).hide();
                }

                if (user.HasConfiguredEasyPassword) {
                    $('#txtEasyPassword', page).val('').attr('placeholder', '******');
                    $('#btnResetEasyPassword', page).removeClass('hide');
                } else {
                    $('#txtEasyPassword', page).val('').attr('placeholder', '');
                    $('#btnResetEasyPassword', page).addClass('hide');
                }

                page.querySelector('.chkEnableLocalEasyPassword').checked = user.Configuration.EnableLocalPassword;
            });
        });

        $('#txtCurrentPassword', page).val('');
        $('#txtNewPassword', page).val('');
        $('#txtNewPasswordConfirm', page).val('');
    }

    function saveEasyPassword(page) {

        var userId = getParameterByName("userId");

        var easyPassword = $('#txtEasyPassword', page).val();

        if (easyPassword) {

            ApiClient.updateEasyPassword(userId, easyPassword).then(function () {

                onEasyPasswordSaved(page, userId);

            });

        } else {
            onEasyPasswordSaved(page, userId);
        }
    }

    function onEasyPasswordSaved(page, userId) {

        ApiClient.getUser(userId).then(function (user) {

            user.Configuration.EnableLocalPassword = page.querySelector('.chkEnableLocalEasyPassword').checked;

            ApiClient.updateUserConfiguration(user.Id, user.Configuration).then(function () {

                Dashboard.hideLoadingMsg();

                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageSettingsSaved'));
                });
                loadUser(page);
            });
        });
    }

    function savePassword(page) {

        var userId = getParameterByName("userId");

        var currentPassword = $('#txtCurrentPassword', page).val();
        var newPassword = $('#txtNewPassword', page).val();

        ApiClient.updateUserPassword(userId, currentPassword, newPassword).then(function () {

            Dashboard.hideLoadingMsg();

            require(['toast'], function (toast) {
                toast(Globalize.translate('PasswordSaved'));
            });
            loadUser(page);

        }, function () {

            Dashboard.hideLoadingMsg();

            Dashboard.alert({
                title: Globalize.translate('HeaderLoginFailure'),
                message: Globalize.translate('MessageInvalidUser')
            });

        });

    }

    function updatePasswordPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $($.mobile.activePage)[0];

            if ($('#txtNewPassword', page).val() != $('#txtNewPasswordConfirm', page).val()) {

                require(['toast'], function (toast) {
                    toast(Globalize.translate('PasswordMatchError'));
                });
            } else {

                Dashboard.showLoadingMsg();
                savePassword(page);
            }


            // Disable default form submission
            return false;

        };

        self.onLocalAccessSubmit = function () {

            var page = $($.mobile.activePage)[0];

            Dashboard.showLoadingMsg();

            saveEasyPassword(page);

            // Disable default form submission
            return false;

        };

        self.resetPassword = function () {

            var msg = Globalize.translate('PasswordResetConfirmation');

            var page = $($.mobile.activePage)[0];

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

                        loadUser(page);

                    });
                });
            });

        };

        self.resetEasyPassword = function () {

            var msg = Globalize.translate('PinCodeResetConfirmation');

            var page = $($.mobile.activePage)[0];

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

                        loadUser(page);

                    });
                });
            });
        };
    }

    window.UpdatePasswordPage = new updatePasswordPage();

    $(document).on('pageinit', ".userPasswordPage", function () {

        var page = this;

        $('.updatePasswordForm').off('submit', UpdatePasswordPage.onSubmit).on('submit', UpdatePasswordPage.onSubmit);
        $('.localAccessForm').off('submit', UpdatePasswordPage.onLocalAccessSubmit).on('submit', UpdatePasswordPage.onLocalAccessSubmit);

    }).on('pageshow', ".userPasswordPage", function () {

        var page = this;

        loadUser(page);

    });

});