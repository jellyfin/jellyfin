define(['jQuery'], function ($) {

    function loadPage(page, config, systemInfo) {

        if (systemInfo.CanSelfUpdate) {
            $('.fldAutomaticUpdates', page).show();
            $('.lnlAutomaticUpdateLevel', page).html(Globalize.translate('LabelAutomaticUpdateLevel'));
        } else {
            $('.fldAutomaticUpdates', page).hide();
            $('.lnlAutomaticUpdateLevel', page).html(Globalize.translate('LabelAutomaticUpdateLevelForPlugins'));
        }

        $('#chkEnableAutomaticServerUpdates', page).checked(config.EnableAutoUpdate);
        $('#chkEnableAutomaticRestart', page).checked(config.EnableAutomaticRestart);

        if (systemInfo.CanSelfRestart) {
            $('#fldEnableAutomaticRestart', page).show();
        } else {
            $('#fldEnableAutomaticRestart', page).hide();
        }

        $('#selectAutomaticUpdateLevel', page).val(config.SystemUpdateLevel).trigger('change');


        $('#chkEnableDashboardResponseCache', page).checked(config.EnableDashboardResponseCaching);
        $('#chkEnableMinification', page).checked(config.EnableDashboardResourceMinification);
        $('#txtDashboardSourcePath', page).val(config.DashboardSourcePath).trigger('change');

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getServerConfiguration().then(function (config) {

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
});
