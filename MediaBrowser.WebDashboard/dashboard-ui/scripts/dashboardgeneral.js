(function ($, document, window) {

    var brandingConfigKey = "branding";
    var currentBrandingOptions;

    function loadPage(page, config, languageOptions) {

        if (Dashboard.lastSystemInfo) {
            Dashboard.setPageTitle(Dashboard.lastSystemInfo.ServerName);
        }

        refreshPageTitle(page);

        $('#txtServerName', page).val(config.ServerName || '');

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture).selectmenu('refresh');

        $('#txtCachePath', page).val(config.CachePath || '');

        Dashboard.hideLoadingMsg();
    }

    function refreshPageTitle(page) {

        ApiClient.getSystemInfo().done(function (systemInfo) {

            Dashboard.setPageTitle(systemInfo.ServerName);
        });
    }

    $(document).on('pageshow', "#dashboardGeneralPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Localization/Options"));

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

        ApiClient.getNamedConfiguration(brandingConfigKey).done(function (config) {

            currentBrandingOptions = config;

            $('#txtLoginDisclaimer', page).val(config.LoginDisclaimer || '');
            $('#txtCustomCss', page).val(config.CustomCss || '');
        });

    }).on('pageinit', "#dashboardGeneralPage", function () {

        var page = this;

        $('#btnSelectCachePath', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtCachePath', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectServerCachePath'),

                instruction: Globalize.translate('HeaderSelectServerCachePathHelp')
            });
        });
    });

    window.DashboardGeneralPage = {

        onSubmit: function () {
            Dashboard.showLoadingMsg();

            var form = this;
            var page = $(form).parents('.page');

            ApiClient.getServerConfiguration().done(function (config) {

                config.ServerName = $('#txtServerName', form).val();
                config.UICulture = $('#selectLocalizationLanguage', form).val();

                config.CachePath = $('#txtCachePath', form).val();

                ApiClient.updateServerConfiguration(config).done(function () {

                    refreshPageTitle(page);

                    ApiClient.getNamedConfiguration(brandingConfigKey).done(function (brandingConfig) {

                        brandingConfig.LoginDisclaimer = $('#txtLoginDisclaimer', form).val();
                        brandingConfig.CustomCss = $('#txtCustomCss', form).val();

                        var cssChanged = currentBrandingOptions && brandingConfig.CustomCss != currentBrandingOptions.CustomCss;

                        ApiClient.updateNamedConfiguration(brandingConfigKey, brandingConfig).done(Dashboard.processServerConfigurationUpdateResult);

                        if (cssChanged) {
                            Dashboard.showDashboardRefreshNotification();
                        }
                    });

                });
            });

            // Disable default form submission
            return false;
        }

    };

})(jQuery, document, window);
