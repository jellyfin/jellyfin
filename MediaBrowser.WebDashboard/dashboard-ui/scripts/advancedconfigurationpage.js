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

        if (systemInfo.CanSelfUpdate) {
            $('.fldAutomaticUpdates', page).show();
            $('.lnlAutomaticUpdateLevel', page).html(Globalize.translate('LabelAutomaticUpdateLevel'));
        } else {
            $('.fldAutomaticUpdates', page).hide();
            $('.lnlAutomaticUpdateLevel', page).html(Globalize.translate('LabelAutomaticUpdateLevelForPlugins'));
        }

        $('#chkEnableAutomaticServerUpdates', page).checked(config.EnableAutoUpdate).checkboxradio("refresh");
        $('#chkEnableAutomaticRestart', page).checked(config.EnableAutomaticRestart).checkboxradio("refresh");

        if (systemInfo.CanSelfRestart) {
            $('#fldEnableAutomaticRestart', page).show();
        } else {
            $('#fldEnableAutomaticRestart', page).hide();
        }

        $('#selectAutomaticUpdateLevel', page).val(config.SystemUpdateLevel).trigger('change');
        $('#chkDebugLog', page).checked(config.EnableDebugLevelLogging).checkboxradio("refresh");

        $('#chkRunAtStartup', page).checked(config.RunAtStartup).checkboxradio("refresh");

        $('#chkEnableDashboardResponseCache', page).checked(config.EnableDashboardResponseCaching).checkboxradio("refresh");
        $('#chkEnableMinification', page).checked(config.EnableDashboardResourceMinification).checkboxradio("refresh");
        $('#txtDashboardSourcePath', page).val(config.DashboardSourcePath).trigger('change');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().then(function (config) {

            config.EnableDebugLevelLogging = $('#chkDebugLog', form).checked();

            config.RunAtStartup = $('#chkRunAtStartup', form).checked();
            config.SystemUpdateLevel = $('#selectAutomaticUpdateLevel', form).val();
            config.EnableAutomaticRestart = $('#chkEnableAutomaticRestart', form).checked();
            config.EnableAutoUpdate = $('#chkEnableAutomaticServerUpdates', form).checked();

            config.EnableDashboardResourceMinification = $('#chkEnableMinification', form).checked();
            config.EnableDashboardResponseCaching = $('#chkEnableDashboardResponseCache', form).checked();
            config.DashboardSourcePath = $('#txtDashboardSourcePath', form).val();

            ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageshow', "#advancedConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getSystemInfo();

        Promise.all([promise1, promise2]).then(function (responses) {

            loadPage(page, responses[0], responses[1]);

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

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

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

        $('.advancedConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);
    });

})(jQuery, document, window);
