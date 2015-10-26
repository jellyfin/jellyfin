(function ($, document, window) {

    var brandingConfigKey = "branding";
    var currentBrandingOptions;

    var currentLanguage;

    function loadPage(page, config, languageOptions) {

        if (Dashboard.lastSystemInfo) {
            Dashboard.setPageTitle(Dashboard.lastSystemInfo.ServerName);
        }

        refreshPageTitle(page);

        page.querySelector('#txtServerName').value = config.ServerName || '';
        page.querySelector('#txtCachePath').value = config.CachePath || '';

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture);

        currentLanguage = config.UICulture;

        Dashboard.hideLoadingMsg();
    }

    function refreshPageTitle(page) {

        ApiClient.getSystemInfo().done(function (systemInfo) {

            Dashboard.setPageTitle(systemInfo.ServerName);
        });
    }

    function onSubmit() {
        Dashboard.showLoadingMsg();

        var form = this;
        var page = $(form).parents('.page');

        ApiClient.getServerConfiguration().done(function (config) {

            config.ServerName = form.querySelector('#txtServerName').value;
            config.UICulture = $('#selectLocalizationLanguage', form).val();

            config.CachePath = form.querySelector('#txtCachePath').value;

            if (config.UICulture != currentLanguage) {
                Dashboard.showDashboardRefreshNotification();
            }

            ApiClient.updateServerConfiguration(config).done(function () {

                refreshPageTitle(page);

                ApiClient.getNamedConfiguration(brandingConfigKey).done(function (brandingConfig) {

                    brandingConfig.LoginDisclaimer = form.querySelector('#txtLoginDisclaimer').value;
                    brandingConfig.CustomCss = form.querySelector('#txtCustomCss').value;

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

    $(document).on('pageinit', "#dashboardGeneralPage", function () {

        var page = this;

        $('#btnSelectCachePath', page).on("click.selectDirectory", function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    callback: function (path) {

                        if (path) {
                            page.querySelector('#txtCachePath').value = path;
                        }
                        picker.close();
                    },

                    header: Globalize.translate('HeaderSelectServerCachePath'),

                    instruction: Globalize.translate('HeaderSelectServerCachePathHelp')
                });
            });
        });

        $('.dashboardGeneralForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#dashboardGeneralPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var promise1 = ApiClient.getServerConfiguration();

        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Localization/Options"));

        $.when(promise1, promise2).done(function (response1, response2) {

            loadPage(page, response1[0], response2[0]);

        });

        ApiClient.getNamedConfiguration(brandingConfigKey).done(function (config) {

            currentBrandingOptions = config;

            page.querySelector('#txtLoginDisclaimer').value = config.LoginDisclaimer || '';
            page.querySelector('#txtCustomCss').value = config.CustomCss || '';
        });

    });

})(jQuery, document, window);
