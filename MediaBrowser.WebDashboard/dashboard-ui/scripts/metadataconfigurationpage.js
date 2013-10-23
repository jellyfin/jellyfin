var MetadataConfigurationPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        var page = this;

        var config;
        var allCultures;
        var allCountries;

        ApiClient.getServerConfiguration().done(function (result) {

            config = result;
            MetadataConfigurationPage.load(page, config, allCultures, allCountries);
        });

        ApiClient.getCultures().done(function (result) {

            MetadataConfigurationPage.populateLanguages(result);

            allCultures = result;
            MetadataConfigurationPage.load(page, config, allCultures, allCountries);
        });

        ApiClient.getCountries().done(function (result) {

            MetadataConfigurationPage.populateCountries(result);

            allCountries = result;
            MetadataConfigurationPage.load(page, config, allCultures, allCountries);
        });
    },

    load: function (page, config, allCultures, allCountries) {

        if (!config || !allCultures || !allCountries) {
            return;
        }

        $('#chkSaveLocal', page).checked(config.SaveLocalMeta).checkboxradio("refresh");
        $('#selectLanguage', page).val(config.PreferredMetadataLanguage).selectmenu("refresh");
        $('#selectCountry', page).val(config.MetadataCountryCode).selectmenu("refresh");
        $('#chkEnableInternetProviders', page).checked(config.EnableInternetProviders).checkboxradio("refresh");
        $('#chkEnableTmdbPersonUpdates', page).checked(config.EnableTmdbUpdates).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },
    
    populateCountries: function (allCountries) {

        var html = "";

        html += "<option value=''>None</option>";

        for (var i = 0, length = allCountries.length; i < length; i++) {

            var culture = allCountries[i];

            html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>";
        }

        $('#selectCountry', '#metadataConfigurationPage').html(html).selectmenu("refresh");
    },

    populateLanguages: function (allCultures) {
        
        var html = "";

        html += "<option value=''>None</option>";

        for (var i = 0, length = allCultures.length; i < length; i++) {

            var culture = allCultures[i];

            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        $('#selectLanguage', '#metadataConfigurationPage').html(html).selectmenu("refresh");
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.EnableTmdbUpdates = $('#chkEnableTmdbPersonUpdates', form).checked();
            config.EnableInternetProviders = $('#chkEnableInternetProviders', form).checked();
            config.SaveLocalMeta = $('#chkSaveLocal', form).checked();
            config.PreferredMetadataLanguage = $('#selectLanguage', form).val();
            config.MetadataCountryCode = $('#selectCountry', form).val();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#metadataConfigurationPage", MetadataConfigurationPage.onPageShow);