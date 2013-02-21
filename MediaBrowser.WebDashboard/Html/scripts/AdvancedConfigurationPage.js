var AdvancedConfigurationPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getSystemInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            AdvancedConfigurationPage.loadPage(response1[0], response2[0]);

        });
    },
    
    loadPage: function (config, systemInfo) {
        
        var page = $.mobile.activePage;
        
        if (systemInfo.SupportsNativeWebSocket) {

            $('#fldWebSocketPortNumber', page).hide();
        } else {
            $('#fldWebSocketPortNumber', page).show();
        }

        $('#txtWebSocketPortNumber', page).val(config.LegacyWebSocketPortNumber);

        $('#txtPortNumber', page).val(config.HttpServerPortNumber);
        $('#chkDebugLog', page).checked(config.EnableDebugLevelLogging).checkboxradio("refresh");

        $('#chkEnableDeveloperTools', page).checked(config.EnableDeveloperTools).checkboxradio("refresh");
        $('#chkRunAtStartup', page).checked(config.RunAtStartup).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().done(function (config) {

            config.LegacyWebSocketPortNumber = $('#txtWebSocketPortNumber', form).val();

            config.HttpServerPortNumber = $('#txtPortNumber', form).val();
            config.EnableDebugLevelLogging = $('#chkDebugLog', form).checked();

            config.EnableDeveloperTools = $('#chkEnableDeveloperTools', form).checked();
            config.RunAtStartup = $('#chkRunAtStartup', form).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#advancedConfigurationPage", AdvancedConfigurationPage.onPageShow);
