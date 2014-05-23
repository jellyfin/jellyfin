(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkMobileClients', page).checked(config.RequireMobileManualLogin).checkboxradio("refresh");
        $('#chkOtherApps', page).checked(config.RequireNonMobileManualLogin).checkboxradio("refresh");

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

                config.RequireMobileManualLogin = $('#chkMobileClients', form).checked();
                config.RequireNonMobileManualLogin = $('#chkOtherApps', form).checked();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AllUserSettingsPage = new allUserSettingsPage();

})(jQuery, document, window);
