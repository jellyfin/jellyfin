define(['datetime', 'cardBuilder'], function (datetime, cardBuilder) {

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getBackdropShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function getProgramScheduleHtml(items, options) {

        options = options || {};

        var groups = [];

        var currentGroupName = '';
        var currentGroup = [];

        var i, length;

        for (i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var dateText = '';

            if (options.indexByDate !== false && item.StartDate) {
                try {

                    var premiereDate = datetime.parseISO8601Date(item.StartDate, true);

                    dateText = LibraryBrowser.getFutureDateText(premiereDate, true);

                } catch (err) {
                }
            }

            if (dateText != currentGroupName) {

                if (currentGroup.length) {
                    groups.push({
                        name: currentGroupName,
                        items: currentGroup
                    });
                }

                currentGroupName = dateText;
                currentGroup = [item];
            } else {
                currentGroup.push(item);
            }
        }

        if (currentGroup.length) {
            groups.push({
                name: currentGroupName,
                items: currentGroup
            });
        }

        var html = '';

        for (i = 0, length = groups.length; i < length; i++) {

            var group = groups[i];

            if (group.name) {
                html += '<div class="homePageSection">';

                html += '<h1 class="listHeader">' + group.name + '</h1>';
            }

            if (enableScrollX()) {
                html += '<div is="emby-itemscontainer" class="itemsContainer hiddenScrollX">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }

            html += cardBuilder.getCardsHtml({
                items: group.items,
                shape: getBackdropShape(),
                preferThumb: true,
                showTitle: true,
                showAirTime: true,
                showAirEndTime: true,
                showChannelName: true,
                cardLayout: true,
                action: 'programdialog',
                cardFooterAside: 'none',
                preferThumb: true,
                coverImage: true,
                overlayText: false

            });
            html += '</div>';

            if (group.name) {
                html += '</div>';
            }
        }

        return Promise.resolve(html);
    }

    function getTimersHtml(timers, options) {

        options = options || {};
        var items = timers.map(function (t) {
            t.Type = 'Timer';
            return t;
        });

        var groups = [];

        var currentGroupName = '';
        var currentGroup = [];

        var i, length;

        for (i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var dateText = '';

            if (options.indexByDate !== false && item.StartDate) {
                try {

                    var premiereDate = datetime.parseISO8601Date(item.StartDate, true);

                    dateText = LibraryBrowser.getFutureDateText(premiereDate, true);

                } catch (err) {
                }
            }

            if (dateText != currentGroupName) {

                if (currentGroup.length) {
                    groups.push({
                        name: currentGroupName,
                        items: currentGroup
                    });
                }

                currentGroupName = dateText;
                currentGroup = [item];
            } else {
                currentGroup.push(item);
            }
        }

        if (currentGroup.length) {
            groups.push({
                name: currentGroupName,
                items: currentGroup
            });
        }

        var html = '';

        for (i = 0, length = groups.length; i < length; i++) {

            var group = groups[i];

            if (group.name) {
                html += '<div class="homePageSection">';

                html += '<h1 class="listHeader">' + group.name + '</h1>';
            }

            if (enableScrollX()) {
                html += '<div is="emby-itemscontainer" class="itemsContainer hiddenScrollX">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }

            html += cardBuilder.getCardsHtml({
                items: group.items,
                shape: getBackdropShape(),
                showTitle: true,
                showAirTime: true,
                showAirEndTime: true,
                showChannelName: true,
                lazy: true,
                cardLayout: true,
                action: 'edit',
                cardFooterAside: 'none',
                preferThumb: true,
                coverImage: true,
                overlayText: false

            });
            html += '</div>';

            if (group.name) {
                html += '</div>';
            }
        }

        return Promise.resolve(html);
    }

    window.LiveTvHelpers = {

        getTimersHtml: getTimersHtml,
        getProgramScheduleHtml: getProgramScheduleHtml

    };
});