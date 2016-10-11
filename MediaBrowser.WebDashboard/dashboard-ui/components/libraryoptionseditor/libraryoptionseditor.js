define(['globalize', 'emby-checkbox'], function (globalize) {

    function embed(parent, contentType, libraryOptions) {

        return new Promise(function (resolve, reject) {

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/libraryoptionseditor/libraryoptionseditor.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                parent.innerHTML = globalize.translateDocument(template);

                setContentType(parent, contentType);

                if (libraryOptions) {
                    setLibraryOptions(parent, libraryOptions);
                }

                resolve();
            }

            xhr.send();
        });
    }

    function setContentType(parent, contentType) {

        if (contentType == 'music' || contentType == 'tvshows' || contentType == 'movies' || contentType == 'homevideos' || contentType == 'musicvideos' || contentType == 'mixed' || !contentType) {
            parent.querySelector('.chkArhiveAsMediaContainer').classList.add('hide');
        } else {
            parent.querySelector('.chkArhiveAsMediaContainer').classList.add('hide');
        }

        if (contentType == 'homevideos' || contentType == 'photos') {
            parent.querySelector('.chkEnablePhotosContainer').classList.remove('hide');
            parent.querySelector('.chkDownloadImagesInAdvanceContainer').classList.add('hide');
            parent.querySelector('.chkEnableInternetProvidersContainer').classList.add('hide');
        } else {
            parent.querySelector('.chkEnablePhotosContainer').classList.add('hide');
            parent.querySelector('.chkDownloadImagesInAdvanceContainer').classList.remove('hide');
            parent.querySelector('.chkEnableInternetProvidersContainer').classList.remove('hide');
        }

        if (contentType == 'photos') {
            parent.querySelector('.chkSaveLocalContainer').classList.add('hide');
        } else {
            parent.querySelector('.chkSaveLocalContainer').classList.remove('hide');
        }

        if (contentType == 'tvshows' || contentType == 'movies' || contentType == 'homevideos' || contentType == 'musicvideos' || contentType == 'mixed' || !contentType) {
            parent.querySelector('.fldExtractChaptersDuringLibraryScan').classList.remove('hide');
            parent.querySelector('.fldExtractChapterImages').classList.remove('hide');
        } else {
            parent.querySelector('.fldExtractChaptersDuringLibraryScan').classList.add('hide');
            parent.querySelector('.fldExtractChapterImages').classList.add('hide');
        }
    }

    function getLibraryOptions(parent) {

        var options = {
            EnableArchiveMediaFiles: parent.querySelector('.chkArhiveAsMedia').checked,
            EnablePhotos: parent.querySelector('.chkEnablePhotos').checked,
            EnableRealtimeMonitor: parent.querySelector('.chkEnableRealtimeMonitor').checked,
            ExtractChapterImagesDuringLibraryScan: parent.querySelector('.chkExtractChaptersDuringLibraryScan').checked,
            EnableChapterImageExtraction: parent.querySelector('.chkExtractChapterImages').checked,
            DownloadImagesInAdvance: parent.querySelector('#chkDownloadImagesInAdvance').checked,
            EnableInternetProviders: parent.querySelector('#chkEnableInternetProviders').checked,
            SaveLocalMetadata: parent.querySelector('#chkSaveLocal').checked
        };

        return options;
    }

    function setLibraryOptions(parent, options) {

        parent.querySelector('.chkArhiveAsMedia').checked = options.EnableArchiveMediaFiles;
        parent.querySelector('.chkEnablePhotos').checked = options.EnablePhotos;
        parent.querySelector('.chkEnableRealtimeMonitor').checked = options.EnableRealtimeMonitor;
        parent.querySelector('.chkExtractChaptersDuringLibraryScan').checked = options.ExtractChapterImagesDuringLibraryScan;
        parent.querySelector('.chkExtractChapterImages').checked = options.EnableChapterImageExtraction;
        parent.querySelector('#chkDownloadImagesInAdvance').checked = options.DownloadImagesInAdvance;
        parent.querySelector('#chkEnableInternetProviders').checked = options.EnableInternetProviders;
        parent.querySelector('#chkSaveLocal').checked = options.SaveLocalMetadata;
    }

    return {
        embed: embed,
        setContentType: setContentType,
        getLibraryOptions: getLibraryOptions,
        setLibraryOptions: setLibraryOptions
    };
});