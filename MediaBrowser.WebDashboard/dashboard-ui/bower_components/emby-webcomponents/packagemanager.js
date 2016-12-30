define(['appSettings', 'pluginManager'], function (appSettings, pluginManager) {
    'use strict';

    function packageManager() {

        var self = this;
        var settingsKey = 'installedpackages1';

        var packages = [];

        self.packages = function () {
            return packages.slice(0);
        };

        function addPackage(pkg) {

            packages = packages.filter(function (p) {

                return p.name !== pkg.name;
            });

            packages.push(pkg);
        }

        self.install = function (url) {

            return loadPackage(url, true).then(function (pkg) {

                var manifestUrls = JSON.parse(appSettings.get(settingsKey) || '[]');

                if (manifestUrls.indexOf(url) === -1) {
                    manifestUrls.push(url);
                    appSettings.set(settingsKey, JSON.stringify(manifestUrls));
                }

                return pkg;
            });
        };

        self.uninstall = function (name) {

            var pkg = packages.filter(function (p) {

                return p.name === name;
            })[0];

            if (pkg) {

                packages = packages.filter(function (p) {

                    return p.name !== name;
                });

                removeUrl(pkg.url);
            }

            return Promise.resolve();
        };

        function removeUrl(url) {

            var manifestUrls = JSON.parse(appSettings.get(settingsKey) || '[]');

            manifestUrls = manifestUrls.filter(function (i) {
                return i !== url;
            });

            appSettings.set(settingsKey, JSON.stringify(manifestUrls));
        }

        self.init = function () {
            var manifestUrls = JSON.parse(appSettings.get(settingsKey) || '[]');

            return Promise.all(manifestUrls.map(loadPackage)).then(function () {
                return Promise.resolve();
            }, function () {
                return Promise.resolve();
            });
        };

        function loadPackage(url, throwError) {

            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                var originalUrl = url;
                url += url.indexOf('?') === -1 ? '?' : '&';
                url += 't=' + new Date().getTime();

                xhr.open('GET', url, true);

                var onError = function () {

                    if (throwError === true) {
                        reject();
                    } else {
                        removeUrl(originalUrl);
                        resolve();
                    }
                };

                xhr.onload = function (e) {
                    if (this.status < 400) {

                        var pkg = JSON.parse(this.response);
                        pkg.url = originalUrl;

                        addPackage(pkg);

                        var plugins = pkg.plugins || [];
                        if (pkg.plugin) {
                            plugins.push(pkg.plugin);
                        }
                        var promises = plugins.map(function (pluginUrl) {
                            return pluginManager.loadPlugin(self.mapPath(pkg, pluginUrl));
                        });
                        Promise.all(promises).then(resolve, resolve);

                    } else {
                        onError();
                    }
                };

                xhr.onerror = onError;

                xhr.send();
            });
        }

        self.mapPath = function (pkg, pluginUrl) {

            var urlLower = pluginUrl.toLowerCase();
            if (urlLower.indexOf('http:') === 0 || urlLower.indexOf('https:') === 0 || urlLower.indexOf('file:') === 0) {
                return pluginUrl;
            }

            var packageUrl = pkg.url;
            packageUrl = packageUrl.substring(0, packageUrl.lastIndexOf('/'));

            packageUrl += '/';
            packageUrl += pluginUrl;

            return packageUrl;
        };
    }

    return new packageManager();
});