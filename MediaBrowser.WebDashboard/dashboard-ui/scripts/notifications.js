(function ($, document, Dashboard) {

    var userId;
    var getNotificationsSummaryPromise;

    function getNotificationsSummary() {

        getNotificationsSummaryPromise = getNotificationsSummaryPromise || ApiClient.getNotificationSummary(userId);

        return getNotificationsSummaryPromise;
    }

    function updateNotificationCount() {

        getNotificationsSummary().done(function (summary) {

            var elem = $('.btnNotifications').removeClass('levelNormal').removeClass('levelWarning').removeClass('levelError').html(summary.UnreadCount);

            if (summary.UnreadCount) {
                elem.addClass('level' + summary.MaxUnreadNotificationLevel);
            }
        });
    }

    function showNotificationsFlyout() {

        var context = this;

        var html = '<div data-role="popup" class="notificationsFlyout" style="min-width:250px;margin-top:30px;margin-right:20px;background: #f8f8f8;">';

        html += '<a href="#" data-rel="back" data-role="button" data-theme="b" data-icon="delete" data-iconpos="notext" class="ui-btn-right">Close</a>';

        html += '<div class="ui-bar-a" style="text-align:center;">';
        html += '<h3 style="margin: .5em 0;">Notifications</h3>';
        html += '</div>';

        html += '<div data-role="content" style="padding: 0;">';

        html += '<p class="notificationsFlyoutlist">Loading...';

        html += '</p>';

        html += '<p style="display:none;" class="btnMarkReadContainer"><button class="btnMarkRead" type="button" data-icon="check" data-mini="true">Mark these read</button></p>';
        html += '</div>';

        html += '</div>';

        $(document.body).append(html);

        $('.notificationsFlyout').popup({ positionTo: context }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        }).on('click', '.btnMarkRead', function () {


            var ids = $('.unreadFlyoutNotification').map(function () {

                return this.getAttribute('data-notificationid');

            }).get();

            ApiClient.markNotificationsRead(Dashboard.getCurrentUserId(), ids, true).done(function () {

                $('.notificationsFlyout').popup("close");

            });

        });

        refreshFlyoutContents();
    }

    function refreshFlyoutContents() {

        var limit = 5;
        var startIndex = 0;

        ApiClient.getNotifications(Dashboard.getCurrentUserId(), { StartIndex: startIndex, Limit: limit }).done(function (result) {

            listUnreadNotifications(result.Notifications, result.TotalRecordCount, startIndex, limit);

        });
    }

    function listUnreadNotifications(notifications, totalRecordCount, startIndex, limit) {

        var elem = $('.notificationsFlyoutlist');

        if (!totalRecordCount) {
            elem.html('<p style="padding:.5em 1em;">No unread notifications.</p>');
            $('.btnMarkReadContainer').hide();
            return;
        }

        if (notifications.filter(function (n) {

            return !n.IsRead;

        }).length) {
            $('.btnMarkReadContainer').show();
        } else {
            $('.btnMarkReadContainer').hide();
        }


        var html = '';

        for (var i = 0, length = notifications.length; i < length; i++) {

            var notification = notifications[i];

            html += getNotificationHtml(notification);

        }

        elem.html(html).trigger('create');
    }

    function getNotificationHtml(notification) {

        var html = '';

        var cssClass = notification.IsRead ? "flyoutNotification" : "flyoutNotification unreadFlyoutNotification";

        html += '<div data-notificationid="' + notification.Id + '" class="' + cssClass + '">';

        html += '<div class="notificationImage">';
        html += getImageHtml(notification);
        html += '</div>';

        html += '<div class="notificationContent">';

        html += '<p class="notificationName">' + notification.Name + '</p>';

        html += '<p>' + humane_date(notification.Date) + '</p>';

        if (notification.Description) {
            html += '<p>' + notification.Description + '</p>';
        }

        if (notification.Url) {
            html += '<p><a href="' + notification.Url + '" target="_blank">More information</a></p>';
        }

        html += '</div>';

        html += '</div>';

        return html;
    }

    function getImageHtml(notification) {

        if (notification.Level == "Error") {

            return '<div class="imgNotification imgNotificationError"><div class="imgNotificationInner imgNotificationIcon"></div></div>';

        }
        if (notification.Level == "Warning") {

            return '<div class="imgNotification imgNotificationWarning"><div class="imgNotificationInner imgNotificationIcon"></div></div>';

        }

        return '<div class="imgNotification imgNotificationNormal"><div class="imgNotificationInner imgNotificationIcon"></div></div>';

    }

    $(Dashboard).on('interiorheaderrendered', function (e, header, user) {

        if (!user || $('.notificationsButton', header).length) {
            return;
        }

        userId = user.Id;

        $('<a class="imageLink btnNotifications" href="#" title="Notifications">0</a>').insertAfter($('.btnCurrentUser', header)).on('click', showNotificationsFlyout);

        updateNotificationCount();
    });

    $(ApiClient).on("websocketmessage", function (e, msg) {


        if (msg.MessageType === "NotificationUpdated" || msg.MessageType === "NotificationAdded" || msg.MessageType === "NotificationsMarkedRead") {

            getNotificationsSummaryPromise = null;

            updateNotificationCount();
        }

    });


})(jQuery, document, Dashboard);