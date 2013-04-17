(function ($, document, LibraryBrowser) {

    function reload(page) {

        Dashboard.showLoadingMsg();

        var getItemPromise;

        var name = getParameterByName('person');

        if (name) {
            getItemPromise = ApiClient.getPerson(name);
        } else {

            name = getParameterByName('studio');

            if (name) {

                getItemPromise = ApiClient.getStudio(name);

            } else {

                name = getParameterByName('genre');

                if (name) {
                    getItemPromise = ApiClient.getGenre(name);
                }
                else {
                    throw new Error('Invalid request');
                }
            }
        }

        var getUserDataPromise = ApiClient.getItembyNameUserData(Dashboard.getCurrentUserId(), name);

        $.when(getItemPromise, getUserDataPromise).done(function (response1, response2) {

            var item = response1[0];
            var userdata = response2[0];

            item.UserData = userdata;
            name = item.Name;

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('#itemName', page).html(name);

            renderDetails(page, item);
            renderTabs(page, item);

            Dashboard.hideLoadingMsg();
        });
    }

    function renderTabs(page, item) {

        var promise;
        
        if (item.Type == "Person") {
            promise = ApiClient.getPersonItemCounts(Dashboard.getCurrentUserId(), item.Name);
        }
        else if (item.Type == "Genre") {
            promise = ApiClient.getGenreItemCounts(Dashboard.getCurrentUserId(), item.Name);
        }
        else if (item.Type == "Studio") {
            promise = ApiClient.getStudioItemCounts(Dashboard.getCurrentUserId(), item.Name);
        } else {
            throw new Error("Unknown item type: " + item.Type);
        }

        promise.done(function (result) {

            var html = '<fieldset data-role="controlgroup" data-type="horizontal" class="libraryTabs">';

            html += '<legend></legend>';

            if (result.MovieCount) {

                html += '<input type="radio" name="ibnItems" id="radioMovies" value="on" data-mini="true">';
                html += '<label for="radioMovies">Movies (' + result.MovieCount + ')</label>';
            }

            if (result.SeriesCount) {

                html += '<input type="radio" name="ibnItems" id="radioShows" value="on" data-mini="true">';
                html += '<label for="radioShows">TV Shows (' + result.SeriesCount + ')</label>';
            }
            
            if (result.EpisodeGuestStarCount) {

                html += '<input type="radio" name="ibnItems" id="radioGuestStar" value="on" data-mini="true">';
                html += '<label for="radioGuestStar">Guest Starred (' + result.EpisodeGuestStarCount + ')</label>';
            }

            if (result.TrailerCount) {

                html += '<input type="radio" name="ibnItems" id="radioTrailers" value="on" data-mini="true">';
                html += '<label for="radioTrailers">Trailers (' + result.TrailerCount + ')</label>';
            }

            if (result.GameCount) {

                html += '<input type="radio" name="ibnItems" id="radioGames" value="on" data-mini="true">';
                html += '<label for="radioGames">Games (' + result.SeriesCount + ')</label>';
            }

            if (result.AlbumCount) {

                html += '<input type="radio" name="ibnItems" id="radioAlbums" value="on" data-mini="true">';
                html += '<label for="radioAlbums">Albums (' + result.AlbumCount + ')</label>';
            }

            if (result.SongCount) {

                html += '<input type="radio" name="ibnItems" id="radioSongs" value="on" data-mini="true">';
                html += '<label for="radioSongs">Songs (' + result.SongCount + ')</label>';
            }

            html += '</fieldset>';

            var elem = $('#items', page).html(html).trigger('create');

            $('input:first', elem).attr("checked", "checked").checkboxradio("refresh").click();
        });
    }

    function renderDetails(page, item) {

        if (item.Overview || item.OverviewHtml) {
            var overview = item.OverviewHtml || item.Overview;

            $('#itemOverview', page).html(overview).show();
            $('#itemOverview a').each(function () {
                $(this).attr("target", "_blank");
            });
        } else {
            $('#itemOverview', page).hide();
        }

        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);

        if (item.Type == "Person" && item.PremiereDate) {

            var birthday = parseISO8601Date(item.PremiereDate, { toLocal: true }).toDateString();

            $('#itemBirthday', page).show().html("Birthday:&nbsp;&nbsp;" + birthday);
        } else {
            $('#itemBirthday', page).hide();
        }

        if (item.Type == "Person" && item.EndDate) {

            var deathday = parseISO8601Date(item.EndDate, { toLocal: true }).toDateString();

            $('#itemDeathDate', page).show().html("Death day:&nbsp;&nbsp;" + deathday);
        } else {
            $('#itemDeathDate', page).hide();
        }

        if (item.Type == "Person" && item.ProductionLocations && item.ProductionLocations.length) {

            var gmap = '<a target="_blank" href="https://maps.google.com/maps?q=' + item.ProductionLocations[0] + '">' + item.ProductionLocations[0] + '</a>';

            $('#itemBirthLocation', page).show().html("Birthplace:&nbsp;&nbsp;" + gmap).trigger('create');
        } else {
            $('#itemBirthLocation', page).hide();
        }
    }

    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    $(document).on('pageshow', "#itemByNameDetailPage", function () {
        reload(this);
    });


})(jQuery, document, LibraryBrowser);