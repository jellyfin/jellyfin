(function ($, document, Notifications) {

    $(document).on("pageshow", "#notificationsPage", function () {

        // If there is no user logged in there can be no notifications
        if (!Dashboard.getCurrentUserId()) return;

        var elem = $(".notificationsList");
        var startIndex = 0;
        var limit = 10;

        Notifications.showNotificationsList(startIndex, limit, elem);

        elem.on("click", ".btnPreviousPage", function (e) {

            e.preventDefault();

            startIndex = startIndex - limit;

            if (startIndex < 0) startIndex = 0;

            Notifications.showNotificationsList(startIndex, limit, elem);

        })
            .on("click", ".btnNextPage", function (e) {

                e.preventDefault();

                startIndex = startIndex + limit;

                Notifications.showNotificationsList(startIndex, limit, elem);

            });

        Notifications.markNotificationsRead([]);

    });

})(jQuery, document, Notifications);