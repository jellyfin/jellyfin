define(['appSettings', 'events'], function (appsettings, events) {
    'use strict';

    function onSaveTimeout() {

        var self = this;
        self.saveTimeout = null;
        self.currentApiClient.updateDisplayPreferences('usersettings', self.displayPrefs, self.currentUserId, 'emby');
    }

    function saveServerPreferences(instance) {
        if (instance.saveTimeout) {
            clearTimeout(instance.saveTimeout);
        }
        instance.saveTimeout = setTimeout(onSaveTimeout.bind(instance), 50);
    }

    function UserSettings() {
    }

    UserSettings.prototype.setUserInfo = function (userId, apiClient) {

        if (this.saveTimeout) {
            clearTimeout(this.saveTimeout);
        }

        this.currentUserId = userId;
        this.currentApiClient = apiClient;

        if (!userId) {
            this.displayPrefs = null;
            return Promise.resolve();
        }

        var self = this;

        return apiClient.getDisplayPreferences('usersettings', userId, 'emby').then(function (result) {
            result.CustomPrefs = result.CustomPrefs || {};
            self.displayPrefs = result;
        });
    };

    UserSettings.prototype.getData = function () {
        return this.displayPrefs;
    };

    UserSettings.prototype.importFrom = function (instance) {
        this.displayPrefs = instance.getData();
    };

    UserSettings.prototype.set = function (name, value, enableOnServer) {

        var userId = this.currentUserId;
        if (!userId) {
            throw new Error('userId cannot be null');
        }

        var currentValue = this.get(name, enableOnServer);
        var result = appsettings.set(name, value, userId);

        if (enableOnServer !== false && this.displayPrefs) {
            this.displayPrefs.CustomPrefs[name] = value == null ? value : value.toString();
            saveServerPreferences(this);
        }

        if (currentValue !== value) {
            events.trigger(this, 'change', [name]);
        }

        return result;
    };

    UserSettings.prototype.get = function (name, enableOnServer) {
        var userId = this.currentUserId;
        if (!userId) {
            // TODO: I'd like to continue to throw this exception but it causes issues with offline use
            // Revisit in the future and restore it
            return null;
            //throw new Error('userId cannot be null');
        }

        if (enableOnServer !== false) {
            if (this.displayPrefs) {
                return this.displayPrefs.CustomPrefs[name];
            }
        }

        return appsettings.get(name, userId);
    };

    UserSettings.prototype.serverConfig = function (config) {

        var apiClient = this.currentApiClient;

        if (config) {

            return apiClient.updateUserConfiguration(this.currentUserId, config);

        } else {

            return apiClient.getUser(this.currentUserId).then(function (user) {

                return user.Configuration;
            });
        }
    };

    UserSettings.prototype.enableCinemaMode = function (val) {

        if (val != null) {
            return this.set('enableCinemaMode', val.toString(), false);
        }

        val = this.get('enableCinemaMode', false);

        if (val) {
            return val !== 'false';
        }

        return true;
    };

    UserSettings.prototype.enableNextVideoInfoOverlay = function (val) {

        if (val != null) {
            return this.set('enableNextVideoInfoOverlay', val.toString());
        }

        val = this.get('enableNextVideoInfoOverlay');

        return val !== 'false';
    };

    UserSettings.prototype.enableThemeSongs = function (val) {

        if (val != null) {
            return this.set('enableThemeSongs', val.toString(), false);
        }

        val = this.get('enableThemeSongs', false);

        return val !== 'false';
    };

    UserSettings.prototype.enableThemeVideos = function (val) {

        if (val != null) {
            return this.set('enableThemeVideos', val.toString(), false);
        }

        val = this.get('enableThemeVideos', false);

        if (val) {
            return val !== 'false';
        }

        return UserSettings.defaults.enableThemeVideos;
    };

    UserSettings.prototype.enableBackdrops = function (val) {

        if (val != null) {
            return this.set('enableBackdrops', val.toString(), false);
        }

        val = this.get('enableBackdrops', false);

        if (val) {
            return val !== 'false';
        }

        return UserSettings.defaults.enableBackdrops;
    };

    UserSettings.prototype.language = function (val) {

        if (val != null) {
            return this.set('language', val.toString(), false);
        }

        return this.get('language', false);
    };

    UserSettings.prototype.dateTimeLocale = function (val) {

        if (val != null) {
            return this.set('datetimelocale', val.toString(), false);
        }

        return this.get('datetimelocale', false);
    };

    UserSettings.prototype.skipBackLength = function (val) {

        if (val != null) {
            return this.set('skipBackLength', val.toString());
        }

        return parseInt(this.get('skipBackLength') || '10000');
    };

    UserSettings.prototype.skipForwardLength = function (val) {

        if (val != null) {
            return this.set('skipForwardLength', val.toString());
        }

        return parseInt(this.get('skipForwardLength') || '30000');
    };

    UserSettings.prototype.dashboardTheme = function (val) {

        if (val != null) {
            return this.set('dashboardTheme', val);
        }

        return this.get('dashboardTheme');
    };

    UserSettings.prototype.skin = function (val) {

        if (val != null) {
            return this.set('skin', val, false);
        }

        return this.get('skin', false) || UserSettings.defaults.skin;
    };

    UserSettings.prototype.theme = function (val) {

        if (val != null) {
            return this.set('appTheme', val, false);
        }

        return this.get('appTheme', false) || UserSettings.defaults.theme;
    };

    UserSettings.prototype.enableSeasonalThemes = function (val) {

        if (val != null) {
            return this.set('enableSeasonalThemes', val, false);
        }

        return this.get('enableSeasonalThemes', false) !== 'false';
    };

    UserSettings.prototype.screensaver = function (val) {

        if (val != null) {
            return this.set('screensaver', val, false);
        }

        return this.get('screensaver', false) || UserSettings.defaults.screensaver;
    };

    UserSettings.prototype.soundEffects = function (val) {

        if (val != null) {
            return this.set('soundeffects', val, false);
        }

        return this.get('soundeffects', false) || UserSettings.defaults.soundEffects;
    };

    // apps should set these values
    UserSettings.defaults = {
        theme: null,
        enableThemeVideos: true
    };

    UserSettings.prototype.loadQuerySettings = function (key, query) {

        var values = this.get(key);

        if (values) {

            values = JSON.parse(values);

            return Object.assign(query, values);
        }

        return query;
    };

    UserSettings.prototype.saveQuerySettings = function (key, query) {

        var values = {};

        if (query.SortBy) {
            values.SortBy = query.SortBy;
        }
        if (query.SortOrder) {
            values.SortOrder = query.SortOrder;
        }

        return this.set(key, JSON.stringify(values));
    };

    UserSettings.prototype.getSubtitleAppearanceSettings = function (key) {

        key = key || 'localplayersubtitleappearance3';

        return JSON.parse(this.get(key, false) || '{}');
    };

    UserSettings.prototype.setSubtitleAppearanceSettings = function (value, key) {

        key = key || 'localplayersubtitleappearance3';

        return this.set(key, JSON.stringify(value), false);
    };

    UserSettings.prototype.setFilter = function (key, value) {

        return this.set(key, value, true);
    };

    UserSettings.prototype.getFilter = function (key) {

        return this.get(key, true);
    };

    return UserSettings;
});