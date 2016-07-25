define(['appSettings', 'apiClientResolver', 'events'], function (appsettings, apiClientResolver, events) {

    return function (configuredUserId) {

        var self = this;

        function getUserId(apiClient) {

            if (configuredUserId) {
                return configuredUserId;
            }

            var apiClientInstance = apiClient || apiClientResolver();

            if (apiClientInstance) {
                return apiClientInstance.getCurrentUserId();
            }

            return null;
        }

        self.set = function (name, value) {

            var userId = getUserId();
            if (!userId) {
                throw new Error('userId cannot be null');
            }

            var currentValue = self.get(name);
            appsettings.set(name, value, userId);

            if (currentValue != value) {
                events.trigger(self, 'change', [name]);
            }
        };

        self.get = function (name) {
            var userId = getUserId();
            if (!userId) {
                throw new Error('userId cannot be null');
            }
            return appsettings.get(name, userId);
        };

        self.enableCinemaMode = function (val) {

            if (val != null) {
                self.set('enableCinemaMode', val.toString());
            }

            val = self.get('enableCinemaMode');

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

            var apiClient = apiClientResolver();

            if (config) {

                return apiClient.updateUserConfiguration(getUserId(apiClient), config);

            } else {

                return apiClient.getUser(getUserId(apiClient)).then(function (user) {

                    return user.Configuration;
                });
            }
        };
    };
});