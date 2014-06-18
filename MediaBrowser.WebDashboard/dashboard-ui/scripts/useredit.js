(function ($, window, document) {

    function loadUser(page, user, loggedInUser) {

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

        if (!loggedInUser.Configuration.IsAdministrator || !user.Id) {

            $('.lnkEditUserPreferencesContainer', page).hide();

        } else {

            $('.lnkEditUserPreferencesContainer', page).show();
            $('.lnkEditUserPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + user.Id);
        }

        Dashboard.setPageTitle(user.Name || Globalize.translate('AddUser'));

        $('#txtUserName', page).val(user.Name);

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

    function onSaveComplete(page) {

        Dashboard.hideLoadingMsg();

        var userId = getParameterByName("userId");

        Dashboard.validateCurrentUser(page);

        if (userId) {
            Dashboard.alert(Globalize.translate('SettingsSaved'));
        } else {
            Dashboard.navigate("userprofiles.html");
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
                onSaveComplete(page);
            });
        } else {
            ApiClient.createUser(user).done(function () {
                onSaveComplete(page);
            });
        }
    }

    function editUserPage() {

        var self = this;

        self.onSubmit = function () {

            var page = $(this).parents('.page');

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            if (!userId) {
                saveUser({
                    Configuration: {}
                }, page);
            } else {
                ApiClient.getUser(userId).done(function (result) {
                    saveUser(result, page);
                });
            }

            // Disable default form submission
            return false;
        };
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

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        var promise1;

        if (!userId) {

            var deferred = $.Deferred();

            deferred.resolveWith(null, [{
                Configuration: {
                    IsAdministrator: true,
                    EnableLiveTvManagement: true,
                    EnableLiveTvAccess: true,
                    EnableRemoteControlOfOtherUsers: true,
                    EnableMediaPlayback: true
                }
            }]);

            promise1 = deferred.promise();
        } else {

            promise1 = ApiClient.getUser(userId);
        }

        var promise2 = Dashboard.getCurrentUser();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadUser(page, response1[0] || response1, response2[0]);

        });

        $("form input:first", page).focus();
    });

})(jQuery, window, document);