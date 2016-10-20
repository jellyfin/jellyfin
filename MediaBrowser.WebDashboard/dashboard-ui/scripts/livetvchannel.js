define(['datetime', 'listView'], function (datetime, listView) {

    function isSameDay(date1, date2) {

        return date1.toDateString() === date2.toDateString();
    }

    function renderPrograms(page, result) {

        var html = '';
        var currentItems = [];
        var currentStartDate = null;

        for (var i = 0, length = result.Items.length; i < length; i++) {

            var item = result.Items[i];

            var itemStartDate = datetime.parseISO8601Date(item.StartDate);
            if (!currentStartDate || !isSameDay(currentStartDate, itemStartDate)) {

                if (currentItems.length) {

                    html += '<h1>' + datetime.toLocaleDateString(itemStartDate, { weekday: 'long', month: 'long', day: 'numeric' }) + '</h1>';

                    html += '<div is="emby-itemscontainer" class="vertical-list">' + listView.getListViewHtml({
                        items: currentItems,
                        enableUserDataButtons: false,
                        showParentTitle: true,
                        image: false,
                        showProgramTime: true,
                        mediaInfo: false,
                        parentTitleWithTitle: true

                    }) + '</div>';
                }

                currentStartDate = itemStartDate;
                currentItems = [];

            }

            currentItems.push(item);
        }

        page.querySelector('#childrenContent').innerHTML = html;
    }

    function loadPrograms(page, channelId) {

        ApiClient.getLiveTvPrograms({

            ChannelIds: channelId,
            UserId: Dashboard.getCurrentUserId(),
            HasAired: false,
            SortBy: "StartDate",
            EnableTotalRecordCount: false,
            EnableImages: false,
            ImageTypeLimit: 0,
            EnableUserData: false

        }).then(function (result) {

            renderPrograms(page, result);
            Dashboard.hideLoadingMsg();
        });
    }

    return {
        renderPrograms: loadPrograms
    };

});