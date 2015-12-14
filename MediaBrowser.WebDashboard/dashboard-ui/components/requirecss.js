define(function () {
    var requireCss = {};

    requireCss.normalize = function (name, normalize) {
        if (name.substr(name.length - 4, 4) == '.css')
            name = name.substr(0, name.length - 4);

        return normalize(name);
    }

    var importedCss = [];

    function isLoaded(url) {
        return importedCss.indexOf(url) != -1;
    }

    function removeFromLoadHistory(url) {

        url = url.toLowerCase();

        importedCss = importedCss.filter(function (c) {
            return url.indexOf(c.toLowerCase()) == -1;
        });
    }

    requireCss.load = function (cssId, req, load, config) {

        // Somehow if the url starts with /css, require will get all screwed up since this extension is also called css
        cssId = cssId.replace('components/requirecss', 'css');
        var url = cssId + '.css';

        var packageName = '';

        // TODO: handle any value before the #
        if (url.indexOf('theme#') != -1) {
            url = url.replace('theme#', '');
            packageName = 'theme';
        }

        if (url.indexOf('http') != 0 && url.indexOf('file:') != 0) {
            url = config.baseUrl + url;
        }

        if (!isLoaded(url)) {
            importedCss.push(url);

            var link = document.createElement('link');

            if (packageName) {
                link.setAttribute('data-package', packageName);
            }

            link.setAttribute('rel', 'stylesheet');
            link.setAttribute('type', 'text/css');
            link.onload = load;
            link.setAttribute('href', url + "?" + config.urlArgs);
            document.head.appendChild(link);
        } else {
            load();
        }
    }

    window.requireCss = {
        unloadPackage: function (packageName) {

            // Todo: unload css here
            var stylesheets = document.head.querySelectorAll("link[data-package='" + packageName + "']");
            for (var i = 0, length = stylesheets.length; i < length; i++) {

                var stylesheet = stylesheets[i];

                Logger.log('Unloading stylesheet: ' + stylesheet.href);
                stylesheet.parentNode.removeChild(stylesheet);
                removeFromLoadHistory(stylesheet.href);
            }
        }
    };

    return requireCss;
});
