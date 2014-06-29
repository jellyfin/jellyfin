(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnablePlayTo', page).checked(config.EnablePlayTo).checkboxradio("refresh");
        $('#chkEnableDlnaDebugLogging', page).checked(config.EnableDebugLogging).checkboxradio("refresh");
        $('#txtClientDiscoveryInterval', page).val(config.ClientDiscoveryIntervalSeconds);

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dlnaSettingsPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getNamedConfiguration("dlna").done(function (config) {

            loadPage(page, config);

        });

    });
    
    function onSubmit() {
        
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("dlna").done(function (config) {

            config.EnablePlayTo = $('#chkEnablePlayTo', form).checked();
            config.EnableDebugLogging = $('#chkEnableDlnaDebugLogging', form).checked();
            config.ClientDiscoveryIntervalSeconds = $('#txtClientDiscoveryInterval', form).val();

            ApiClient.updateNamedConfiguration("dlna", config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    window.DlnaSettingsPage = {
        onSubmit: onSubmit
    };

})(jQuery, document, window);
