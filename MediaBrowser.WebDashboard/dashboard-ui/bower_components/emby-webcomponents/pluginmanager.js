define(['events'], function (events) {
    'use strict';

    // TODO: replace with each plugin version
    var cacheParam = new Date().getTime();

    function loadStrings(plugin, globalize) {
        var strings = plugin.getTranslations ? plugin.getTranslations() : [];
        return globalize.loadStrings({
            name: plugin.id || plugin.packageName,
            strings: strings
        });
    }

    function definePluginRoute(pluginManager, route, plugin) {

        route.contentPath = pluginManager.mapPath(plugin, route.path);
        route.path = pluginManager.mapRoute(plugin, route);

        Emby.App.defineRoute(route, plugin.id);
    }

    function PluginManager() {

        this.pluginsList = [];
    }

    PluginManager.prototype.loadPlugin = function (url) {

        console.log('Loading plugin: ' + url);
        var instance = this;

        return new Promise(function (resolve, reject) {

            require([url, 'globalize', 'appRouter'], function (pluginFactory, globalize, appRouter) {

                var plugin = new pluginFactory();

                // See if it's already installed
                var existing = instance.pluginsList.filter(function (p) {
                    return p.id === plugin.id;
                })[0];

                if (existing) {
                    resolve(url);
                    return;
                }

                plugin.installUrl = url;

                var urlLower = url.toLowerCase();
                if (urlLower.indexOf('http:') === -1 && urlLower.indexOf('https:') === -1 && urlLower.indexOf('file:') === -1) {
                    if (url.indexOf(appRouter.baseUrl()) !== 0) {

                        url = appRouter.baseUrl() + '/' + url;
                    }
                }

                var separatorIndex = Math.max(url.lastIndexOf('/'), url.lastIndexOf('\\'));
                plugin.baseUrl = url.substring(0, separatorIndex);

                var paths = {};
                paths[plugin.id] = plugin.baseUrl;

                requirejs.config({
                    waitSeconds: 0,
                    paths: paths
                });

                instance.register(plugin);

                if (plugin.getRoutes) {
                    plugin.getRoutes().forEach(function (route) {
                        definePluginRoute(instance, route, plugin);
                    });
                }

                if (plugin.type === 'skin') {

                    // translations won't be loaded for skins until needed
                    resolve(plugin);
                } else {

                    loadStrings(plugin, globalize).then(function () {
                        resolve(plugin);
                    }, reject);
                }
            });
        });
    };

    // In lieu of automatic discovery, plugins will register dynamic objects
    // Each object will have the following properties:
    // name
    // type (skin, screensaver, etc)
    PluginManager.prototype.register = function (obj) {

        this.pluginsList.push(obj);
        events.trigger(this, 'registered', [obj]);
    };

    PluginManager.prototype.ofType = function (type) {

        return this.pluginsList.filter(function (o) {
            return o.type === type;
        });
    };

    PluginManager.prototype.plugins = function () {
        return this.pluginsList;
    };

    PluginManager.prototype.mapRoute = function (plugin, route) {

        if (typeof plugin === 'string') {
            plugin = this.pluginsList.filter(function (p) {
                return (p.id || p.packageName) === plugin;
            })[0];
        }

        route = route.path || route;

        if (route.toLowerCase().indexOf('http') === 0) {
            return route;
        }

        return '/plugins/' + plugin.id + '/' + route;
    };

    PluginManager.prototype.mapPath = function (plugin, path, addCacheParam) {

        if (typeof plugin === 'string') {
            plugin = this.pluginsList.filter(function (p) {
                return (p.id || p.packageName) === plugin;
            })[0];
        }

        var url = plugin.baseUrl + '/' + path;

        if (addCacheParam) {
            url += url.indexOf('?') === -1 ? '?' : '&';
            url += 'v=' + cacheParam;
        }

        return url;
    };

    return new PluginManager();
});