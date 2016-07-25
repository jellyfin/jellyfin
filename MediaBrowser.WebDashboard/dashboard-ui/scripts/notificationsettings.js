define(['jQuery'], function ($) {

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Notifications/Types")).then(function (list) {

            var html = '';

            var lastCategory = "";

            html += list.map(function (i) {

                var itemHtml = '';

                if (i.Category != lastCategory) {
                    lastCategory = i.Category;

                    if (lastCategory) {
                        itemHtml += '</div>';

                    }
                    itemHtml += '<h1>';
                    itemHtml += i.Category;
                    itemHtml += '</h1>';

                    itemHtml += '<div class="paperList" style="margin-bottom:2em;">';
                }

                itemHtml += '<a class="clearLink" href="notificationsetting.html?type=' + i.Type + '">';
                itemHtml += '<paper-icon-item>';

                if (i.Enabled) {
                    itemHtml += '<paper-fab mini class="blue" icon="notifications-active" item-icon></paper-fab>';
                }
                else {
                    itemHtml += '<paper-fab mini style="background-color:#999;" icon="notifications-off" item-icon></paper-fab>';
                }

                itemHtml += '<paper-item-body two-line>';
                itemHtml += '<div>' + i.Name + '</div>';

                itemHtml += '</paper-item-body>';

                itemHtml += '<button type="button" is="paper-icon-button-light"><iron-icon icon="mode-edit"></iron-icon></button>';

                itemHtml += '</paper-icon-item>';
                itemHtml += '</a>';

                return itemHtml;

            }).join('');

            if (list.length) {
                html += '</div>';
            }

            $('.notificationList', page).html(html).trigger('create');

            Dashboard.hideLoadingMsg();
        });
    }

    function getTabs() {
        return [
        {
            href: 'notificationsettings.html',
            name: Globalize.translate('TabNotifications')
        },
        {
            href: 'appservices.html?context=notifications',
            name: Globalize.translate('TabServices')
        }];
    }

    return function (view, params) {

        view.addEventListener('viewshow', function () {

            LibraryMenu.setTabs('notifications', 0, getTabs);

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
                reload(view);
            });
        });
    };
});