(function ($, document, window) {

    function loadPage(page, config, systemInfo) {

        var os = systemInfo.OperatingSystem.toLowerCase();

        if (os.indexOf('windows') != -1) {
            $('#windowsStartupDescription', page).show();
        } else {
            $('#windowsStartupDescription', page).hide();
        }

        if (systemInfo.SupportsAutoRunAtStartup) {
            $('#fldRunAtStartup', page).show();
        } else {
            $('#fldRunAtStartup', page).hide();
        }
        $('#chkEnableAutomaticRestart', page).checked(config.EnableAutomaticRestart).checkboxradio("refresh");

        if (systemInfo.CanSelfRestart) {
            $('#fldEnableAutomaticRestart', page).show();
        } else {
            $('#fldEnableAutomaticRestart', page).hide();
        }

        $('#selectAutomaticUpdateLevel', page).val(config.SystemUpdateLevel).selectmenu('refresh').trigger('change');
        $('#chkDebugLog', page).checked(config.EnableDebugLevelLogging).checkboxradio("refresh");

        $('#chkRunAtStartup', page).checked(config.RunAtStartup).checkboxradio("refresh");

        $('#chkEnableDashboardResponseCache', page).checked(config.EnableDashboardResponseCaching).checkboxradio("refresh");
        $('#chkEnableMinification', page).checked(config.EnableDashboardResourceMinification).checkboxradio("refresh");
        $('#txtDashboardSourcePath', page).val(config.DashboardSourcePath).trigger('change');

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

        $('#btnSelectDashboardSourcePath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtDashboardSourcePath', page).val(path);
                    }
                    picker.close();
                }
            });
        });

    });

    function advancedConfigurationPage() {

        var self = this;

        self.onSubmit = function () {
            Dashboard.showLoadingMsg();

            var form = this;

            ApiClient.getServerConfiguration().done(function (config) {

                config.EnableDebugLevelLogging = $('#chkDebugLog', form).checked();

                config.RunAtStartup = $('#chkRunAtStartup', form).checked();
                config.SystemUpdateLevel = $('#selectAutomaticUpdateLevel', form).val();
                config.EnableAutomaticRestart = $('#chkEnableAutomaticRestart', form).checked();

                config.EnableDashboardResourceMinification = $('#chkEnableMinification', form).checked();
                config.EnableDashboardResponseCaching = $('#chkEnableDashboardResponseCache', form).checked();
                config.DashboardSourcePath = $('#txtDashboardSourcePath', form).val();

                ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            // Disable default form submission
            return false;
        };
    }

    window.AdvancedConfigurationPage = new advancedConfigurationPage();

})(jQuery, document, window);
