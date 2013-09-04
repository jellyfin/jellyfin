(function ($, document, window) {

    function loadPage(page, config, systemInfo) {

        if (systemInfo.SupportsNativeWebSocket) {

            $('#fldWebSocketPortNumber', page).hide();
        } else {
            $('#fldWebSocketPortNumber', page).show();
        }

        $('#selectAutomaticUpdateLevel', page).val(config.SystemUpdateLevel).selectmenu('refresh').trigger('change');
        $('#txtWebSocketPortNumber', page).val(config.LegacyWebSocketPortNumber);

        $('#txtPortNumber', page).val(config.HttpServerPortNumber);
        $('#chkDebugLog', page).checked(config.EnableDebugLevelLogging).checkboxradio("refresh");

        $('#chkEnableDeveloperTools', page).checked(config.EnableDeveloperTools).checkboxradio("refresh");
        $('#chkRunAtStartup', page).checked(config.RunAtStartup).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#advancedConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getSystemInfo();

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });
        
    }).on('pageinit', "#advancedConfigurationPage", function () {

        var page = this;

        $('#selectAutomaticUpdateLevel', page).on('change', function () {

            if (this.value == "Dev") {
                $('#devBuildWarning', page).show();
            } else {
                $('#devBuildWarning', page).hide();
            }

        });
        
    });

    function advancedConfigurationPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.LegacyWebSocketPortNumber = $('#txtWebSocketPortNumber', form).val();

                config.HttpServerPortNumber = $('#txtPortNumber', form).val();
                config.EnableDebugLevelLogging = $('#chkDebugLog', form).checked();

                config.EnableDeveloperTools = $('#chkEnableDeveloperTools', form).checked();
                config.RunAtStartup = $('#chkRunAtStartup', form).checked();
                config.SystemUpdateLevel = $('#selectAutomaticUpdateLevel', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AdvancedConfigurationPage = new advancedConfigurationPage();

})(jQuery, document, window);
