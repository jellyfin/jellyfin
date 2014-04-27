(function () {

    function fillItems(elem, items, cssClass, idPrefix, disabledList) {

        var html = '<div data-role="controlgroup">';

        html += items.map(function (u) {

            var id = idPrefix + u.Id;

            var checkedHtml = disabledList.indexOf(u.Id) != -1 ? '' : ' checked="checked"';

            return '<label for="' + id + '">' + u.Name + '</label><input class="' + cssClass + '" type="checkbox" data-itemid="' + u.Id + '" data-mini="true" id="' + id + '"' + checkedHtml + ' />';

        }).join('');

        html += '</div>';

        elem.html(html).trigger('create');
    }

    function reload(page) {

        var type = getParameterByName('type');

        var promise1 = ApiClient.getUsers();
        var promise2 = ApiClient.getServerConfiguration();
        var promise3 = $.getJSON(ApiClient.getUrl("Notifications/Types"));
        var promise4 = $.getJSON(ApiClient.getUrl("Notifications/Services"));

        $.when(promise1, promise2, promise3, promise4).done(function (response1, response2, response3, response4) {

            var users = response1[0];
            var config = response2[0];
            var types = response3[0];
            var services = response4[0];

            var notificationConfig = config.NotificationOptions.Options.filter(function (n) {

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
                    DisabledSendToUsers: [],
                    DisabledServices: []
                };
            }

            fillItems($('.monitorUsersList', page), users, 'chkMonitor', 'chkMonitor', notificationConfig.DisabledMonitorUsers);
            fillItems($('.sendToUsersList', page), users, 'chkSendTo', 'chkSendTo', notificationConfig.DisabledSendToUsers);
            fillItems($('.servicesList', page), services, 'chkService', 'chkService', notificationConfig.DisabledServices);

            $('#chkEnabled', page).checked(notificationConfig.Enabled || false).checkboxradio('refresh');

            $('#txtTitle', page).val(notificationConfig.Title || typeInfo.DefaultTitle);

        });
    }

    function save(page) {

        var type = getParameterByName('type');

        var promise1 = ApiClient.getServerConfiguration();
        var promise2 = $.getJSON(ApiClient.getUrl("Notifications/Types"));

        $.when(promise1, promise2).done(function (response1, response2) {

            var config = response1[0];
            var types = response2[0];

            var notificationOptions = config.NotificationOptions;

            var notificationConfig = config.NotificationOptions.Options.filter(function (n) {

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

            // Don't persist if it's just the default
            if (notificationConfig.Title == typeInfo.DefaultTitle) {
                notificationConfig.Title = null;
            }

            notificationConfig.DisabledMonitorUsers = $('.chkMonitor:not(:checked)', page).get().map(function (c) {
                return c.getAttribute('data-itemid');
            });

            notificationConfig.DisabledSendToUsers = $('.chkSendTo:not(:checked)', page).get().map(function (c) {
                return c.getAttribute('data-itemid');
            });

            notificationConfig.DisabledServices = $('.chkService:not(:checked)', page).get().map(function (c) {
                return c.getAttribute('data-itemid');
            });

            ApiClient.updateServerConfiguration(config).done(function(r) {

                Dashboard.navigate('notificationsettings.html');
            });

        });
    }

    $(document).on('pageshow', "#notificationSettingPage", function () {

        var page = this;

        reload(page);
    });

    window.NotificationSettingPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');
            save(page);
            return false;
        }
    };

})(jQuery, window);