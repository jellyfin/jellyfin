(function ($, document, Notifications) {

    $(document).on("pageinit", "#notificationsPage", function () {

        // If there is no user logged in there can be no notifications
        if (!Dashboard.getCurrentUserId()) return;
        
        var elem = $(".notificationsList");
        var btn = $(".btnMarkReadContainer");
        var startIndex = 0;
        var limit = 10;

        Notifications.showNotificationsList(startIndex, limit, elem, btn);

        elem.on("click", ".btnPreviousPage", function (e) {

            e.preventDefault();

            startIndex = startIndex - limit;

            if (startIndex < 0) startIndex = 0;

            Notifications.showNotificationsList(startIndex, limit, elem, btn);

        })
            .on("click", ".btnNextPage", function (e) {

                e.preventDefault();

                startIndex = startIndex + limit;

                Notifications.showNotificationsList(startIndex, limit, elem, btn);

            });

        $(".readOnlyContent").on("click", ".btnMarkRead", function () {

            var ids = $(".notificationsList div").map(function () {

                return this.getAttribute('data-notificationid');

            }).get();

            Notifications.markNotificationsRead(ids, function () {

                Notifications.showNotificationsList(startIndex, limit, elem, btn);

            });

        });

    });

})(jQuery, document, Notifications);