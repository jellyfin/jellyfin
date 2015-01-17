(function ($, document, window) {

    var brandingConfigKey = "branding";

    function loadPage(page, config, languageOptions) {

        $('#txtServerName', page).val(config.ServerName || '');

        $('#selectLocalizationLanguage', page).html(languageOptions.map(function (l) {

            return '<option value="' + l.Value + '">' + l.Name + '</option>';

        })).val(config.UICulture).selectmenu('refresh');

        $('#txtPortNumber', page).val(config.HttpServerPortNumber);
        $('#txtPublicPort', page).val(config.PublicPort);

        $('#chkUseHttps', page).checked(config.UseHttps).checkboxradio('refresh');
        $('#txtHttpsPort', page).val(config.HttpsPortNumber);
        $('#txtCertificatePath', page).val(config.CertificatePath);

        $('#txtDdns', page).val(config.WanDdns || '');

        $('#chkEnableUpnp', page).checked(config.EnableUPnP).checkboxradio('refresh');
        $('#txtCachePath', page).val(config.CachePath || '');

        Dashboard.hideLoadingMsg();
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

            $('#txtLoginDisclaimer', page).val(config.LoginDisclaimer || '');
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

            ApiClient.getServerConfiguration().done(function (config) {

                config.ServerName = $('#txtServerName', form).val();
                config.UICulture = $('#selectLocalizationLanguage', form).val();

                config.HttpServerPortNumber = $('#txtPortNumber', form).val();
                config.PublicPort = $('#txtPublicPort', form).val();

                config.UseHttps = $('#chkUseHttps', form).checked();
                config.HttpsPortNumber = $('#txtHttpsPort', form).val();
                config.CertificatePath = $('#txtCertificatePath', form).val();


                config.EnableUPnP = $('#chkEnableUpnp', form).checked();

                config.WanDdns = $('#txtDdns', form).val();
                config.CachePath = $('#txtCachePath', form).val();

                ApiClient.updateServerConfiguration(config).done(function () {
                    
                    ApiClient.getNamedConfiguration(brandingConfigKey).done(function (brandingConfig) {

                        brandingConfig.LoginDisclaimer = $('#txtLoginDisclaimer', form).val();

                        ApiClient.updateNamedConfiguration(brandingConfigKey, brandingConfig).done(Dashboard.processServerConfigurationUpdateResult);
                    });

                });
            });

            // Disable default form submission
            return false;
        }

    };

})(jQuery, document, window);
