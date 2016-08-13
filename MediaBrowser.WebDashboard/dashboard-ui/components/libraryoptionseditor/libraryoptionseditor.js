define(['globalize', 'emby-checkbox'], function (globalize) {

    function embed(parent, contentType) {

        return new Promise(function (resolve, reject) {

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/libraryoptionseditor/libraryoptionseditor.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                parent.innerHTML = globalize.translateDocument(template);

                setContentType(parent, contentType);

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
            EnableVideoArchiveFiles: parent.querySelector('.chkArhiveAsMedia').checked
        };

        options.EnableAudioArchiveFiles = options.EnableVideoArchiveFiles;

        return options;
    }

    return {
        embed: embed,
        setContentType: setContentType,
        getLibraryOptions: getLibraryOptions
    };
});