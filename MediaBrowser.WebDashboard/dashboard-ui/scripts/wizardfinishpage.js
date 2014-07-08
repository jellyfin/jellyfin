var WizardFinishPage = {

    onFinish: function () {

        ApiClient.getServerConfiguration().done(function (config) {

            config.IsStartupWizardCompleted = true;

            ApiClient.updateServerConfiguration(config).done(function () {

                Dashboard.navigate('dashboard.html');
            });
        });
    }
};