define(function () {

    return {

        load: function (url, req, load, config) {

            if (url.indexOf('://') == -1) {
                url = config.baseUrl + url;
            }

            if (config.urlArgs) {
                url += config.urlArgs(url, url);
            }

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
