(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            var name = item.Name;

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('#itemName', page).html(name);

            setInitialCollapsibleState(page, item);
            renderDetails(page, item);

            if (LibraryBrowser.shouldDisplayGallery(item)) {
                $('#galleryCollapsible', page).show();
            } else {
                $('#galleryCollapsible', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function setInitialCollapsibleState(page, item) {

        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
        }
    }

    function renderDetails(page, item) {

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        if (item.Overview || item.OverviewHtml) {
            var overview = item.OverviewHtml || item.Overview;

            $('#itemOverview', page).html(overview).show();
            $('#itemOverview a').each(function () {
                $(this).attr("target", "_blank");
            });
        } else {
            $('#itemOverview', page).hide();
        }

        if (item.CommunityRating) {
            $('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
        } else {
            $('#itemCommunityRating', page).hide();
        }

        $('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        $('#seasonsCollapsible .collapsibleTitle', page).html('Seasons (' + item.ChildCount + ')');

        LibraryBrowser.renderPremiereDate($('#itemPremiereDate', page), item);
        LibraryBrowser.renderGenres($('#itemGenres', page), item);
        LibraryBrowser.renderStudios($('#itemStudios', page), item);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);

        var airs = item.Status == "Ended" ? "Aired" : "Airs";

        if (item.AirDays && item.AirDays.length) {

            airs += " " + item.AirDays.map(function (i) {
                return i.substring(0, 3);
            }).join(',');
        }
        
        if (item.AirTime) {
            airs += " at " + item.AirTime;
        }
        
        if (item.Studios && item.Studios.length) {
            airs += " on " + item.Studios[0];
        }

        $('#itemAirTime', page).html(airs);
    }

    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderSeasons(page) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: getParameterByName('id'),
            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio,ItemCounts,DisplayMediaType,DateCreated,UserData"

        }).done(function (result) {

            var html = LibraryBrowser.getPosterDetailViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
            });

            $('#seasonsContent', page).html(html);
        });
    }

    function renderGallery(page, item) {

        var html = LibraryBrowser.getGalleryHtml(item);

        $('#galleryContent', page).html(html).trigger('create');
    }

    function renderCast(page, item) {
        var html = '';

        var casts = item.People || [];

        for (var i = 0, length = casts.length; i < length; i++) {

            var cast = casts[i];

            html += LibraryBrowser.createCastImage(cast);
        }

        $('#castContent', page).html(html);
    }

    $(document).on('pageshow', "#tvSeriesPage", function () {

        var page = this;

        reload(page);

        $('#seasonsCollapsible', page).on('expand.lazyload', function () {

            renderSeasons(page);

            $(this).off('expand.lazyload');
        });

        $('#castCollapsible', page).on('expand.lazyload', function () {
            renderCast(page, currentItem);

            $(this).off('expand.lazyload');
        });

        $('#galleryCollapsible', page).on('expand.lazyload', function () {

            renderGallery(page, currentItem);

            $(this).off('expand.lazyload');
        });

    }).on('pagehide', "#tvSeriesPage", function () {

        currentItem = null;
        var page = this;

        $('#seasonsCollapsible', page).off('expand.lazyload');
        $('#castCollapsible', page).off('expand.lazyload');
        $('#galleryCollapsible', page).off('expand.lazyload');
    });

})(jQuery, document, LibraryBrowser, window);
