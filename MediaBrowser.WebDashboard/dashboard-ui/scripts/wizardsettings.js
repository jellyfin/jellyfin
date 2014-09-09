(function ($, document) {

    function save(page) {

        Dashboard.showLoadingMsg();

        // After saving chapter task, now save server config
        ApiClient.getServerConfiguration().done(function (config) {

            config.PreferredMetadataLanguage = $('#selectLanguage', page).val();
            config.MetadataCountryCode = $('#selectCountry', page).val();
            config.SaveLocalMeta = $('#chkSaveLocalMetadata', page).checked();

            config.EnableInternetProviders = $('#chkEnableInternetProviders', page).checked();

            ApiClient.updateServerConfiguration(config).done(function (result) {

                navigateToNextPage();

            });
        });

    }

    function reloadData(page, config, cultures, countries) {

        Dashboard.populateLanguages($('#selectLanguage', page), cultures);
        Dashboard.populateCountries($('#selectCountry', page), countries);

        $('#selectLanguage', page).val(config.PreferredMetadataLanguage).selectmenu("refresh");
        $('#selectCountry', page).val(config.MetadataCountryCode).selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = ApiClient.getCultures();
        var promise3 = ApiClient.getCountries();

        $.when(promise1, promise2, promise3).done(function (response1, response2, response3) {

            reloadData(page, response1[0], response2[0], response3[0]);

        });
    }

    function navigateToNextPage() {

        ApiClient.getSystemInfo().done(function(info) {

            if (info.SupportsRunningAsService) {
                Dashboard.navigate('wizardservice.html');

            } else {
                Dashboard.navigate('wizardfinish.html');
            }
        });
    }

    $(document).on('pageshow', "#wizardSettingsPage", function () {

        var page = this;

        reload(page);
    });

    window.WizardSettingsPage = {

        onSubmit: function () {

            var form = this;

            save(form);

            return false;
        }

    };

})(jQuery, document, window);
