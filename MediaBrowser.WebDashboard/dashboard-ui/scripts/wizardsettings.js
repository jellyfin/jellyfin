(function ($, document) {

    function save(page) {

        Dashboard.showLoadingMsg();

        // After saving chapter task, now save server config
        ApiClient.getServerConfiguration().done(function (config) {

            config.SaveLocalMeta = $('#chkSaveLocalMetadata', page).checked();
            config.PreferredMetadataLanguage = $('#selectLanguage', page).val();
            config.MetadataCountryCode = $('#selectCountry', page).val();

            ApiClient.updateServerConfiguration(config).done(function (result) {

                navigateToNextPage();

            });
        });

    }

    function populateCountries(page, allCountries) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = allCountries.length; i < length; i++) {

            var culture = allCountries[i];

            html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>";
        }

        $('#selectCountry', page).html(html).selectmenu("refresh");
    }

    function populateLanguages(page, allCultures) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = allCultures.length; i < length; i++) {

            var culture = allCultures[i];

            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        $('#selectLanguage', page).html(html).selectmenu("refresh");
    }

    function reloadData(page, config, cultures, countries) {

        populateLanguages(page, cultures);
        populateCountries(page, countries);

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

        Dashboard.navigate('wizardimagesettings.html');
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
