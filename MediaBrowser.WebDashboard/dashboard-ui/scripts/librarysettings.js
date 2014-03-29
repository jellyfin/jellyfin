(function ($, document, window) {

    function loadPage(page, config) {

        $('#txtSeasonZeroName', page).val(config.SeasonZeroDisplayName);

        $('#chkEnableRealtimeMonitor', page).checked(config.EnableRealtimeMonitor).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#librarySettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    });

    function librarySettingsPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.SeasonZeroDisplayName = $('#txtSeasonZeroName', form).val();

                config.EnableRealtimeMonitor = $('#chkEnableRealtimeMonitor', form).checked();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.LibrarySettingsPage = new librarySettingsPage();

})(jQuery, document, window);
