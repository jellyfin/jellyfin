(function ($, document, window) {

    function onSaveComplete() {
        Dashboard.hideLoadingMsg();

        Dashboard.navigate('wizardlibrary.html');
    }

    function wizardUserPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var page = $.mobile.activePage;

            ApiClient.getUsers().done(function (users) {

                var user;

                if (users.length) {

                    user = users[0];

                    user.Name = $('#txtUsername', page).val();

                    ApiClient.updateUser(user).done(onSaveComplete);

                } else {

                    user = { Name: $('#txtUsername', page).val() };

                    ApiClient.createUser(user).done(onSaveComplete);
                }

            });

            return false;
        };
    }

    $(document).on('pageshow', "#wizardUserPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getUsers().done(function (users) {

            var user = users[0] || { Name: "User" };

            $('#txtUsername', page).val(user.Name);

            Dashboard.hideLoadingMsg();
        });

    });

    window.WizardUserPage = new wizardUserPage();

})(jQuery, document, window);