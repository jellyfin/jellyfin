define(['datetime', 'libraryBrowser', 'cardBuilder', 'apphost', 'scrollStyles', 'emby-itemscontainer'], function (datetime, libraryBrowser, cardBuilder, appHost) {

    function getUpcomingPromise(context, params) {

        Dashboard.showLoadingMsg();

        var query = {

            Limit: 40,
            Fields: "AirTime,UserData",
            UserId: Dashboard.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        query.ParentId = params.topParentId;

        return ApiClient.getJSON(ApiClient.getUrl("Shows/Upcoming", query));
    }

    function loadUpcoming(context, params, promise) {

        promise.then(function (result) {

            var items = result.Items;

            if (items.length) {
                context.querySelector('.noItemsMessage').style.display = 'none';
            } else {
                context.querySelector('.noItemsMessage').style.display = 'block';
            }

            var elem = context.querySelector('#upcomingItems');
            renderUpcoming(elem, items);

            Dashboard.hideLoadingMsg();
        });
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function renderUpcoming(elem, items) {

        var groups = [];

        var currentGroupName = '';
        var currentGroup = [];

        var i, length;

        for (i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var dateText = '';

            if (item.PremiereDate) {
                try {

                    var premiereDate = datetime.parseISO8601Date(item.PremiereDate, true);

                    if (datetime.isRelativeDay(premiereDate, -1)) {
                        dateText = Globalize.translate('Yesterday');
                    } else {
                        dateText = libraryBrowser.getFutureDateText(premiereDate, true);
                    }

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

        var html = '';

        for (i = 0, length = groups.length; i < length; i++) {

            var group = groups[i];

            html += '<div class="homePageSection">';
            html += '<h1 class="listHeader">' + group.name + '</h1>';

            var allowBottomPadding = true;

            if (enableScrollX()) {
                allowBottomPadding = false;
                html += '<div is="emby-itemscontainer" class="itemsContainer hiddenScrollX">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }

            var supportsImageAnalysis = appHost.supports('imageanalysis');

            html += cardBuilder.getCardsHtml({
                items: group.items,
                showLocationTypeIndicator: false,
                shape: getThumbShape(),
                showTitle: true,
                preferThumb: true,
                lazy: true,
                showDetailsMenu: true,
                centerText: !supportsImageAnalysis,
                showParentTitle: true,
                allowBottomPadding: allowBottomPadding,
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis

            });
            html += '</div>';

            html += '</div>';
        }

        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);
    }
    return function (view, params, tabContent) {

        var self = this;
        var upcomingPromise;

        self.preRender = function () {
            upcomingPromise = getUpcomingPromise(view, params);
        };

        self.renderTab = function () {

            loadUpcoming(tabContent, params, upcomingPromise);
        };
    };
});