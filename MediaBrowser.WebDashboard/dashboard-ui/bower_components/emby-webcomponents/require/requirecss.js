define(function () {
    'use strict';

    var requireCss = {};

    requireCss.normalize = function (name, normalize) {
        if (name.substr(name.length - 4, 4) === '.css') {
            name = name.substr(0, name.length - 4);
        }

        return normalize(name);
    };

    var importedCss = [];

    function isLoaded(url) {
        return importedCss.indexOf(url) !== -1;
    }

    function removeFromLoadHistory(url) {

        url = url.toLowerCase();

        importedCss = importedCss.filter(function (c) {
            return url.indexOf(c.toLowerCase()) === -1;
        });
    }

    requireCss.load = function (cssId, req, load, config) {

        // Somehow if the url starts with /css, require will get all screwed up since this extension is also called css
        var srch = '/emby-webcomponents/require/requirecss';
        var index = cssId.indexOf(srch);

        if (index !== -1) {
            cssId = 'css' + cssId.substring(index + srch.length);
        }

        var url = cssId + '.css';

        if (url.indexOf('://') === -1) {
            url = config.baseUrl + url;
        }

        if (!isLoaded(url)) {
            importedCss.push(url);

            var link = document.createElement('link');

            link.setAttribute('rel', 'stylesheet');
            link.setAttribute('type', 'text/css');
            link.onload = load;

            var linkUrl = url;

            if (config.urlArgs) {
                linkUrl += config.urlArgs(cssId, url);
            }
            link.setAttribute('href', linkUrl);
            document.head.appendChild(link);
        } else {
            load();
        }
    };

    window.requireCss = {
        removeStylesheet: function (stylesheet) {

            stylesheet.parentNode.removeChild(stylesheet);
            removeFromLoadHistory(stylesheet.href);
        }
    };

    return requireCss;
});
