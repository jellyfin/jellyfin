(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkMobileClients', page).checked(config.RequireManualLoginForMobileApps).checkboxradio("refresh");
        $('#chkOtherApps', page).checked(config.RequireManualLoginForOtherApps).checkboxradio("refresh");

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

                config.RequireManualLoginForMobileApps = $('#chkMobileClients', form).checked();
                config.RequireManualLoginForOtherApps = $('#chkOtherApps', form).checked();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AllUserSettingsPage = new allUserSettingsPage();

})(jQuery, document, window);
