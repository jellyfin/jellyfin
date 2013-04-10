(function ($, document, LibraryBrowser) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            var name = item.Name;

            Dashboard.setPageTitle(name);

            renderImage(page, item);

            renderDetails(page, item);

            $('#itemName', page).html(name);

            renderFavorites(page, item);
            LibraryBrowser.renderLinks(item);

            Dashboard.hideLoadingMsg();
        });
    }

    function renderImage(page, item) {

        $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));
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

        var miscInfo = [];

        if (item.ProductionYear) {
            miscInfo.push(item.ProductionYear);
        }

        if (item.OfficialRating) {
            miscInfo.push(item.OfficialRating);
        }

        if (item.RunTimeTicks) {

            var minutes = item.RunTimeTicks / 600000000;

            minutes = minutes || 1;

            miscInfo.push(parseInt(minutes) + "min");
        }

        if (item.DisplayMediaType) {
            miscInfo.push(item.DisplayMediaType);
        }

        if (item.VideoFormat && item.VideoFormat !== 'Standard') {
            miscInfo.push(item.VideoFormat);
        }

        $('#itemMiscInfo', page).html(miscInfo.join('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'));

        renderGenres(page, item);
        renderStudios(page, item);
    }

    function renderStudios(page, item) {
        if (item.Studios && item.Studios.length) {
            var elem = $('#itemStudios', page).show();

            var html = 'Studios:&nbsp;&nbsp;';

            for (var i = 0, length = item.Studios.length; i < length; i++) {

                if (i > 0) {
                    html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                }

                html += '<a href="itembynamedetails.html?studio=' + item.Studios[i] + '">' + item.Studios[i] + '</a>';
            }

            elem.html(html).trigger('create');


        } else {
            $('#itemStudios', page).hide();
        }
    }

    function renderGenres(page, item) {
        
        if (item.Genres && item.Genres.length) {
            var elem = $('#itemGenres', page).show();

            var html = 'Genres:&nbsp;&nbsp;';

            for (var i = 0, length = item.Genres.length; i < length; i++) {

                if (i > 0) {
                    html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                }

                html += '<a href="itembynamedetails.html?genre=' + item.Genres[i] + '">' + item.Genres[i] + '</a>';
            }

            elem.html(html).trigger('create');


        } else {
            $('#itemGenres', page).hide();
        }
    }
    
    function renderFavorites(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserRatingHtml(item));
    }

    $(document).on('pageshow', "#tvSeriesPage", function () {

        reload(this);

    }).on('pagehide', "#tvSeriesPage", function () {

        currentItem = null;
    });


})(jQuery, document, LibraryBrowser);

var tvSeriesPage = {

    setFavorite: function () {
        var item = tvSeriesPage.item;

        item.UserData = item.UserData || {};

        var setting = !item.UserData.IsFavorite;
        item.UserData.IsFavorite = setting;

        ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), item.Id, setting);

        renderFavorites(page, item);
    },

    setLike: function () {

        var item = tvSeriesPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = true;

        ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, true);

        renderFavorites(page, item);
    },

    clearLike: function () {

        var item = tvSeriesPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = undefined;

        ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), item.Id);

        renderFavorites(page, item);
    },

    setDislike: function () {
        var item = tvSeriesPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = false;

        ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, false);

        renderFavorites(page, item);
    },

    setPlayed: function () {
        var item = tvSeriesPage.item;

        item.UserData = item.UserData || {};

        var setting = !item.UserData.Played;
        item.UserData.Played = setting;

        ApiClient.updatePlayedStatus(Dashboard.getCurrentUserId(), item.Id, setting);

        renderFavorites(page, item);
    }

};