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

        var deferred = DeferredBuilder.Deferred();

        if (getDictionary(name, culture)) {
            deferred.resolve();
        } else {

            var url = getUrl(name, culture);

            $.getJSON(url).done(function (dictionary) {

                dictionaries[url] = dictionary;
                deferred.resolve();

            }).fail(function () {

                // If there's no dictionary for that language, grab English
                $.getJSON(getUrl(name, 'en-US')).done(function (dictionary) {

                    dictionaries[url] = dictionary;
                    deferred.resolve();

                });
            });
        }

        return deferred.promise();
    }

    var currentCulture = 'en-US';
    function setCulture(value) {

        currentCulture = value;

        return $.when(loadDictionary('html', value), loadDictionary('javascript', value));
    }

    function getDeviceCulture() {
        var deferred = DeferredBuilder.Deferred();

        var culture;

        if (navigator.globalization && navigator.globalization.getLocaleName) {

            navigator.globalization.getLocaleName(function (locale) {

                culture = (locale.value || '').replace('_', '-');
                Logger.log('Device culture is ' + culture);
                deferred.resolveWith(null, [culture]);

            }, function () {

                Logger.log('navigator.globalization.getLocaleName failed');

                deferred.resolveWith(null, [null]);
            });

        } else {

            Logger.log('navigator.globalization.getLocaleName is unavailable');

            culture = document.documentElement.getAttribute('data-culture');
            deferred.resolveWith(null, [culture]);
        }

        return deferred.promise();
    }


    function ensure() {

        var deferred = DeferredBuilder.Deferred();

        getDeviceCulture().done(function (culture) {

            if (!culture) {
                culture = 'en-US';
            }

            setCulture(culture).done(function () {
                deferred.resolve();
            });
        });

        return deferred.promise();
    }

    function translateDocument(html, dictionaryName) {

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