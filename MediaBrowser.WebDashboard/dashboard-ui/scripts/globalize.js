(function () {

    var dictionaries = {};

    function getUrl(name, culture) {

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
            });
        }

        return deferred.promise();
    }

    var currentCulture = 'en-US';
    function setCulture(value) {

        var promises = [];

        currentCulture = value;

        promises.push(loadDictionary('html', value));
        promises.push(loadDictionary('javascript', value));

        return $.when(promises);
    }

    function ensure() {

        var culture = document.documentElement.getAttribute('data-culture');

        if (!culture) {
            culture = 'en-US';
        }

        return setCulture(culture);
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