define(['appSettings', 'pluginManager'], function (appSettings, pluginManager) {
    'use strict';

    var settingsKey = 'installedpackages1';

    function addPackage(packageManager, pkg) {

        packageManager.packagesList = packageManager.packagesList.filter(function (p) {

            return p.name !== pkg.name;
        });

        packageManager.packagesList.push(pkg);
    }

    function removeUrl(url) {

        var manifestUrls = JSON.parse(appSettings.get(settingsKey) || '[]');

        manifestUrls = manifestUrls.filter(function (i) {
            return i !== url;
        });

        appSettings.set(settingsKey, JSON.stringify(manifestUrls));
    }

    function loadPackage(packageManager, url, throwError) {

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

                    addPackage(packageManager, pkg);

                    var plugins = pkg.plugins || [];
                    if (pkg.plugin) {
                        plugins.push(pkg.plugin);
                    }
                    var promises = plugins.map(function (pluginUrl) {
                        return pluginManager.loadPlugin(packageManager.mapPath(pkg, pluginUrl));
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

    function PackageManager() {

        this.packagesList = [];
    }

    PackageManager.prototype.init = function () {
        var manifestUrls = JSON.parse(appSettings.get(settingsKey) || '[]');

        var instance = this;
        return Promise.all(manifestUrls.map(function (u) {

            return loadPackage(instance, u);

        })).then(function () {
            return Promise.resolve();
        }, function () {
            return Promise.resolve();
        });
    };

    PackageManager.prototype.packages = function () {
        return this.packagesList.slice(0);
    };

    PackageManager.prototype.install = function (url) {

        return loadPackage(this, url, true).then(function (pkg) {

            var manifestUrls = JSON.parse(appSettings.get(settingsKey) || '[]');

            if (manifestUrls.indexOf(url) === -1) {
                manifestUrls.push(url);
                appSettings.set(settingsKey, JSON.stringify(manifestUrls));
            }

            return pkg;
        });
    };

    PackageManager.prototype.uninstall = function (name) {

        var pkg = this.packagesList.filter(function (p) {

            return p.name === name;
        })[0];

        if (pkg) {

            this.packagesList = this.packagesList.filter(function (p) {

                return p.name !== name;
            });

            removeUrl(pkg.url);
        }

        return Promise.resolve();
    };

    PackageManager.prototype.mapPath = function (pkg, pluginUrl) {

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

    return new PackageManager();
});