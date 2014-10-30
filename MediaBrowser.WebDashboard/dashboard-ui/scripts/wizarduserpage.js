(function ($, document, window) {

    function getUsers() {
        return ApiClient.getUsers({IsGuest: false});
    }

    function onSaveComplete(user) {

        var page = $.mobile.activePage;
        var userId = user.Id;

        var metadataKey = "xbmcmetadata";

        Dashboard.hideLoadingMsg();

        ApiClient.getNamedConfiguration(metadataKey).done(function (config) {

            config.UserId = userId;

            ApiClient.updateNamedConfiguration(metadataKey, config).done(function () {

            });
        });

        var callback = function() {

            Dashboard.navigate('wizardlibrary.html');
        };

        ConnectHelper.updateUserInfo(user, $('#txtConnectUserName', page).val(), callback, callback);
    }

    function submit(form) {

        Dashboard.showLoadingMsg();

        getUsers().done(function (users) {

            var user;

            if (users.length) {

                user = users[0];

                user.Name = $('#txtUsername', form).val();

                ApiClient.updateUser(user).done(function () {

                    onSaveComplete(user);
                });

            } else {

                ApiClient.createUser($('#txtUsername', form).val()).done(onSaveComplete);
            }

        });
    }

    function wizardUserPage() {

        var self = this;

        self.onSubmit = function () {
            var form = this;


            submit(form);

            return false;
        };
    }

    $(document).on('pageshow', "#wizardUserPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        getUsers().done(function (users) {

            var user = users[0];

            $('#txtUsername', page).val(user.Name);
            $('#txtConnectUserName', page).val(user.ConnectUserName);

            Dashboard.hideLoadingMsg();
        });

    });

    window.WizardUserPage = new wizardUserPage();

})(jQuery, document, window);