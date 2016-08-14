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

        if (contentType == 'music' || contentType == 'tvshows' || contentType == 'movies' || contentType == 'homevideos' || contentType == 'musicvideos' || contentType == 'mixed') {
            parent.querySelector('.chkArhiveAsMediaContainer').classList.remove('hide');
        } else {
            parent.querySelector('.chkArhiveAsMediaContainer').classList.add('hide');
        }

        if (contentType == 'music' || contentType == 'tvshows' || contentType == 'movies' || contentType == 'homevideos' || contentType == 'musicvideos' || contentType == 'mixed') {
            parent.classList.remove('hide');
        } else {
            parent.classList.add('hide');
        }
    }

    function getLibraryOptions(parent) {

        var options = {
            EnableArchiveMediaFiles: parent.querySelector('.chkArhiveAsMedia').checked
        };

        return options;
    }

    function setLibraryOptions(parent, options) {

        parent.querySelector('.chkArhiveAsMedia').checked = options.EnableArchiveMediaFiles;;
    }

    return {
        embed: embed,
        setContentType: setContentType,
        getLibraryOptions: getLibraryOptions,
        setLibraryOptions: setLibraryOptions
    };
});