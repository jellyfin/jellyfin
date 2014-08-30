(function () {

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Notifications/Types")).done(function (list) {

            var html = '<ul data-role="listview" data-inset="true">';

            var lastCategory = "";

            html += list.map(function (i) {

                var itemHtml = '';

                if (i.Category != lastCategory) {
                    lastCategory = i.Category;
                    itemHtml += '<li data-role="list-divider">';
                    itemHtml += i.Category;
                    itemHtml += '</li>';
                }

                itemHtml += '<li>';
                itemHtml += '<a href="notificationsetting.html?type=' + i.Type + '">';
                itemHtml += '<h3>' + i.Name + '</h3>';

                if (i.Enabled) {
                    itemHtml += '<p style="color:#009F00;">' + Globalize.translate('LabelEnabled') + '</p>';
                } else {
                    itemHtml += '<p style="color:#cc0000;">' + Globalize.translate('LabelDisabled') + '</p>';
                }

                itemHtml += '</a>';
                itemHtml += '</li>';

                return itemHtml;

            }).join('');

            html += '</ul>';

            $('.notificationList', page).html(html).trigger('create');

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageshow', "#notificationSettingsPage", function () {

        var page = this;

        reload(page);
    });

})(jQuery, window);