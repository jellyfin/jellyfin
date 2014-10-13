(function ($, window, document) {

    var currentUser;

    function loadUser(page, user, loggedInUser) {

        currentUser = user;

        if (!loggedInUser.Configuration.IsAdministrator) {

            $('#fldIsAdmin', page).hide();
            $('#featureAccessFields', page).hide();
            $('#accessControlDiv', page).hide();

        } else {

            $('#accessControlDiv', page).show();
            $('#fldIsAdmin', page).show();
            $('#featureAccessFields', page).show();
            $('.lnkEditUserPreferencesContainer', page).show();
        }

        if (user.Id && loggedInUser.Configuration.IsAdministrator && user.ConnectLinkType != 'Guest') {
            $('#fldConnectInfo', page).show();
        } else {
            $('#fldConnectInfo', page).hide();
        }

        if (!loggedInUser.Configuration.IsAdministrator || !user.Id) {

            $('.lnkEditUserPreferencesContainer', page).hide();

        } else {

            $('.lnkEditUserPreferencesContainer', page).show();
            $('.lnkEditUserPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + user.Id);
        }

        Dashboard.setPageTitle(user.Name || Globalize.translate('AddUser'));

        $('#txtUserName', page).val(user.Name);
        $('#txtConnectUserName', page).val(currentUser.ConnectUserName);

        $('#chkIsAdmin', page).checked(user.Configuration.IsAdministrator || false).checkboxradio("refresh");
        $('#chkBlockNotRated', page).checked(user.Configuration.BlockNotRated || false).checkboxradio("refresh");

        $('#chkDisabled', page).checked(user.Configuration.IsDisabled || false).checkboxradio("refresh");
        $('#chkIsHidden', page).checked(user.Configuration.IsHidden || false).checkboxradio("refresh");
        $('#chkEnableRemoteControlOtherUsers', page).checked(user.Configuration.EnableRemoteControlOfOtherUsers || false).checkboxradio("refresh");
        $('#chkEnableMediaPlayback', page).checked(user.Configuration.EnableMediaPlayback || false).checkboxradio("refresh");

        $('#chkManageLiveTv', page).checked(user.Configuration.EnableLiveTvManagement || false).checkboxradio("refresh");
        $('#chkEnableLiveTvAccess', page).checked(user.Configuration.EnableLiveTvAccess || false).checkboxradio("refresh");
        $('#chkEnableContentDeletion', page).checked(user.Configuration.EnableContentDeletion || false).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    function onSaveComplete(page, user) {

        Dashboard.hideLoadingMsg();

        var userId = getParameterByName("userId");

        if (userId) {

            var currentConnectUsername = currentUser.ConnectUserName || '';
            var enteredConnectUsername = $('#txtConnectUserName', page).val();

            if (currentConnectUsername == enteredConnectUsername) {
                Dashboard.alert(Globalize.translate('SettingsSaved'));
            } else {
                updateConnectInfo(page, user);
            }
        } else {
            Dashboard.navigate("userprofiles.html");
        }
    }

    function updateConnectInfo(page, user) {

        var currentConnectUsername = currentUser.ConnectUserName || '';
        var enteredConnectUsername = $('#txtConnectUserName', page).val();

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

                    callback: function () {

                        loadData(page);
                    }

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

                    callback: function () {

                        loadData(page);
                    }

                });
            });
        }
    }

    function saveUser(user, page) {

        user.Name = $('#txtUserName', page).val();

        user.Configuration.IsAdministrator = $('#chkIsAdmin', page).checked();

        user.Configuration.BlockNotRated = $('#chkBlockNotRated', page).checked();

        user.Configuration.IsHidden = $('#chkIsHidden', page).checked();
        user.Configuration.IsDisabled = $('#chkDisabled', page).checked();
        user.Configuration.EnableRemoteControlOfOtherUsers = $('#chkEnableRemoteControlOtherUsers', page).checked();
        user.Configuration.EnableLiveTvManagement = $('#chkManageLiveTv', page).checked();
        user.Configuration.EnableMediaPlayback = $('#chkEnableMediaPlayback', page).checked();
        user.Configuration.EnableLiveTvAccess = $('#chkEnableLiveTvAccess', page).checked();
        user.Configuration.EnableContentDeletion = $('#chkEnableContentDeletion', page).checked();

        var userId = getParameterByName("userId");

        if (userId) {
            ApiClient.updateUser(user).done(function () {
                onSaveComplete(page, user);
            });
        } else {
            ApiClient.createUser(user).done(function () {
                onSaveComplete(page, user);
            });
        }
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

        if (userId) {

            return ApiClient.getUser(userId);
        }

        var deferred = $.Deferred();

        deferred.resolveWith(null, [{
            Configuration: {
                IsAdministrator: false,
                EnableLiveTvManagement: true,
                EnableLiveTvAccess: true,
                EnableRemoteControlOfOtherUsers: true,
                EnableMediaPlayback: true
            }
        }]);

        return deferred.promise();
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        var promise1 = getUser();
        var promise2 = Dashboard.getCurrentUser();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadUser(page, response1[0] || response1, response2[0]);

        });
    }

    window.EditUserPage = new editUserPage();

    $(document).on('pagebeforeshow', "#editUserPage", function () {

        var page = this;

        var userId = getParameterByName("userId");

        if (userId) {
            $('#userProfileNavigation', page).show();
        } else {
            $('#userProfileNavigation', page).hide();
        }

        Dashboard.getCurrentUser().done(function (loggedInUser) {

            if (loggedInUser.Configuration.IsAdministrator) {
                $('#lnkParentalControl', page).show();
            } else {
                $('#lnkParentalControl', page).hide();
            }
        });

    }).on('pageshow', "#editUserPage", function () {

        var page = this;

        loadData(page);

        $("form input:first", page).focus();
    });

})(jQuery, window, document);