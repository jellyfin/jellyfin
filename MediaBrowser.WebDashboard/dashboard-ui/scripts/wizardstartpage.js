var WizardStartPage = {

    gotoNextPage: function () {

        ApiClient.getUsers().done(function (users) {

            if (users.length > 1) {

                Dashboard.navigate('wizardlibrary.html');

            } else {
                Dashboard.navigate('wizarduser.html');
            }
        });

    }
};