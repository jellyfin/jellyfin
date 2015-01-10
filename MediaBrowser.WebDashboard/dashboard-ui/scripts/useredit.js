(function ($, window, document) {

    var currentUser;

    function loadUser(page, user) {

        currentUser = user;

        if (user.ConnectLinkType == 'Guest') {
            $('#fldConnectInfo', page).hide();
            $('#txtUserName', page).prop("disabled", "disabled");
        } else {
            $('#txtUserName', page).prop("disabled", "").removeAttr('disabled');
            $('#fldConnectInfo', page).show();
        }

        $('.lnkEditUserPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + user.Id);

        Dashboard.setPageTitle(user.Name);

        $('#txtUserName', page).val(user.Name);
        $('#txtConnectUserName', page).val(currentUser.ConnectUserName);

        $('#chkIsAdmin', page).checked(user.Policy.IsAdministrator).checkboxradio("refresh");

        $('#chkDisabled', page).checked(user.Policy.IsDisabled).checkboxradio("refresh");
        $('#chkIsHidden', page).checked(user.Policy.IsHidden).checkboxradio("refresh");
        $('#chkRemoteControlSharedDevices', page).checked(user.Policy.EnableSharedDeviceControl).checkboxradio("refresh");
        $('#chkEnableRemoteControlOtherUsers', page).checked(user.Policy.EnableRemoteControlOfOtherUsers).checkboxradio("refresh");
        $('#chkEnableMediaPlayback', page).checked(user.Policy.EnableMediaPlayback).checkboxradio("refresh");

        $('#chkManageLiveTv', page).checked(user.Policy.EnableLiveTvManagement).checkboxradio("refresh");
        $('#chkEnableLiveTvAccess', page).checked(user.Policy.EnableLiveTvAccess).checkboxradio("refresh");
        $('#chkEnableContentDeletion', page).checked(user.Policy.EnableContentDeletion).checkboxradio("refresh");

        $('#chkDisableUserPreferences', page).checked((!user.Policy.EnableUserPreferenceAccess)).checkboxradio("refresh");

        $('#chkEnableSync', page).checked(user.Policy.EnableSync).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page, user) {

        Dashboard.hideLoadingMsg();

        var currentConnectUsername = currentUser.ConnectUserName || '';
        var enteredConnectUsername = $('#txtConnectUserName', page).val();

        if (currentConnectUsername == enteredConnectUsername) {
            Dashboard.alert(Globalize.translate('SettingsSaved'));
        } else {

            ConnectHelper.updateUserInfo(user, $('#txtConnectUserName', page).val(), function () {

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
        user.Policy.EnableMediaPlayback = $('#chkEnableMediaPlayback', page).checked();
        user.Policy.EnableLiveTvAccess = $('#chkEnableLiveTvAccess', page).checked();
        user.Policy.EnableContentDeletion = $('#chkEnableContentDeletion', page).checked();
        user.Policy.EnableUserPreferenceAccess = !$('#chkDisableUserPreferences', page).checked();
        user.Policy.EnableSharedDeviceControl = $('#chkRemoteControlSharedDevices', page).checked();

        user.Policy.EnableSync = $('#chkEnableSync', page).checked();

        ApiClient.updateUser(user).done(function () {

            ApiClient.updateUserPolicy(user.Id, user.Policy).done(function () {

                onSaveComplete(page, user);
            });
        });
    }

    function editUserPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            getUser().done(function (result) {
                saveUser(result, page);
            });

            // Disable default form submission
            return false;
        };
    }

    function getUser() {

        var userId = getParameterByName("userId");

        return ApiClient.getUser(userId);
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        getUser().done(function (user) {

            loadUser(page, user);

        });
    }

    window.EditUserPage = new editUserPage();

    $(document).on('pagebeforeshow', "#editUserPage", function () {

        var page = this;

        loadData(page);

    });

})(jQuery, window, document);

(function () {

    window.ConnectHelper = {

        updateUserInfo: function (user, newConnectUsername, actionCallback, noActionCallback) {

            var currentConnectUsername = user.ConnectUserName || '';
            var enteredConnectUsername = newConnectUsername;

            var linkUrl = ApiClient.getUrl('Users/' + user.Id + '/Connect/Link');

            if (currentConnectUsername && !enteredConnectUsername) {

                // Remove connect info
                // Add/Update connect info
                ApiClient.ajax({

                    type: "DELETE",
                    url: linkUrl

                }).done(function () {

                    Dashboard.alert({

                        message: Globalize.translate('MessageMediaBrowserAccontRemoved'),
                        title: Globalize.translate('HeaderMediaBrowserAccountRemoved'),

                        callback: actionCallback

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

                }).done(function (result) {

                    var msgKey = result.IsPending ? 'MessagePendingMediaBrowserAccountAdded' : 'MessageMediaBrowserAccountAdded';

                    Dashboard.alert({
                        message: Globalize.translate(msgKey),
                        title: Globalize.translate('HeaderMediaBrowserAccountAdded'),

                        callback: actionCallback

                    });
                });
            } else {
                if (noActionCallback) {
                    noActionCallback();
                }
            }

        }

    };


})();