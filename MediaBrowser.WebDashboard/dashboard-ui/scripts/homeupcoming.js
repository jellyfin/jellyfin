(function ($, document) {

    function loadUpcoming(page) {
        Dashboard.showLoadingMsg();

        var limit = AppInfo.hasLowImageBandwidth ?
         24 :
         40;

        var query = {

            Limit: limit,
            Fields: "AirTime,UserData,SeriesStudio,SyncInfo",
            UserId: Dashboard.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        query.ParentId = LibraryMenu.getTopParentId();

        var context = '';

        if (query.ParentId) {

            context = 'tv';

        }

        ApiClient.getJSON(ApiClient.getUrl("Shows/Upcoming", query)).done(function (result) {

            var items = result.Items;

            if (items.length) {
                page.querySelector('.noItemsMessage').style.display = 'none';
            } else {
                page.querySelector('.noItemsMessage').style.display = 'block';
            }

            var elem = page.querySelector('#upcomingItems');
            elem.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: items,
                showLocationTypeIndicator: false,
                shape: "backdrop",
                showTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true,
                context: context || 'home-upcoming',
                lazy: true,
                showDetailsMenu: true

            });

            ImageLoader.lazyChildren(elem);

            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    $(document).on('pageinitdepends', "#indexPage", function () {

        var page = this;
        var tabContent = page.querySelector('.homeUpcomingTabContent');

        $(page.querySelector('neon-animated-pages')).on('iron-select', function () {

            if (parseInt(this.selected) == 3) {
                if (LibraryBrowser.needsRefresh(tabContent)) {
                    loadUpcoming(tabContent);
                }
            }
        });
    });

})(jQuery, document);