(function () {

    function load(page, config, allCultures, allCountries) {
        if (!config || !allCultures || !allCountries) {
            return;
        }

        $('#chkEnableInternetProviders', page).checked(config.EnableInternetProviders).checkboxradio("refresh");
        $('#chkSaveLocal', page).checked(config.SaveLocalMeta).checkboxradio("refresh");
        $('#selectLanguage', page).val(config.PreferredMetadataLanguage);
        $('#selectCountry', page).val(config.MetadataCountryCode);

        $('#selectImageSavingConvention', page).val(config.ImageSavingConvention);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().then(function (config) {

            config.ImageSavingConvention = $('#selectImageSavingConvention', form).val();

            config.EnableInternetProviders = $('#chkEnableInternetProviders', form).checked();
            config.SaveLocalMeta = $('#chkSaveLocal', form).checked();
            config.PreferredMetadataLanguage = $('#selectLanguage', form).val();
            config.MetadataCountryCode = $('#selectCountry', form).val();

            ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinit', "#metadataConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        $('.metadataConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var config;
        var allCultures;
        var allCountries;

        ApiClient.getServerConfiguration().then(function (result) {

            config = result;
            load(page, config, allCultures, allCountries);
        });

        function populateLanguages(select, languages) {

            var html = "";

            html += "<option value=''></option>";

            for (var i = 0, length = languages.length; i < length; i++) {

                var culture = languages[i];

                html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
            }

            select.innerHTML = html;
        }

        function populateCountries(select, allCountries) {

            var html = "";

            html += "<option value=''></option>";

            for (var i = 0, length = allCountries.length; i < length; i++) {

                var culture = allCountries[i];

                html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>";
            }

            select.innerHTML = html;
        }

        ApiClient.getCultures().then(function (result) {

            populateLanguages(page.querySelector('#selectLanguage'), result);

            allCultures = result;
            load(page, config, allCultures, allCountries);
        });

        ApiClient.getCountries().then(function (result) {

            populateCountries(page.querySelector('#selectCountry'), result);

            allCountries = result;
            load(page, config, allCultures, allCountries);
        });
    });

})();