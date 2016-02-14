(function ($, document, window) {

    function loadPage(page, config) {

        page.querySelector('#chkEnablePlayTo').checked = config.EnablePlayTo;
        page.querySelector('#chkEnableDlnaDebugLogging').checked = config.EnableDebugLog;

        $('#txtClientDiscoveryInterval', page).val(config.ClientDiscoveryIntervalSeconds);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("dlna").then(function (config) {

            config.EnablePlayTo = form.querySelector('#chkEnablePlayTo').checked;
            config.EnableDebugLog = form.querySelector('#chkEnableDlnaDebugLogging').checked;

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
