define(function () {

    var importedFiles = [];

    return {

        load: function (cssId, req, load, config) {

            // Somehow if the url starts with /html, require will get all screwed up since this extension is also called html
            cssId = cssId.replace('js/requirehtml', 'html');

            var url = cssId + '.html';

            if (url.indexOf('://') == -1) {
                url = config.baseUrl + url;
            }

            if (importedFiles.indexOf(url) == -1) {
                importedFiles.push(url);

                var link = document.createElement('link');
                link.rel = 'import';

                if (config.urlArgs) {
                    if (url.toLowerCase().indexOf('bower_') == -1 || url.toLowerCase().indexOf('emby-webcomponents') != -1) {
                        url = url + config.urlArgs(cssId, url);
                    }
                }

                link.onload = load;
                link.href = url;

                document.head.appendChild(link);

                return;
            }

            load();
        },

        normalize: function (name, normalize) {
            if (name.substr(name.length - 5, 5) == '.html')
                name = name.substr(0, name.length - 5);

            return normalize(name);
        }
    };
});
