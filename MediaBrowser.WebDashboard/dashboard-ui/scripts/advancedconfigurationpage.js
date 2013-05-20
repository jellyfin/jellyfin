(function ($, document, window) {

    function loadPage(page, config, systemInfo) {

        if (systemInfo.SupportsNativeWebSocket) {

            $('#fldWebSocketPortNumber', page).hide();
        } else {
            $('#fldWebSocketPortNumber', page).show();
        }

        $('#selectAutomaticUpdateLevel', page).val(config.SystemUpdateLevel).selectmenu('refresh');
        $('#txtWebSocketPortNumber', page).val(config.LegacyWebSocketPortNumber);

        $('#txtItemsByNamePath', page).val(config.ItemsByNamePath);

        var customIbn = config.ItemsByNamePath ? true : false;
        $('#chkEnableCustomIBNPath', page).checked(customIbn).checkboxradio("refresh");

        if (customIbn) {
            $('#fieldEnterIBNPath', page).show();
            $('#txtItemsByNamePath', page).attr("required", "required");
        } else {
            $('#fieldEnterIBNPath', page).hide();
            $('#txtItemsByNamePath', page).removeAttr("required");
        }

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

        $('#btnSelectIBNPath', page).on("click.selectDirectory", function () {

            Dashboard.selectDirectory({

                callback: function (path) {

                    if (path) {
                        $('#txtItemsByNamePath', page).val(path);
                    }
                    $('#popupDirectoryPicker', page).popup("close");
                },

                header: "Select Items By Name Path",

                instruction: "Browse or enter the path to your items by name folder. The folder must be writeable."
            });
        });

        $('#chkEnableCustomIBNPath', page).on("change.showIBNText", function() {
            
            if (this.checked) {
                $('#fieldEnterIBNPath', page).show();
                $('#txtItemsByNamePath', page).attr("required", "required");
            } else {
                $('#fieldEnterIBNPath', page).hide();
                $('#txtItemsByNamePath', page).removeAttr("required");
            }

        });

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

    }).on('pagehide', "#advancedConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        $('#chkEnableCustomIBNPath', page).off("change.showIBNText");
        $('#btnSelectIBNPath', page).off("click.selectDirectory");
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
                
                if ($('#chkEnableCustomIBNPath', form).checked()) {
                    config.ItemsByNamePath = $('#txtItemsByNamePath', form).val();
                } else {
                    config.ItemsByNamePath = '';
                }

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AdvancedConfigurationPage = new advancedConfigurationPage();

})(jQuery, document, window);
