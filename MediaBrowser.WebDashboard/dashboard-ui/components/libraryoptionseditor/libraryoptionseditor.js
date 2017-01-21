define(['globalize', 'emby-checkbox', 'emby-select'], function (globalize) {
    'use strict';

    function populateLanguages(select) {

        return ApiClient.getCultures().then(function (languages) {

            var html = "";

            html += "<option value=''></option>";

            for (var i = 0, length = languages.length; i < length; i++) {

                var culture = languages[i];

                html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
            }

            select.innerHTML = html;
        });
    }

    function populateCountries(select) {

        return ApiClient.getCountries().then(function (allCountries) {

            var html = "";

            html += "<option value=''></option>";

            for (var i = 0, length = allCountries.length; i < length; i++) {

                var culture = allCountries[i];

                html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>";
            }

            select.innerHTML = html;
        });
    }

    function embed(parent, contentType, libraryOptions) {

        return new Promise(function (resolve, reject) {

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/libraryoptionseditor/libraryoptionseditor.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                parent.innerHTML = globalize.translateDocument(template);

                var promises = [
                    populateLanguages(parent.querySelector('#selectLanguage')),
                    populateCountries(parent.querySelector('#selectCountry'))
                ];

                Promise.all(promises).then(function () {

                    setContentType(parent, contentType);

                    if (libraryOptions) {
                        setLibraryOptions(parent, libraryOptions);
                    }

                    resolve();
                });
            }

            xhr.send();
        });
    }

    function setContentType(parent, contentType) {

        if (contentType == 'homevideos' || contentType == 'photos') {
            parent.querySelector('.chkEnablePhotosContainer').classList.remove('hide');
            parent.querySelector('.chkDownloadImagesInAdvanceContainer').classList.add('hide');
            parent.querySelector('.chkEnableInternetProvidersContainer').classList.add('hide');
            parent.querySelector('.fldMetadataLanguage').classList.add('hide');
            parent.querySelector('.fldMetadataCountry').classList.add('hide');
        } else {
            parent.querySelector('.chkEnablePhotosContainer').classList.add('hide');
            parent.querySelector('.chkDownloadImagesInAdvanceContainer').classList.remove('hide');
            parent.querySelector('.chkEnableInternetProvidersContainer').classList.remove('hide');
            parent.querySelector('.fldMetadataLanguage').classList.remove('hide');
            parent.querySelector('.fldMetadataCountry').classList.remove('hide');
        }

        if (contentType == 'photos') {
            parent.querySelector('.chkSaveLocalContainer').classList.add('hide');
        } else {
            parent.querySelector('.chkSaveLocalContainer').classList.remove('hide');
        }

        if (contentType == 'tvshows' || contentType == 'movies' || contentType == 'homevideos' || contentType == 'musicvideos' || contentType == 'mixed' || !contentType) {
            parent.querySelector('.chapterSettingsSection').classList.remove('hide');
        } else {
            parent.querySelector('.chapterSettingsSection').classList.add('hide');
        }

        if (contentType == 'tvshows') {
            parent.querySelector('.chkImportMissingEpisodesContainer').classList.remove('hide');
            parent.querySelector('.chkAutomaticallyGroupSeriesContainer').classList.remove('hide');
        } else {
            parent.querySelector('.chkImportMissingEpisodesContainer').classList.add('hide');
            parent.querySelector('.chkAutomaticallyGroupSeriesContainer').classList.add('hide');
        }
    }

    function getLibraryOptions(parent) {

        var options = {
            EnableArchiveMediaFiles: false,
            EnablePhotos: parent.querySelector('.chkEnablePhotos').checked,
            EnableRealtimeMonitor: parent.querySelector('.chkEnableRealtimeMonitor').checked,
            ExtractChapterImagesDuringLibraryScan: parent.querySelector('.chkExtractChaptersDuringLibraryScan').checked,
            EnableChapterImageExtraction: parent.querySelector('.chkExtractChapterImages').checked,
            DownloadImagesInAdvance: parent.querySelector('#chkDownloadImagesInAdvance').checked,
            EnableInternetProviders: parent.querySelector('#chkEnableInternetProviders').checked,
            ImportMissingEpisodes: parent.querySelector('#chkImportMissingEpisodes').checked,
            SaveLocalMetadata: parent.querySelector('#chkSaveLocal').checked,
            EnableAutomaticSeriesGrouping: parent.querySelector('.chkAutomaticallyGroupSeries').checked,
            PreferredMetadataLanguage: parent.querySelector('#selectLanguage').value,
            MetadataCountryCode: parent.querySelector('#selectCountry').value
        };

        return options;
    }

    function setLibraryOptions(parent, options) {

        parent.querySelector('#selectLanguage').value = options.PreferredMetadataLanguage || '';
        parent.querySelector('#selectCountry').value = options.MetadataCountryCode || '';

        parent.querySelector('.chkEnablePhotos').checked = options.EnablePhotos;
        parent.querySelector('.chkEnableRealtimeMonitor').checked = options.EnableRealtimeMonitor;
        parent.querySelector('.chkExtractChaptersDuringLibraryScan').checked = options.ExtractChapterImagesDuringLibraryScan;
        parent.querySelector('.chkExtractChapterImages').checked = options.EnableChapterImageExtraction;
        parent.querySelector('#chkDownloadImagesInAdvance').checked = options.DownloadImagesInAdvance;
        parent.querySelector('#chkEnableInternetProviders').checked = options.EnableInternetProviders;
        parent.querySelector('#chkSaveLocal').checked = options.SaveLocalMetadata;
        parent.querySelector('#chkImportMissingEpisodes').checked = options.ImportMissingEpisodes;
        parent.querySelector('.chkAutomaticallyGroupSeries').checked = options.EnableAutomaticSeriesGrouping;
    }

    return {
        embed: embed,
        setContentType: setContentType,
        getLibraryOptions: getLibraryOptions,
        setLibraryOptions: setLibraryOptions
    };
});