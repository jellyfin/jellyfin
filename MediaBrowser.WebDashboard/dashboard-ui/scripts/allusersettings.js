(function ($, document, window) {

    function loadPage(page, config) {

        var clients = config.ManualLoginClients;

        $('#chkMobileClients', page).checked(clients.filter(function (i) {

            return i == "Mobile";

        }).length > 0).checkboxradio("refresh");

        $('#chkMBT', page).checked(clients.filter(function (i) {

            return i == "MediaBrowserTheater";

        }).length > 0).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#allUserSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });
    });

    function allUserSettingsPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                var clients = [];

                if ($('#chkMobileClients', form).checked()) {
                    clients.push("Mobile");
                }

                if ($('#chkMBT', form).checked()) {
                    clients.push("MediaBrowserTheater");
                }

                config.ManualLoginClients = clients;

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AllUserSettingsPage = new allUserSettingsPage();

})(jQuery, document, window);
