
const config = {
    setup: (view) => {
        ApiClient.getDisplayPreferences('usersettings', ApiClient.getCurrentUserId(), 'emby').then(function (userSettings) {
            document.querySelector('#modularHomeEnabled').checked = userSettings.CustomPrefs['useModularHome'] === 'true';
        });

        document.querySelector('.configForm')
            .addEventListener('submit', function (e) {
                ApiClient.getDisplayPreferences('usersettings', ApiClient.getCurrentUserId(), 'emby').then(function (userSettings) {
                    userSettings.CustomPrefs['useModularHome'] = document.querySelector('#modularHomeEnabled').checked ? 'true' : 'false';

                    ApiClient.updateDisplayPreferences('usersettings', userSettings, ApiClient.getCurrentUserId(), 'emby');
                });

                e.preventDefault();
                return false;
            });
    }
}


export default function (view) {
    config.setup(view);
}
