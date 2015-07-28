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

    function loadDictionary(name, culture, loadUrl, saveUrl) {

        var deferred = DeferredBuilder.Deferred();

        if (getDictionary(name, culture)) {
            deferred.resolve();
        } else {

            $.getJSON(loadUrl).done(function (dictionary) {
                dictionaries[saveUrl] = dictionary;
                deferred.resolve();
            });
        }

        return deferred.promise();
    }

    var currentCulture = 'en-US';
    function setCulture(value) {

        currentCulture = value;

        var htmlValue = value;
        var jsValue = value;

        var htmlUrl = getUrl('html', htmlValue);
        var jsUrl = getUrl('javascript', jsValue);

        var htmlLoadUrl = getUrl('html', htmlValue);
        var jsLoadUrl = getUrl('javascript', jsValue);

        //htmlLoadUrl = getUrl('html', 'server');
        //jsLoadUrl = getUrl('javascript', 'javascript');

        return $.when(loadDictionary('html', htmlValue, htmlLoadUrl, htmlUrl), loadDictionary('javascript', jsValue, jsLoadUrl, jsUrl));
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