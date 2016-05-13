define(function () {

    var importedFiles = [];

    return {

        load: function (url, req, load, config) {

            if (url.indexOf('http') != 0 && url.indexOf('file:') != 0) {
                url = config.baseUrl + url;
            }

            url = url + "?" + config.urlArgs;

            var xhr = new XMLHttpRequest();
            xhr.open('GET', url, true);

            xhr.onload = function (e) {
                load(this.response);
            };

            xhr.send();
        },

        normalize: function (name, normalize) {
            return normalize(name);
        }
    };
});
