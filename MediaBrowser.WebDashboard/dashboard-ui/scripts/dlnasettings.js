(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnablePlayTo', page).checked(config.DlnaOptions.EnablePlayTo).checkboxradio("refresh");
        $('#chkEnableDlnaDebugLogging', page).checked(config.DlnaOptions.EnableDebugLogging).checkboxradio("refresh");
        $('#txtClientDiscoveryInterval', page).val(config.DlnaOptions.ClientDiscoveryIntervalSeconds);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dlnaSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (config) {

            loadPage(page, config);

        });

    });
    
    function onSubmit() {
        
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            config.DlnaOptions.EnablePlayTo = $('#chkEnablePlayTo', form).checked();
            config.DlnaOptions.EnableDebugLogging = $('#chkEnableDlnaDebugLogging', form).checked();
            config.DlnaOptions.ClientDiscoveryIntervalSeconds = $('#txtClientDiscoveryInterval', form).val();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    window.DlnaSettingsPage = {
        onSubmit: onSubmit
    };

})(jQuery, document, window);
