
const config = {
    setup: (view) => {
        ApiClient.getDisplayPreferences('usersettings', ApiClient.getCurrentUserId(), 'emby').then(function (userSettings) {
            document.querySelector('#modularHomeEnabled').checked = userSettings.CustomPrefs['useModularHome'] === 'true';
        });

        ApiClient.fetch({
            url: '/ModularHomeViews/UserSettings?userId=' + ApiClient.getCurrentUserId(),
            type: 'GET',
            dataType: 'json',
            headers: {
                accept: 'application/json'
            }
        }).then(function (settings) {
            ApiClient.fetch({
                url: '/ModularHomeViews/Sections',
                type: 'GET',
                dataType: 'json',
                headers: {
                    accept: 'application/json'
                }
            }).then(function (response) {
                if (response.TotalRecordCount > 0) {

                    let html = '';
                    for (let i = 0; i < response.TotalRecordCount; ++i) {
                        let section = response.Items[i];

                        let checked = false;
                        if (settings.EnabledSections.includes(section.Section)) {
                            checked = true;
                        }

                        html += '<label class="checkboxContainer">';
                        html += '<input is="emby-checkbox" type="checkbox" class="sectionEnabledCheckbox" data-section="' + section.Section + '" ' + (checked ? 'checked' : '') + ' />';
                        html += '<span>' + section.DisplayText + '</span>';
                        html += '</label>';
                    }

                    let elem = document.querySelector('#enabledSections');
                    elem.innerHTML = html;
                }
            });
        });

        document.querySelector('.configForm')
            .addEventListener('submit', function (e) {
                ApiClient.getDisplayPreferences('usersettings', ApiClient.getCurrentUserId(), 'emby').then(function (userSettings) {
                    userSettings.CustomPrefs['useModularHome'] = document.querySelector('#modularHomeEnabled').checked ? 'true' : 'false';

                    ApiClient.updateDisplayPreferences('usersettings', userSettings, ApiClient.getCurrentUserId(), 'emby');
                });

                let data = {
                    UserId: ApiClient.getCurrentUserId(),
                    EnabledSections: []
                };

                let checkboxes = document.querySelectorAll('.sectionEnabledCheckbox');

                checkboxes.forEach(function (checkbox) {
                    let sectionId = checkbox.getAttribute('data-section');

                    if (checkbox.checked) {
                        data.EnabledSections.push(sectionId);
                    }
                });

                ApiClient.ajax({
                    url: '/ModularHomeViews/UserSettings',
                    type: 'POST',
                    data: JSON.stringify(data),
                    contentType: 'application/json'
                })

                e.preventDefault();
                return false;
            });
    }
}


export default function (view) {
    config.setup(view);
}
