define(['jQuery', 'emby-checkbox', 'fnchecked'], function ($) {

    var notificationsConfigurationKey = "notifications";

    function fillItems(elem, items, cssClass, idPrefix, currentList, isEnabledList) {

        var html = '<div class="checkboxList paperList" style="padding: .5em 1em;">';

        html += items.map(function (u) {

            var isChecked = isEnabledList ? currentList.indexOf(u.Id) != -1 : currentList.indexOf(u.Id) == -1;

            var checkedHtml = isChecked ? ' checked="checked"' : '';

            return '<label><input is="emby-checkbox" class="' + cssClass + '" type="checkbox" data-itemid="' + u.Id + '"' + checkedHtml + '/><span>' + u.Name + '</span></label>';

        }).join('');

        html += '</div>';

        elem.html(html).trigger('create');
    }

    function reload(page) {

        var type = getParameterByName('type');

        var promise1 = ApiClient.getUsers();
        var promise2 = ApiClient.getNamedConfiguration(notificationsConfigurationKey);
        var promise3 = ApiClient.getJSON(ApiClient.getUrl("Notifications/Types"));
        var promise4 = ApiClient.getJSON(ApiClient.getUrl("Notifications/Services"));

        Promise.all([promise1, promise2, promise3, promise4]).then(function (responses) {

            var users = responses[0];
            var notificationOptions = responses[1];
            var types = responses[2];
            var services = responses[3];

            var notificationConfig = notificationOptions.Options.filter(function (n) {

                return n.Type == type;

            })[0];

            var typeInfo = types.filter(function (n) {

                return n.Type == type;

            })[0] || {};

            if (typeInfo.IsBasedOnUserEvent) {
                $('.monitorUsers', page).show();
            } else {
                $('.monitorUsers', page).hide();
            }

            if (typeInfo.Variables.length) {
                $('.tokenHelp', page).show();

                $('.tokenList', page).html(typeInfo.Variables.map(function (v) {

                    return '{' + v + '}';

                }).join(', '));

            } else {
                $('.tokenHelp', page).hide();
            }

            $('.notificationType', page).html(typeInfo.Name || 'Unknown Notification');

            if (!notificationConfig) {

                notificationConfig = {
                    DisabledMonitorUsers: [],
                    SendToUsers: [],
                    DisabledServices: [],
                    SendToUserMode: 'Admins'
                };
            }

            fillItems($('.monitorUsersList', page), users, 'chkMonitor', 'chkMonitor', notificationConfig.DisabledMonitorUsers);
            fillItems($('.sendToUsersList', page), users, 'chkSendTo', 'chkSendTo', notificationConfig.SendToUsers, true);
            fillItems($('.servicesList', page), services, 'chkService', 'chkService', notificationConfig.DisabledServices);

            $('#chkEnabled', page).checked(notificationConfig.Enabled || false);

            $('#txtTitle', page).val(notificationConfig.Title || typeInfo.DefaultTitle);

            $('#selectUsers', page).val(notificationConfig.SendToUserMode).trigger('change');

        });
    }

    function save(page) {

        var type = getParameterByName('type');

        var promise1 = ApiClient.getNamedConfiguration(notificationsConfigurationKey);
        var promise2 = ApiClient.getJSON(ApiClient.getUrl("Notifications/Types"));

        Promise.all([promise1, promise2]).then(function (responses) {

            var notificationOptions = responses[0];
            var types = responses[1];

            var notificationConfig = notificationOptions.Options.filter(function (n) {

                return n.Type == type;

            })[0];

            if (!notificationConfig) {
                notificationConfig = {
                    Type: type
                };
                notificationOptions.Options.push(notificationConfig);
            }

            var typeInfo = types.filter(function (n) {

                return n.Type == type;

            })[0] || {};

            notificationConfig.Enabled = $('#chkEnabled', page).checked();
            notificationConfig.Title = $('#txtTitle', page).val();
            notificationConfig.SendToUserMode = $('#selectUsers', page).val();

            // Don't persist if it's just the default
            if (notificationConfig.Title == typeInfo.DefaultTitle) {
                notificationConfig.Title = null;
            }

            notificationConfig.DisabledMonitorUsers = $('.chkMonitor', page).get().filter(function (c) {
                return !c.checked;
            }).map(function (c) {
                return c.getAttribute('data-itemid');
            });

            notificationConfig.SendToUsers = $('.chkSendTo', page).get().filter(function (c) {
                return c.checked;
            }).map(function (c) {
                return c.getAttribute('data-itemid');
            });

            notificationConfig.DisabledServices = $('.chkService', page).get().filter(function (c) {
                return !c.checked;
            }).map(function (c) {
                return c.getAttribute('data-itemid');
            });

            ApiClient.updateNamedConfiguration(notificationsConfigurationKey, notificationOptions).then(function (r) {

                Dashboard.processServerConfigurationUpdateResult();
                Dashboard.navigate('notificationsettings.html');
            });

        });
    }

    function onSubmit() {
        var page = $(this).parents('.page');
        save(page);
        return false;
    }

    $(document).on('pageinit', "#notificationSettingPage", function () {

        var page = this;

        $('#selectUsers', page).on('change', function () {

            if (this.value == 'Custom') {
                $('.selectCustomUsers', page).show();
            } else {
                $('.selectCustomUsers', page).hide();
            }

        });

        $('.notificationSettingForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#notificationSettingPage", function () {

        var page = this;

        reload(page);
    });

});