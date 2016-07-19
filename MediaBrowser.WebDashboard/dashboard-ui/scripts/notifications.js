define(['libraryBrowser'], function (libraryBrowser) {

    function notifications() {

        var self = this;

        self.getNotificationsSummaryPromise = null;

        self.total = 0;

        self.getNotificationsSummary = function () {

            var apiClient = window.ApiClient;

            if (!apiClient) {
                return;
            }

            self.getNotificationsSummaryPromise = self.getNotificationsSummaryPromise || apiClient.getNotificationSummary(Dashboard.getCurrentUserId());

            return self.getNotificationsSummaryPromise;
        };

        self.updateNotificationCount = function () {

            if (!Dashboard.getCurrentUserId()) {
                return;
            }

            if (!window.ApiClient) {
                return;
            }

            var promise = self.getNotificationsSummary();

            if (!promise) {
                return;
            }

            promise.then(function (summary) {

                var btnNotificationsInner = document.querySelector('.btnNotificationsInner');
                if (btnNotificationsInner) {

                    btnNotificationsInner.classList.remove('levelNormal');
                    btnNotificationsInner.classList.remove('levelWarning');
                    btnNotificationsInner.classList.remove('levelError');
                    btnNotificationsInner.innerHTML = summary.UnreadCount;

                    if (summary.UnreadCount) {
                        btnNotificationsInner.classList.add('level' + summary.MaxUnreadNotificationLevel);
                    }
                }
            });
        };

        self.markNotificationsRead = function (ids, callback) {

            ApiClient.markNotificationsRead(Dashboard.getCurrentUserId(), ids, true).then(function () {

                self.getNotificationsSummaryPromise = null;

                self.updateNotificationCount();

                if (callback) {
                    callback();
                }

            });

        };

        self.showNotificationsList = function (startIndex, limit, elem) {

            refreshNotifications(startIndex, limit, elem, true);

        };
    }

    function refreshNotifications(startIndex, limit, elem, showPaging) {

        var apiClient = window.ApiClient;

        if (apiClient) {
            return apiClient.getNotifications(Dashboard.getCurrentUserId(), { StartIndex: startIndex, Limit: limit }).then(function (result) {

                listUnreadNotifications(result.Notifications, result.TotalRecordCount, startIndex, limit, elem, showPaging);

            });
        }
    }

    function listUnreadNotifications(list, totalRecordCount, startIndex, limit, elem, showPaging) {

        if (!totalRecordCount) {
            elem.html('<p style="padding:.5em 1em;">' + Globalize.translate('LabelNoUnreadNotifications') + '</p>');

            return;
        }

        Notifications.total = totalRecordCount;

        var html = '';

        if (totalRecordCount > limit && showPaging === true) {

            var query = { StartIndex: startIndex, Limit: limit };

            html += libraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: totalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false
            });
        }

        require(['humanedate', 'paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
            for (var i = 0, length = list.length; i < length; i++) {

                var notification = list[i];

                html += getNotificationHtml(notification);

            }

            elem.html(html).trigger('create');
        });
    }

    function getNotificationHtml(notification) {

        var itemHtml = '';

        if (notification.Url) {
            itemHtml += '<a class="clearLink" href="' + notification.Url + '" target="_blank">';
        }

        itemHtml += '<paper-icon-item>';

        if (notification.Level == "Error") {
            itemHtml += '<paper-fab mini class="" style="background:#cc3333;" icon="error" item-icon></paper-fab>';
        } else {
            itemHtml += '<paper-fab mini  class="blue" icon="dvr" item-icon></paper-fab>';
        }

        itemHtml += '<paper-item-body three-line>';

        itemHtml += '<div>';
        itemHtml += notification.Name;
        itemHtml += '</div>';

        itemHtml += '<div secondary>';
        itemHtml += humane_date(notification.Date);
        itemHtml += '</div>';

        if (notification.Description) {
            itemHtml += '<div secondary>';
            itemHtml += notification.Description;
            itemHtml += '</div>';
        }

        itemHtml += '</paper-item-body>';

        itemHtml += '</paper-icon-item>';

        if (notification.Url) {
            itemHtml += '</a>';
        }

        return itemHtml;
    }

    window.Notifications = new notifications();
    var needsRefresh = true;

    function onWebSocketMessage(e, msg) {
        if (msg.MessageType === "NotificationUpdated" || msg.MessageType === "NotificationAdded" || msg.MessageType === "NotificationsMarkedRead") {

            Notifications.getNotificationsSummaryPromise = null;

            Notifications.updateNotificationCount();
        }
    }

    function initializeApiClient(apiClient) {
        Events.off(apiClient, "websocketmessage", onWebSocketMessage);
        Events.on(apiClient, "websocketmessage", onWebSocketMessage);
    }

    if (window.ApiClient) {
        initializeApiClient(window.ApiClient);
    }

    Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
        initializeApiClient(apiClient);
    });

    Events.on(ConnectionManager, 'localusersignedin', function () {
        needsRefresh = true;
    });

    Events.on(ConnectionManager, 'localusersignedout', function () {
        needsRefresh = true;
    });

    pageClassOn('pageshow', "type-interior", function () {

        if (needsRefresh) {
            Notifications.updateNotificationCount();
        }

    });

});