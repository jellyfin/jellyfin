(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnablePlayTo', page).checked(config.EnablePlayTo).checkboxradio("refresh");
        $('#chkEnableDlnaDebugLogging', page).checked(config.EnableDebugLogging).checkboxradio("refresh");
        $('#txtClientDiscoveryInterval', page).val(config.ClientDiscoveryIntervalSeconds);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("dlna").then(function (config) {

            config.EnablePlayTo = $('#chkEnablePlayTo', form).checked();
            config.EnableDebugLogging = $('#chkEnableDlnaDebugLogging', form).checked();
            config.ClientDiscoveryIntervalSeconds = $('#txtClientDiscoveryInterval', form).val();

            ApiClient.updateNamedConfiguration("dlna", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#dlnaSettingsPage", function () {

        $('.dlnaSettingsForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#dlnaSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("dlna").then(function (config) {

            loadPage(page, config);

        });

    });

})(jQuery, document, window);
