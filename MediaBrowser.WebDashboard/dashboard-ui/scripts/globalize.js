(function () {

    var dictionaries = {};

    function getUrl(name, culture) {

        var parts = culture.split('-');
        if (parts.length == 2) {
            parts[1] = parts[1].toUpperCase();
            culture = parts.join('-');
        }

        return 'strings/' + name + '/' + culture + '.json';
    }
    function getDictionary(name, culture) {

        return dictionaries[getUrl(name, culture)];
    }

    function loadDictionary(name, culture) {

        return new Promise(function (resolve, reject) {

            if (getDictionary(name, culture)) {
                console.log('Globalize loadDictionary resolved: ' + name);
                resolve();
                return;
            }

            var url = getUrl(name, culture);

            var requestUrl = url + "?v=" + AppInfo.appVersion;

            console.log('Requesting ' + requestUrl);

            var xhr = new XMLHttpRequest();
            xhr.open('GET', requestUrl, true);

            var onError = function () {

                console.log('Dictionary not found. Reverting to english');

                // Grab the english version
                var xhr2 = new XMLHttpRequest();
                xhr2.open('GET', getUrl(name, 'en-US'), true);

                xhr2.onload = function (e) {
                    dictionaries[url] = JSON.parse(this.response);
                    console.log('Globalize loadDictionary resolved: ' + name);
                    resolve();
                };

                xhr2.send();
            };

            xhr.onload = function (e) {

                console.log('Globalize response status: ' + this.status);

                if (this.status < 400) {

                    dictionaries[url] = JSON.parse(this.response);
                    console.log('Globalize loadDictionary resolved: ' + name);
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

        return Promise.all([loadDictionary('html', value), loadDictionary('javascript', value)]);
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

            if (AppInfo.isNativeApp) {

                resolve(navigator.language || navigator.userLanguage);

            } else if (AppInfo.supportsUserDisplayLanguageSetting) {

                console.log('AppInfo.supportsUserDisplayLanguageSetting is true');

                resolve(AppSettings.displayLanguage());

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

    function translateDocument(html, dictionaryName) {

        dictionaryName = dictionaryName || 'html';

        var glossary = getDictionary(dictionaryName, currentCulture) || {};
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

            var glossary = getDictionary('javascript', currentCulture) || {};
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