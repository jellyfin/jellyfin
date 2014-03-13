(function ($, document, window) {

    function loadPage(page, config) {

        $('#chkEnablePlayTo', page).checked(config.DlnaOptions.EnablePlayTo).checkboxradio("refresh");
        
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

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    window.DlnaSettingsPage = {
        onSubmit: onSubmit
    };

})(jQuery, document, window);
