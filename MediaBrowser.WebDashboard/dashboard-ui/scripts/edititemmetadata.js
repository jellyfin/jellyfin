(function ($, document, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            if (item.IsFolder) {
                $('#fldRecursive', page).show();
            } else {
                $('#fldRecursive', page).hide();
            }

            $('#btnRefresh', page).button('enable');

            $('#refreshLoading', page).hide();

            currentItem = item;

            LibraryBrowser.renderName(item, $('.itemName', page), true);
            LibraryBrowser.renderParentName(item, $('.parentName', page));

            setFieldVisibilities(page, item);
            fillItemInfo(page, item);

            Dashboard.hideLoadingMsg();
        });
    }

    function setFieldVisibilities(page, item) {

        if (item.Type == "Series" || item.Type == "Person") {
            $('#fldEndDate', page).show();
        } else {
            $('#fldEndDate', page).hide();
        }

        if (item.Type == "Movie" || item.MediaType == "Game" || item.MediaType == "Trailer" || item.Type == "MusicVideo") {
            $('#fldBudget', page).show();
            $('#fldRevenue', page).show();
        } else {
            $('#fldBudget', page).hide();
            $('#fldRevenue', page).hide();
        }

        if (item.MediaType == "Game") {
            $('#fldPlayers', page).show();
            $('#fldGamesDb', page).show();
        } else {
            $('#fldPlayers', page).hide();
            $('#fldGamesDb', page).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "MusicVideo") {
            $('#fldCriticRating', page).show();
            $('#fldCriticRatingSummary', page).show();
            $('#fldRottenTomatoes', page).show();
        } else {
            $('#fldCriticRating', page).hide();
            $('#fldCriticRatingSummary', page).hide();
            $('#fldRottenTomatoes', page).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Person" || item.Type == "BoxSet") {
            $('#fldTmdb', page).show();
        } else {
            $('#fldTmdb', page).hide();
        }

        if (item.Type == "Series" || item.Type == "Season" || item.Type == "Episode") {
            $('#fldTvdb', page).show();
            $('#fldTvCom', page).show();
        } else {
            $('#fldTvdb', page).hide();
            $('#fldTvCom', page).hide();
        }

        if (item.Type == "Audio") {
            $('#fldArtist', page).show();
            $('#fldAlbum', page).show();
            $('#fldAlbumArtist', page).show();
        } else {
            $('#fldArtist', page).hide();
            $('#fldAlbum', page).hide();
            $('#fldAlbumArtist', page).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Person" || item.Type == "Series" || item.Type == "Season" || item.Type == "Episode" || item.Type == "MusicVideo") {
            $('#fldImdb', page).show();
        } else {
            $('#fldImdb', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Artist" || item.Type == "MusicArtist" || item.Type == "MusicAlbum") {
            $('#fldMusicBrainz', page).show();
        } else {
            $('#fldMusicBrainz', page).hide();
        }

        if (item.MediaType == "Video") {
            $('#fldOriginalAspectRatio', page).show();
        } else {
            $('#fldOriginalAspectRatio', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode" || item.Type == "Season") {
            $('#fldIndexNumber', page).show();

            if (item.Type == "Episode") {
                $('#lblIndexNumber', page).html('Episode number');
            }
            else if (item.Type == "Season") {
                $('#lblIndexNumber', page).html('Season number');
            }
            else if (item.Type == "Audio") {
                $('#lblIndexNumber', page).html('Track number');
            }
            else {
                $('#lblIndexNumber', page).html('Number');
            }
        } else {
            $('#fldIndexNumber', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode") {
            $('#fldParentIndexNumber', page).show();

            if (item.Type == "Episode") {
                $('#lblParentIndexNumber', page).html('Season number');
            }
            else if (item.Type == "Audio") {
                $('#lblParentIndexNumber', page).html('Disc number');
            }
            else {
                $('#lblParentIndexNumber', page).html('Parent number');
            }
        } else {
            $('#fldParentIndexNumber', page).hide();
        }
    }

    function fillItemInfo(page, item) {

        ApiClient.getCultures().done(function (result) {

            var select = $('#selectLanguage', page);

            populateLanguages(result, select);

            select.val(item.Language || "").selectmenu('refresh');
        });

        ApiClient.getParentalRatings().done(function (result) {

            var select = $('#selectOfficialRating', page);

            populateRatings(result, select);

            select.val(item.OfficialRating || "").selectmenu('refresh');

            select = $('#selectCustomRating', page);

            populateRatings(result, select);

            select.val(item.CustomRating || "").selectmenu('refresh');
        });

        $('#txtName', page).val(item.Name || "");
        $('#txtSortName', page).val(item.SortName || "");
        $('#txtDisplayMediaType', page).val(item.DisplayMediaType || "");
        $('#txtCommunityRating', page).val(item.CommunityRating || "");
        $('#txtHomePageUrl', page).val(item.HomePageUrl || "");

        $('#txtBudget', page).val(item.Budget || "");
        $('#txtRevenue', page).val(item.Revenue || "");

        $('#txtCriticRating', page).val(item.CriticRating || "");
        $('#txtCriticRatingSummary', page).val(item.CriticRatingSummary || "");

        $('#txtIndexNumber', page).val(item.IndexNumber || "");
        $('#txtParentIndexNumber', page).val(item.ParentIndexNumber || "");
        $('#txtPlayers', page).val(item.Players || "");

        $('#txtAlbum', page).val(item.Album || "");
        $('#txtAlbumArtist', page).val(item.AlbumArtist || "");

        var artists = item.Artists || [];
        $('#txtArtist', page).val(artists.join(','));

        var date;

        if (item.PremiereDate) {
            try {
                date = parseISO8601Date(item.PremiereDate, { toLocal: true });

                $('#txtPremiereDate', page).val(date.toISOString().slice(0, 10));
            }
            catch (e) {
                $('#txtPremiereDate', page).val('');
            }
        } else {
            $('#txtPremiereDate', page).val('');
        }

        if (item.EndDate) {
            try {
                date = parseISO8601Date(item.EndDate, { toLocal: true });

                $('#txtEndDate', page).val(date.toISOString().slice(0, 10));
            }
            catch (e) {
                $('#txtEndDate', page).val('');
            }
        } else {
            $('#txtEndDate', page).val('');
        }

        $('#txtProductionYear', page).val(item.ProductionYear || "");

        $('#txtOriginalAspectRatio', page).val(item.AspectRatio || "");

        var providerIds = item.ProviderIds || {};

        $('#txtGamesDb', page).val(providerIds.Gamesdb || "");
        $('#txtImdb', page).val(providerIds.Imdb || "");
        $('#txtTmdb', page).val(providerIds.Tmdb || "");
        $('#txtTvdb', page).val(providerIds.Tvdb || "");
        $('#txtTvCom', page).val(providerIds.Tvcom || "");
        $('#txtMusicBrainz', page).val(providerIds.Musicbrainz || "");
        $('#txtRottenTomatoes', page).val(providerIds.RottenTomatoes || "");

    }

    function populateLanguages(allCultures, select) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = allCultures.length; i < length; i++) {

            var culture = allCultures[i];

            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        select.html(html).selectmenu("refresh");
    }

    function populateRatings(allParentalRatings, select) {

        var html = "";

        html += "<option value=''></option>";

        var ratings = [];
        var i, length, rating;

        for (i = 0, length = allParentalRatings.length; i < length; i++) {

            rating = allParentalRatings[i];

            ratings.push({ Name: rating.Name, Value: rating.Name });
        }

        for (i = 0, length = ratings.length; i < length; i++) {

            rating = ratings[i];

            html += "<option value='" + rating.Value + "'>" + rating.Name + "</option>";
        }

        select.html(html).selectmenu("refresh");
    }

    function editItemMetadataPage() {
        var self = this;

        self.onSubmit = function () {

            Dashboard.alert('coming soon');

            return false;
        };
    }

    window.EditItemMetadataPage = new editItemMetadataPage();

    $(document).on('pageinit', "#editItemMetadataPage", function () {

        var page = this;


        $('#btnRefresh', this).on('click', function () {

            $(this).button('disable');

            $('#refreshLoading', page).show();

            ApiClient.refreshItem(currentItem.Id, true, $('#fldRecursive', page).checked()).done(function () {

                reload(page);

            });
        });

    }).on('pageshow', "#editItemMetadataPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#editItemMetadataPage", function () {

        var page = this;

        currentItem = null;
    });

})(jQuery, document, window);