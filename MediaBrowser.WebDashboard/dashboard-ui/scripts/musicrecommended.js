(function ($, document) {

    function itemsPerRow() {

        var screenWidth = $(window).width();

        return screenWidth >= 1920 ? 9 : (screenWidth >= 1200 ? 12 : (screenWidth >= 1000 ? 10 : 8));
    }

    function enableScrollX() {
        return $.browser.mobile && AppInfo.enableAppLayouts;
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'square';
    }

    function loadLatest(page, parentId) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var options = {
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            var elem = page.querySelector('#recentlyAddedSongs');
            elem.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: items,
                showUnplayedIndicator: false,
                showLatestItemsPopup: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                lazy: true,
                centerText: true,
                overlayPlayButton: true

            });
            ImageLoader.lazyChildren(elem);

            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    function loadRecentlyPlayed(page, parentId) {

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo,SyncInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#recentlyPlayed', page).show()[0];
            } else {
                elem = $('#recentlyPlayed', page).hide()[0];
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                defaultAction: 'play',
                lazy: true,
                centerText: true,
                overlayMoreButton: true

            });
            ImageLoader.lazyChildren(itemsContainer);

        });

    }

    function loadFrequentlyPlayed(page, parentId) {

        var options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo,SyncInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#topPlayed', page).show()[0];
            } else {
                elem = $('#topPlayed', page).hide()[0];
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                defaultAction: 'play',
                lazy: true,
                centerText: true,
                overlayMoreButton: true

            });
            ImageLoader.lazyChildren(itemsContainer);

        });

    }

    function loadPlaylists(page, parentId) {

        var options = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "Playlist",
            Recursive: true,
            ParentId: parentId,
            Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete,SyncInfo",
            StartIndex: 0,
            Limit: itemsPerRow()
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#playlists', page).show()[0];
            } else {
                elem = $('#playlists', page).hide()[0];
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: getSquareShape(),
                showTitle: true,
                lazy: true,
                defaultAction: 'play',
                coverImage: true,
                showItemCounts: true,
                centerText: true,
                overlayMoreButton: true

            });
            ImageLoader.lazyChildren(itemsContainer);

        });
    }

    $(document).on('pagebeforeshowready', "#musicRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();

        var page = this;

        var containers = page.querySelectorAll('.itemsContainer');
        if (enableScrollX()) {
            $(containers).addClass('hiddenScrollX');
        } else {
            $(containers).removeClass('hiddenScrollX');
        }

        if (LibraryBrowser.needsRefresh(page)) {
            loadLatest(page, parentId);
            loadPlaylists(page, parentId);
            loadRecentlyPlayed(page, parentId);
            loadFrequentlyPlayed(page, parentId);
        }
    });


})(jQuery, document);