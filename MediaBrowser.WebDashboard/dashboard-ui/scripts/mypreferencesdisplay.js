define(['userSettingsBuilder', 'appStorage'], function (userSettingsBuilder, appStorage) {
    'use strict';

    return function (view, params) {

        var userId = params.userId || Dashboard.getCurrentUserId();
        var userSettingsInstance = new userSettingsBuilder();
        var userSettingsLoaded;

        function loadForm(page, user) {

            userSettingsInstance.setUserInfo(userId, ApiClient).then(function () {
                userSettingsLoaded = true;
                page.querySelector('.chkDisplayMissingEpisodes').checked = user.Configuration.DisplayMissingEpisodes || false;
                page.querySelector('.chkDisplayUnairedEpisodes').checked = user.Configuration.DisplayUnairedEpisodes || false;

                page.querySelector('#chkThemeSong').checked = userSettingsInstance.enableThemeSongs();
                page.querySelector('#selectBackdrop').value = appStorage.getItem('enableBackdrops-' + user.Id) || '0';

                page.querySelector('#selectLanguage').value = userSettingsInstance.language() || '';

                Dashboard.hideLoadingMsg();
            });
        }

        function refreshGlobalUserSettings() {
            require(['userSettings'], function (userSettings) {
                userSettings.importFrom(userSettingsInstance);
            });
        }

        function saveUser(page, user) {

            user.Configuration.DisplayMissingEpisodes = page.querySelector('.chkDisplayMissingEpisodes').checked;
            user.Configuration.DisplayUnairedEpisodes = page.querySelector('.chkDisplayUnairedEpisodes').checked;

            if (userSettingsLoaded) {

                if (AppInfo.supportsUserDisplayLanguageSetting) {
                    userSettingsInstance.language(page.querySelector('#selectLanguage').value);
                }

                userSettingsInstance.enableThemeSongs(page.querySelector('#chkThemeSong').checked);

                if (userId === Dashboard.getCurrentUserId()) {
                    refreshGlobalUserSettings();
                }
            }

            appStorage.setItem('enableBackdrops-' + user.Id, page.querySelector('#selectBackdrop').value);

            return ApiClient.updateUserConfiguration(user.Id, user.Configuration);
        }

        function save(page) {

            if (!AppInfo.enableAutoSave) {
                Dashboard.showLoadingMsg();
            }

            ApiClient.getUser(userId).then(function (user) {

                saveUser(page, user).then(function () {

                    Dashboard.hideLoadingMsg();
                    if (!AppInfo.enableAutoSave) {
                        require(['toast'], function (toast) {
                            toast(Globalize.translate('SettingsSaved'));
                        });
                    }

                }, function () {
                    Dashboard.hideLoadingMsg();
                });

            });
        }

        view.querySelector('.displayPreferencesForm').addEventListener('submit', function (e) {
            save(view);
            e.preventDefault();
            // Disable default form submission
            return false;
        });

        if (AppInfo.enableAutoSave) {
            view.querySelector('.btnSave').classList.add('hide');
        } else {
            view.querySelector('.btnSave').classList.remove('hide');
        }

        view.addEventListener('viewshow', function () {
            var page = this;

            Dashboard.showLoadingMsg();

            ApiClient.getUser(userId).then(function (user) {

                loadForm(page, user);

                var requiresUserPreferences = view.querySelectorAll('.requiresUserPreferences');
                for (var i = 0, length = requiresUserPreferences.length; i < length; i++) {
                    if (user.Policy.EnableUserPreferenceAccess) {
                        requiresUserPreferences[i].classList.remove('hide');
                    } else {
                        requiresUserPreferences[i].classList.add('hide');
                    }
                }
            });

            if (AppInfo.supportsUserDisplayLanguageSetting) {
                page.querySelector('.languageSection').classList.remove('hide');
            } else {
                page.querySelector('.languageSection').classList.add('hide');
            }
        });

        view.addEventListener('viewbeforehide', function () {
            var page = this;

            if (AppInfo.enableAutoSave) {
                save(page);
            }
        });
    };

});