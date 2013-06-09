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
        if (item.Type == "Series") {
            $('#fldStatus', page).show();
            $('#fldAirDays', page).show();
            $('#fldAirTime', page).show();
        } else {
            $('#fldStatus', page).hide();
            $('#fldAirDays', page).hide();
            $('#fldAirTime', page).hide();
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
        
        if (item.Type == "MusicAlbum") {
            $('#fldMusicBrainzReleaseGroupId', page).show();
        } else {
            $('#fldMusicBrainzReleaseGroupId', page).hide();
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
        var selectStatus = $('#selectStatus', page);
        populateStatus(selectStatus);
        selectStatus.val(item.Status || "").selectmenu('refresh');
        var selectAirDays = $('#selectAirDays', page);
        populateAirDays(selectAirDays);
        selectAirDays.val(item.AirDays || "").selectmenu('refresh');
        populateListView($('#listGenres', page), item.Genres);
        populateListView($('#listStudios', page), item.Studios.map(function (element) { return element.Name || ''; }));
        populateListView($('#listTags', page), item.Tags);
        
        $('#txtName', page).val(item.Name || "");
        $('#txtOverview', page).val(item.Overview || "");
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
        $('#txtAirTime', page).val(convertTo24HourFormat(item.AirTime || ""));

        $('#txtOriginalAspectRatio', page).val(item.AspectRatio || "");

        var providerIds = item.ProviderIds || {};

        $('#txtGamesDb', page).val(providerIds.Gamesdb || "");
        $('#txtImdb', page).val(providerIds.Imdb || "");
        $('#txtTmdb', page).val(providerIds.Tmdb || "");
        $('#txtTvdb', page).val(providerIds.Tvdb || "");
        $('#txtTvCom', page).val(providerIds.Tvcom || "");
        $('#txtMusicBrainz', page).val(providerIds.Musicbrainz || "");
        $('#txtMusicBrainzReleaseGroupId', page).val(item.MusicBrainzReleaseGroupId || "");
        $('#txtRottenTomatoes', page).val(providerIds.RottenTomatoes || "");

    }

    function convertTo24HourFormat(time) {
        if (time == "")
            return time;
        var hours = Number(time.match(/^(\d+)/)[1]);
        var minutes = Number(time.match(/:(\d+)/)[1]);
        var ampm = time.match(/\s(.*)$/)[1];
        ampm = ampm.toUpperCase();
        if (ampm == "PM" && hours < 12) hours = hours + 12;
        if (ampm == "AM" && hours == 12) hours = 0;
        var sHours = hours.toString();
        var sMinutes = minutes.toString();
        if (hours < 10) sHours = "0" + sHours;
        if (minutes < 10) sMinutes = "0" + sMinutes;
        return sHours + ":" + sMinutes;
    }
    
    function convertTo12HourFormat(time) {
        if (time == "")
            return time;
        var hours = Number(time.match(/^(\d+)/)[1]);
        var minutes = Number(time.match(/:(\d+)/)[1]);
        var ampm = "AM";
        if (hours >= 12) {
            ampm = "PM";
            hours = hours - 12;
            hours = hours == 0 ? 12 : hours;
        }
        hours = hours == 0 ? 12 : hours;
        var sHours = hours.toString();
        var sMinutes = minutes.toString();
        if (hours < 10) sHours = "0" + sHours;
        if (minutes < 10) sMinutes = "0" + sMinutes;
        return sHours + ":" + sMinutes + " " + ampm;
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

    function populateStatus(select) {
        var html = "";

        html += "<option value=''></option>";
        html += "<option value='Continuing'>Continuing</option>";
        html += "<option value='Ended'>Ended</option>";
        select.html(html).selectmenu("refresh");
    }
    
    function populateAirDays(select) {
        var days = new Array("Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday");
        var html = "";
        html += "<option value=''></option>";
        for (var i = 0; i < days.length; i++) {
            html += "<option value='" + days[i] + "'>" + days[i] + "</option>";
        }
        select.html(html).selectmenu("refresh");
    }

    function populateListView(list, items) {
        items = items || new Array();
        items.sort(function(a, b) { return a.toLowerCase().localeCompare(b.toLowerCase()); });
        var html = '';
        for (var i = 0; i < items.length; i++) {
            html += '<li><a class="data">' + items[i] + '</a><a onclick="EditItemMetadataPage.RemoveElementFromListview(this)"></a></li>';
        }
        list.html(html).listview('refresh');
    }

    function editableListViewValues(list) {
        return list.find('a.data').map(function () { return $(this).text(); }).get();
    }
    
    function editItemMetadataPage() {

        var self = this;

        self.onSubmit = function () {

            var form = this;

            var item = {
                
                Id: getParameterByName('id'),
                Name: $('#txtName', form).val(),
                SortName: $('#txtSortName', form).val(),
                DisplayMediaType: $('#txtDisplayMediaType', form).val(),
                CommunityRating: $('#txtCommunityRating', form).val(),
                HomePageUrl: $('#txtHomePageUrl', form).val(),
                Budget: $('#txtBudget', form).val(),
                Revenue: $('#txtRevenue', form).val(),
                CriticRating: $('#txtCriticRating', form).val(),
                CriticRatingSummary: $('#txtCriticRatingSummary', form).val(),
                IndexNumber: $('#txtIndexNumber', form).val(),
                ParentIndexNumber: $('#txtParentIndexNumber', form).val(),
                Players: $('#txtPlayers', form).val(),
                Album: $('#txtAlbum', form).val(),
                AlbumArtist: $('#txtAlbumArtist', form).val(),
                Artists: [$('#txtArtist', form).val()],
                Overview: $('#txtOverview', form).val(),
                Status: $('#selectStatus', form).val(),
                AirDays: $('#selectAirDays', form).val(),
                AirTime: convertTo12HourFormat($('#txtAirTime', form).val()),
                Genres: editableListViewValues($("#listGenres", form)),
                Tags: editableListViewValues($("#listTags", form)),
                Studios: editableListViewValues($("#listStudios", form)).map(function (element) { return { Name: element }; }),
                    
                PremiereDate: $('#txtPremiereDate', form).val(),
                EndDate: $('#txtEndDate', form).val(),
                ProductionYear: $('#txtProductionYear', form).val(),
                AspectRatio: $('#txtOriginalAspectRatio', form).val(),
                
                Language: $('#selectLanguage', form).val(),
                OfficialRating: $('#selectOfficialRating', form).val(),
                CustomRating: $('#selectCustomRating', form).val(),
                People: currentItem.People,
                
                ProviderIds:
                {
                    Gamesdb: $('#txtGamesDb', form).val(),
                    Imdb: $('#txtImdb', form).val(),
                    Tmdb: $('#txtTmdb', form).val(),
                    Tvdb: $('#txtTvdb', form).val(),
                    Tvcom: $('#txtTvCom', form).val(),
                    Musicbrainz: $('#txtMusicBrainz', form).val(),
                    MusicBrainzReleaseGroupId: $('#txtMusicBrainzReleaseGroupId', form).val(),
                    RottenTomatoes: $('#txtRottenTomatoes', form).val()
                }

            };

            ApiClient.updateItem(item).done(function () {

                Dashboard.alert('Item saved.');

            });

            return false;
        };
        self.AddElementToEditableListview = function(source) {
            var input = $(source).parent().find('input[type="text"]');
            var text = input.val();
            input.val('');
            if (text == '') return;
            var list = $(source).parents('[data-role="editableListviewContainer"]').find('ul[data-role="listview"]');
            var items = editableListViewValues(list);
            items.push(text);
            populateListView(list, items);
        };

        self.RemoveElementFromListview = function(source) {
            var list = $(source).parents('ul[data-role="listview"]');
            $(source).parent().remove();
            list.listview('refresh');
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