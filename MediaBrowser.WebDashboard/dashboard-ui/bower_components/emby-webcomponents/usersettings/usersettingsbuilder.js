define(['appSettings', 'events'], function (appsettings, events) {

    return function () {

        var self = this;
        var currentUserId;
        var currentApiClient;
        var displayPrefs;

        self.setUserInfo = function (userId, apiClient) {

            currentUserId = userId;
            currentApiClient = apiClient;

            if (!userId) {
                displayPrefs = null;
                return Promise.resolve();
            }

            return apiClient.getDisplayPreferences('usersettings', userId, 'emby').then(function (result) {
                result.CustomPrefs = result.CustomPrefs || {};
                displayPrefs = result;
            });
        };

        function saveServerPreferences() {
            currentApiClient.updateDisplayPreferences('usersettings', displayPrefs, currentUserId, 'emby');
        }

        self.set = function (name, value, enableOnServer) {

            var userId = currentUserId;
            if (!userId) {
                throw new Error('userId cannot be null');
            }

            var currentValue = self.get(name);
            appsettings.set(name, value, userId);

            if (enableOnServer !== false && displayPrefs) {
                displayPrefs.CustomPrefs[name] = value == null ? value : value.toString();
                saveServerPreferences();
            }

            if (currentValue != value) {
                events.trigger(self, 'change', [name]);
            }
        };

        self.get = function (name, enableOnServer) {
            var userId = currentUserId;
            if (!userId) {
                throw new Error('userId cannot be null');
            }

            if (enableOnServer !== false) {
                if (displayPrefs) {
                    return displayPrefs.CustomPrefs[name];
                }
            }

            return appsettings.get(name, userId);
        };

        self.enableCinemaMode = function (val) {

            if (val != null) {
                self.set('enableCinemaMode', val.toString());
            }

            val = self.get('enableCinemaMode', false);

            if (val) {
                return val != 'false';
            }

            return true;
        };

        self.language = function (val) {

            if (val != null) {
                self.set('language', val.toString());
            }

            return self.get('language');
        };

        self.skipBackLength = function (val) {

            if (val != null) {
                self.set('skipBackLength', val.toString());
            }

            return parseInt(self.get('skipBackLength') || '15000');
        };

        self.skipForwardLength = function (val) {

            if (val != null) {
                self.set('skipForwardLength', val.toString());
            }

            return parseInt(self.get('skipForwardLength') || '15000');
        };

        self.serverConfig = function (config) {

            var apiClient = currentApiClient;

            if (config) {

                return apiClient.updateUserConfiguration(currentUserId, config);

            } else {

                return apiClient.getUser(currentUserId).then(function (user) {

                    return user.Configuration;
                });
            }
        };
    };
});