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

            Dashboard.populateLanguages($('#selectLanguage', page), result);

            allCultures = result;
            MetadataConfigurationPage.load(page, config, allCultures, allCountries);
        });

        ApiClient.getCountries().done(function (result) {

            Dashboard.populateCountries($('#selectCountry', page), result);

            allCountries = result;
            MetadataConfigurationPage.load(page, config, allCultures, allCountries);
        });
    },

    load: function (page, config, allCultures, allCountries) {

        if (!config || !allCultures || !allCountries) {
            return;
        }

        $('#chkEnableInternetProviders', page).checked(config.EnableInternetProviders).checkboxradio("refresh");
        $('#chkSaveLocal', page).checked(config.SaveLocalMeta).checkboxradio("refresh");
        $('#selectLanguage', page).val(config.PreferredMetadataLanguage).selectmenu("refresh");
        $('#selectCountry', page).val(config.MetadataCountryCode).selectmenu("refresh");

        $('#selectImageSavingConvention', page).val(config.ImageSavingConvention).selectmenu("refresh");

        Dashboard.hideLoadingMsg();
    },
    
    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.ImageSavingConvention = $('#selectImageSavingConvention', form).val();

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