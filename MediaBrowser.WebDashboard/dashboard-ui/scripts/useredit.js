define(['jQuery'], function ($) {

    var currentUser;

    function loadUser(page, user) {

        currentUser = user;

        if (user.Policy.IsDisabled) {
            $('.disabledUserBanner', page).show();
        } else {
            $('.disabledUserBanner', page).hide();
        }

        if (user.ConnectLinkType == 'Guest') {
            $('#fldConnectInfo', page).hide();
            $('#txtUserName', page).prop("disabled", "disabled");
        } else {
            $('#txtUserName', page).prop("disabled", "").removeAttr('disabled');
            $('#fldConnectInfo', page).show();
        }

        $('.lnkEditUserPreferences', page).attr('href', 'mypreferencesmenu.html?userId=' + user.Id);

        Dashboard.setPageTitle(user.Name);

        $('#txtUserName', page).val(user.Name);
        $('#txtConnectUserName', page).val(currentUser.ConnectUserName);

        $('#chkIsAdmin', page).checked(user.Policy.IsAdministrator);

        $('#chkDisabled', page).checked(user.Policy.IsDisabled);
        $('#chkIsHidden', page).checked(user.Policy.IsHidden);
        $('#chkRemoteControlSharedDevices', page).checked(user.Policy.EnableSharedDeviceControl);
        $('#chkEnableRemoteControlOtherUsers', page).checked(user.Policy.EnableRemoteControlOfOtherUsers);

        $('#chkEnableDownloading', page).checked(user.Policy.EnableContentDownloading);

        $('#chkManageLiveTv', page).checked(user.Policy.EnableLiveTvManagement);
        $('#chkEnableLiveTvAccess', page).checked(user.Policy.EnableLiveTvAccess);
        $('#chkEnableContentDeletion', page).checked(user.Policy.EnableContentDeletion);

        $('#chkDisableUserPreferences', page).checked((!user.Policy.EnableUserPreferenceAccess));

        $('#chkEnableMediaPlayback', page).checked(user.Policy.EnableMediaPlayback);
        $('#chkEnableAudioPlaybackTranscoding', page).checked(user.Policy.EnableAudioPlaybackTranscoding);
        $('#chkEnableVideoPlaybackTranscoding', page).checked(user.Policy.EnableVideoPlaybackTranscoding);

        $('#chkEnableSync', page).checked(user.Policy.EnableSync);
        $('#chkEnableSyncTranscoding', page).checked(user.Policy.EnableSyncTranscoding);
        $('#chkEnableSharing', page).checked(user.Policy.EnablePublicSharing);

        Dashboard.hideLoadingMsg();
    }

    function updateUserInfo(user, newConnectUsername, actionCallback, noActionCallback) {
        var currentConnectUsername = user.ConnectUserName || '';
        var enteredConnectUsername = newConnectUsername;

        var linkUrl = ApiClient.getUrl('Users/' + user.Id + '/Connect/Link');

        if (currentConnectUsername && !enteredConnectUsername) {

            // Remove connect info
            // Add/Update connect info
            ApiClient.ajax({

                type: "DELETE",
                url: linkUrl

            }).then(function () {

                Dashboard.alert({

                    message: Globalize.translate('MessageEmbyAccontRemoved'),
                    title: Globalize.translate('HeaderEmbyAccountRemoved'),

                    callback: actionCallback

                });
            }, function () {

                Dashboard.alert({

                    message: Globalize.translate('ErrorRemovingEmbyConnectAccount')

                });
            });

        }
        else if (currentConnectUsername != enteredConnectUsername) {

            // Add/Update connect info
            ApiClient.ajax({
                type: "POST",
                url: linkUrl,
                data: {
                    ConnectUsername: enteredConnectUsername
                },
                dataType: 'json'

            }).then(function (result) {

                var msgKey = result.IsPending ? 'MessagePendingEmbyAccountAdded' : 'MessageEmbyAccountAdded';

                Dashboard.alert({
                    message: Globalize.translate(msgKey),
                    title: Globalize.translate('HeaderEmbyAccountAdded'),

                    callback: actionCallback

                });

            }, function () {

                showEmbyConnectErrorMessage('.');
            });

        } else {
            if (noActionCallback) {
                noActionCallback();
            }
        }
    }

    function showEmbyConnectErrorMessage(username) {

        var html;
        var text;

        if (username) {

            html = Globalize.translate('ErrorAddingEmbyConnectAccount1', '<a href="https://emby.media/connect" target="_blank">https://emby.media/connect</a>');
            html += '<br/><br/>' + Globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

            text = Globalize.translate('ErrorAddingEmbyConnectAccount1', 'https://emby.media/connect');
            text += '\n\n' + Globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

        } else {
            html = text = Globalize.translate('DefaultErrorMessage');
        }

        require(['alert'], function (alert) {
            alert({
                text: text,
                html: html
            });
        });
    }

    function onSaveComplete(page, user) {

        Dashboard.hideLoadingMsg();

        var currentConnectUsername = currentUser.ConnectUserName || '';
        var enteredConnectUsername = $('#txtConnectUserName', page).val();

        if (currentConnectUsername == enteredConnectUsername) {
            require(['toast'], function (toast) {
                toast(Globalize.translate('SettingsSaved'));
            });
        } else {

            updateUserInfo(user, $('#txtConnectUserName', page).val(), function () {

                loadData(page);
            });
        }
    }

    function saveUser(user, page) {

        user.Name = $('#txtUserName', page).val();

        user.Policy.IsAdministrator = $('#chkIsAdmin', page).checked();

        user.Policy.IsHidden = $('#chkIsHidden', page).checked();
        user.Policy.IsDisabled = $('#chkDisabled', page).checked();
        user.Policy.EnableRemoteControlOfOtherUsers = $('#chkEnableRemoteControlOtherUsers', page).checked();
        user.Policy.EnableLiveTvManagement = $('#chkManageLiveTv', page).checked();
        user.Policy.EnableLiveTvAccess = $('#chkEnableLiveTvAccess', page).checked();
        user.Policy.EnableContentDeletion = $('#chkEnableContentDeletion', page).checked();
        user.Policy.EnableUserPreferenceAccess = !$('#chkDisableUserPreferences', page).checked();
        user.Policy.EnableSharedDeviceControl = $('#chkRemoteControlSharedDevices', page).checked();

        user.Policy.EnableMediaPlayback = $('#chkEnableMediaPlayback', page).checked();
        user.Policy.EnableAudioPlaybackTranscoding = $('#chkEnableAudioPlaybackTranscoding', page).checked();
        user.Policy.EnableVideoPlaybackTranscoding = $('#chkEnableVideoPlaybackTranscoding', page).checked();

        user.Policy.EnableContentDownloading = $('#chkEnableDownloading', page).checked();

        user.Policy.EnableSync = $('#chkEnableSync', page).checked();
        user.Policy.EnableSyncTranscoding = $('#chkEnableSyncTranscoding', page).checked();
        user.Policy.EnablePublicSharing = $('#chkEnableSharing', page).checked();

        ApiClient.updateUser(user).then(function () {

            ApiClient.updateUserPolicy(user.Id, user.Policy).then(function () {

                onSaveComplete(page, user);
            });
        });
    }

    function onSubmit() {
        var page = $(this).parents('.page');

        Dashboard.showLoadingMsg();

        getUser().then(function (result) {
            saveUser(result, page);
        });

        // Disable default form submission
        return false;
    }

    function getUser() {

        var userId = getParameterByName("userId");

        return ApiClient.getUser(userId);
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        getUser().then(function (user) {

            loadUser(page, user);
        });
    }

    $(document).on('pageinit', "#editUserPage", function () {

        $('.editUserProfileForm').off('submit', onSubmit).on('submit', onSubmit);

        this.querySelector('.sharingHelp').innerHTML = Globalize.translate('OptionAllowLinkSharingHelp', 30);

    }).on('pagebeforeshow', "#editUserPage", function () {

        var page = this;

        loadData(page);

    });

});