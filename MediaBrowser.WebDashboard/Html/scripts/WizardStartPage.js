var WizardStartPage = {

    gotoNextPage: function () {

        ApiClient.getUsers().done(function (users) {

            if (users.length > 1) {

                Dashboard.navigate('wizardLibrary.html');

            } else {
                Dashboard.navigate('wizardUser.html');
            }
        });

    }
};