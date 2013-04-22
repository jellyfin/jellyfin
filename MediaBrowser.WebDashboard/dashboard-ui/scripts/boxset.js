(function ($, document, LibraryBrowser) {

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

            renderDetails(page, item);

            if (LibraryBrowser.shouldDisplayGallery(item)) {
                $('#galleryCollapsible', page).show();
            } else {
                $('#galleryCollapsible', page).hide();
            }

            $('#moviesCollapsible .collapsibleTitle', page).html('Titles (' + item.ChildCount + ')');

            Dashboard.hideLoadingMsg();
        });
    }

    function renderDetails(page, item) {

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        LibraryBrowser.renderOverview($('#itemOverview', page), item);

        if (item.CommunityRating) {
            $('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
        } else {
            $('#itemCommunityRating', page).hide();
        }

        $('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('#itemGenres', page), item);
        LibraryBrowser.renderStudios($('#itemStudios', page), item);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);
    }

    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderMovies(page) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: getParameterByName('id'),
            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio,ItemCounts,DisplayMediaType,DateCreated,UserData"

        }).done(function (result) {

            var html = LibraryBrowser.getPosterDetailViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
            });


            $('#moviesContent', page).html(html);
        });
    }

    function renderGallery(page, item) {

        var html = LibraryBrowser.getGalleryHtml(item);

        $('#galleryContent', page).html(html).trigger('create');
    }

    $(document).on('pageshow', "#boxsetPage", function () {
        
        var page = this;

        reload(page);

        $('#moviesCollapsible', page).on('expand.lazyload', function () {

            renderMovies(page);

            $(this).off('expand.lazyload');
        });

        $('#galleryCollapsible', page).on('expand.lazyload', function () {

            renderGallery(page, currentItem);

            $(this).off('expand.lazyload');
        });
        
    }).on('pagehide', "#boxsetPage", function () {

        var page = this;

        $('#moviesCollapsible', page).off('expand.lazyload');
        $('#galleryCollapsible', page).off('expand.lazyload');
    });


})(jQuery, document, LibraryBrowser);