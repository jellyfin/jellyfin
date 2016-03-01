(function () {

    var dictionaries = {};

    function getUrl(culture) {

        var parts = culture.split('-');
        if (parts.length == 2) {
            parts[1] = parts[1].toUpperCase();
            culture = parts.join('-');
        }

        return 'strings/' + culture + '.json';
    }
    function getDictionary(culture) {

        return dictionaries[getUrl(culture)];
    }

    function loadDictionary(culture) {

        return new Promise(function (resolve, reject) {

            if (getDictionary(culture)) {
                console.log('Globalize loadDictionary resolved.');
                resolve();
                return;
            }

            var url = getUrl(culture);

            var requestUrl = url + "?v=" + AppInfo.appVersion;

            console.log('Requesting ' + requestUrl);

            var xhr = new XMLHttpRequest();
            xhr.open('GET', requestUrl, true);

            var onError = function () {

                console.log('Dictionary not found. Reverting to english');

                // Grab the english version
                var xhr2 = new XMLHttpRequest();
                xhr2.open('GET', getUrl('en-US'), true);

                xhr2.onload = function (e) {
                    dictionaries[url] = JSON.parse(this.response);
                    console.log('Globalize loadDictionary resolved.');
                    resolve();
                };

                xhr2.send();
            };

            xhr.onload = function (e) {

                console.log('Globalize response status: ' + this.status);

                if (this.status < 400) {

                    dictionaries[url] = JSON.parse(this.response);
                    console.log('Globalize loadDictionary resolved.');
                    resolve();

                } else {
                    onError();
                }
            };

            xhr.onerror = onError;

            xhr.send();
        });
    }

    var currentCulture = 'en-US';
    function setCulture(value) {

        console.log('Setting culture to ' + value);
        currentCulture = value;

        return loadDictionary(value);
    }

    function normalizeLocaleName(culture) {

        culture = culture.replace('_', '-');

        // If it's de-DE, convert to just de
        var parts = culture.split('-');
        if (parts.length == 2) {
            if (parts[0].toLowerCase() == parts[1].toLowerCase()) {
                culture = parts[0].toLowerCase();
            }
        }

        return culture;
    }

    function getDeviceCulture() {

        return new Promise(function (resolve, reject) {

            if (Dashboard.isConnectMode()) {

                resolve(navigator.language || navigator.userLanguage);

            } else {

                console.log('Getting culture from document');
                resolve(document.documentElement.getAttribute('data-culture'));
            }
        });
    }


    function ensure() {

        console.log('Entering Globalize.ensure');

        return getDeviceCulture().then(function (culture) {

            culture = normalizeLocaleName(culture || 'en-US');

            return setCulture(culture);
        });
    }

    function translateDocument(html) {

        var glossary = getDictionary(currentCulture) || {};
        return translateHtml(html, glossary);
    }

    function translateHtml(html, dictionary) {

        var startIndex = html.indexOf('${');

        if (startIndex == -1) {
            return html;
        }

        startIndex += 2;
        var endIndex = html.indexOf('}', startIndex);

        if (endIndex == -1) {
            return html;
        }

        var key = html.substring(startIndex, endIndex);
        var val = dictionary[key] || key;

        html = html.replace('${' + key + '}', val);

        return translateHtml(html, dictionary);
    }

    // Mimic Globalize api
    // https://github.com/jquery/globalize
    // Maybe later switch to it

    window.Globalize = {
        translate: function (key) {

            var glossary = getDictionary(currentCulture) || {};
            var val = glossary[key] || key;

            for (var i = 1; i < arguments.length; i++) {

                val = val.replace('{' + (i - 1) + '}', arguments[i]);

            }

            return val;
        },
        ensure: ensure,
        translateDocument: translateDocument
    };

})();