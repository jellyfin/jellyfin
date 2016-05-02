define(['jQuery'], function ($) {

    function load(page, config, allCultures, allCountries) {
        if (!config || !allCultures || !allCountries) {
            return;
        }

        page.querySelector('#chkEnableInternetProviders').checked = config.EnableInternetProviders;
        page.querySelector('#chkSaveLocal').checked = config.SaveLocalMeta;
        $('#selectLanguage', page).val(config.PreferredMetadataLanguage);
        $('#selectCountry', page).val(config.MetadataCountryCode);
        $('#chkDownloadImagesInAdvance', page).checked(config.DownloadImagesInAdvance);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().then(function (config) {

            config.EnableInternetProviders = form.querySelector('#chkEnableInternetProviders').checked;
            config.SaveLocalMeta = form.querySelector('#chkSaveLocal').checked;
            config.PreferredMetadataLanguage = $('#selectLanguage', form).val();
            config.MetadataCountryCode = $('#selectCountry', form).val();
            config.DownloadImagesInAdvance = $('#chkDownloadImagesInAdvance', form).checked();

            ApiClient.updateServerConfiguration(config).then(function() {
                Dashboard.processServerConfigurationUpdateResult();

                showConfirmMessage(config);
            });
        });

        // Disable default form submission
        return false;
    }

    function showConfirmMessage(config) {

        var msg = [];

        msg.push(Globalize.translate('MetadataSettingChangeHelp'));

        if (config.DownloadImagesInAdvance) {
            msg.push(Globalize.translate('DownloadImagesInAdvanceWarning'));
        }

        if (!msg.length) {
            return;
        }

        require(['alert'], function (alert) {
            alert({
                text: msg.join('<br/><br/>')
            });
        });
    }

    function getTabs() {
        return [
        {
            href: 'metadata.html',
            name: Globalize.translate('TabSettings')
        },
         {
             href: 'metadataimages.html',
             name: Globalize.translate('TabServices')
         },
         {
             href: 'metadatanfo.html',
             name: Globalize.translate('TabNfoSettings')
         }];
    }

    $(document).on('pageinit', "#metadataConfigurationPage", function () {

        Dashboard.showLoadingMsg();

        $('.metadataConfigurationForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#metadataConfigurationPage", function () {

        LibraryMenu.setTabs('metadata', 0, getTabs);
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

});