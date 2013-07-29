(function ($, document, window) {

    var currentItem;

    function getPromise() {

        var name = getParameterByName('person');

        if (name) {
            return ApiClient.getPerson(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('studio');

        if (name) {

            return ApiClient.getStudio(name, Dashboard.getCurrentUserId());

        }

        name = getParameterByName('genre');

        if (name) {
            return ApiClient.getGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicgenre');

        if (name) {
            return ApiClient.getMusicGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('gamegenre');

        if (name) {
            return ApiClient.getGameGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('artist');

        if (name) {
            return ApiClient.getArtist(name, Dashboard.getCurrentUserId());
        }
        else {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), getParameterByName('id'));
        }
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        getPromise().done(function (item) {

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
            
            if (item.Type == "Person" || item.Type == "Studio" || item.Type == "MusicGenre" || item.Type == "Genre" || item.Type == "Artist") {
                $('#peopleTab', page).hide();
            } else {
                $('#peopleTab', page).show();
            }

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

        if (item.Type == "MusicAlbum") {
            $('#albumAssociationMessage', page).show();
        } else {
            $('#albumAssociationMessage', page).hide();
        }

        if (item.MediaType == "Game" || item.Type == "MusicAlbum") {
            $('#fldGamesDb', page).show();
        } else {
            $('#fldGamesDb', page).hide();
        }

        if (item.MediaType == "Game") {
            $('#fldPlayers', page).show();
        } else {
            $('#fldPlayers', page).hide();
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

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Person" || item.Type == "BoxSet" || item.Type == "MusicAlbum") {
            $('#fldTmdb', page).show();
        } else {
            $('#fldTmdb', page).hide();
        }

        if (item.Type == "Series" || item.Type == "Season" || item.Type == "Episode" || item.Type == "MusicAlbum") {
            $('#fldTvdb', page).show();
            $('#fldTvCom', page).show();
        } else {
            $('#fldTvdb', page).hide();
            $('#fldTvCom', page).hide();
        }

        if (item.Type == "Series" || item.Type == "Season" || item.Type == "Episode") {
            $('#fldTvCom', page).show();
        } else {
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

        if (item.MediaType == "Video") {
            $('#fld3dFormat', page).show();
        } else {
            $('#fld3dFormat', page).hide();
        }

        if (item.Type == "Audio") {
            $('#fldAlbumArtist', page).show();
        } else {
            $('#fldAlbumArtist', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "MusicVideo") {
            $('#fldArtist', page).show();
            $('#fldAlbum', page).show();
        } else {
            $('#fldArtist', page).hide();
            $('#fldAlbum', page).hide();
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

        if (item.Type == "Person" || item.Type == "Genre" || item.Type == "Studio" || item.Type == "GameGenre" || item.Type == "MusicGenre") {
            $('#fldCommunityRating', page).hide();
            $('#fldOfficialRating', page).hide();
            $('#fldCustomRating', page).hide();
            $('#genresCollapsible', page).hide();
            $('#studiosCollapsible', page).hide();
        } else {
            $('#fldCommunityRating', page).show();
            $('#fldOfficialRating', page).show();
            $('#fldCustomRating', page).show();
            $('#genresCollapsible', page).show();
            $('#studiosCollapsible', page).show();
        }

        if (item.Type == "Person") {
            $('#lblPremiereDate', page).html('Date of birth');
            $('#lblYear', page).html('Birth year');
            $('#lblEndDate', page).html('Death date');
        } else {
            $('#lblPremiereDate', page).html('Release date');
            $('#lblYear', page).html('Year');
            $('#lblEndDate', page).html('End date');
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

        $('#select3dFormat', page).val(item.Video3DFormat || "").selectmenu('refresh');

        populateListView($('#listAirDays', page), item.AirDays);
        populateListView($('#listGenres', page), item.Genres);
        populateListView($('#listStudios', page), item.Studios.map(function (element) { return element.Name || ''; }));
        populateListView($('#listTags', page), item.Tags);
        var enableInternetProviders = (item.EnableInternetProviders || false);
        $("#enableInternetProviders", page).attr('checked', enableInternetProviders).checkboxradio('refresh');
        if (enableInternetProviders) {
            $('#providerSettingsContainer', page).show();
        } else {
            $('#providerSettingsContainer', page).hide();
        }
        populateInternetProviderSettings(page, item.LockedFields);

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

        $('#txtIndexNumber', page).val(('IndexNumber' in item) ? item.IndexNumber : "");
        $('#txtParentIndexNumber', page).val(('ParentIndexNumber' in item) ? item.ParentIndexNumber : "");
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
        var match = time.match(/^(\d+):(\d+)(.*)$/);
        if (match) {
            var hours = Number(match[1]);
            var minutes = Number(match[2]);
            var ampm = $.trim(match[3]);
            ampm = ampm.toUpperCase();
            if (ampm == "PM" && hours < 12) hours = hours + 12;
            if (ampm == "AM" && hours == 12) hours = 0;
            var sHours = hours.toString();
            var sMinutes = minutes.toString();
            if (hours < 10) sHours = "0" + sHours;
            if (minutes < 10) sMinutes = "0" + sMinutes;
            return sHours + ":" + sMinutes;
        } else {
            return time;
        }
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

    function populateListView(list, items, sortCallback) {
        items = items || new Array();
        if (typeof (sortCallback) === 'undefined') {
            items.sort(function (a, b) { return a.toLowerCase().localeCompare(b.toLowerCase()); });
        } else {
            items = sortCallback(items);
        }
        var html = '';
        for (var i = 0; i < items.length; i++) {
            html += '<li><a class="data">' + items[i] + '</a><a onclick="EditItemMetadataPage.removeElementFromListview(this)"></a></li>';
        }
        list.html(html).listview('refresh');
    }

    function editableListViewValues(list) {
        return list.find('a.data').map(function () { return $(this).text(); }).get();
    }

    function generateSliders(fields, type) {
        var html = '';
        for (var i = 0; i < fields.length; i++) {
            var field = fields[i];
            var fieldTitle = $.trim(field.replace(/([A-Z])/g, ' $1'));
            html += '<div data-role="fieldcontain">';
            html += '<label for="lock' + field + '">' + fieldTitle + ':</label>';
            html += '<select name="lock' + type + '" id="lock' + field + '" data-role="slider" data-mini="true">';
            html += '<option value="' + field + '">Off</option>';
            html += '<option value="" selected="selected">On</option>';
            html += '</select>';
            html += '</div>';
        }
        return html;
    }
    function populateInternetProviderSettings(page, lockedFields) {
        var container = $('#providerSettingsContainer', page);
        lockedFields = lockedFields || new Array();
        var metadatafields = new Array("Name", "Overview", "Cast", "Genres", "ProductionLocations", "Studios", "Tags");
        var html = '';
        html += "<h3>Fields</h3>";
        html += generateSliders(metadatafields, 'Fields');
        container.html(html).trigger('create');
        for (var fieldIndex = 0; fieldIndex < lockedFields.length; fieldIndex++) {
            var field = lockedFields[fieldIndex];
            $('#lock' + field).val(field).slider('refresh');
        }
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
                AirDays: editableListViewValues($("#listAirDays", form)),
                AirTime: convertTo12HourFormat($('#txtAirTime', form).val()),
                Genres: editableListViewValues($("#listGenres", form)),
                Tags: editableListViewValues($("#listTags", form)),
                Studios: editableListViewValues($("#listStudios", form)).map(function (element) { return { Name: element }; }),

                PremiereDate: $('#txtPremiereDate', form).val(),
                EndDate: $('#txtEndDate', form).val(),
                ProductionYear: $('#txtProductionYear', form).val(),
                AspectRatio: $('#txtOriginalAspectRatio', form).val(),
                Video3DFormat: $('#select3dFormat', form).val(),

                Language: $('#selectLanguage', form).val(),
                OfficialRating: $('#selectOfficialRating', form).val(),
                CustomRating: $('#selectCustomRating', form).val(),
                People: currentItem.People,
                EnableInternetProviders: $("#enableInternetProviders", form).prop('checked'),
                LockedFields: $('select[name="lockFields"]', form).map(function () {
                    var value = $(this).val();
                    if (value != '') return value;
                }).get(),

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

            var updatePromise;

            if (currentItem.Type == "Artist") {
                updatePromise = ApiClient.updateArtist(item);
            }
            else if (currentItem.Type == "Genre") {
                updatePromise = ApiClient.updateGenre(item);
            }
            else if (currentItem.Type == "MusicGenre") {
                updatePromise = ApiClient.updateMusicGenre(item);
            }
            else if (currentItem.Type == "GameGenre") {
                updatePromise = ApiClient.updateGameGenre(item);
            }
            else if (currentItem.Type == "Person") {
                updatePromise = ApiClient.updatePerson(item);
            }
            else if (currentItem.Type == "Studio") {
                updatePromise = ApiClient.updateStudio(item);
            }
            else {
                updatePromise = ApiClient.updateItem(item);
            }

            updatePromise.done(function () {

                Dashboard.alert('Item saved.');

            });

            return false;
        };

        self.addElementToEditableListview = function (source, sortCallback) {
            var input = $(source).parent().find('input[type="text"], select');
            var text = input.val();
            input.val('');
            if (text == '') return;
            var list = $(source).parents('[data-role="editableListviewContainer"]').find('ul[data-role="listview"]');
            var items = editableListViewValues(list);
            items.push(text);
            populateListView(list, items, sortCallback);
        };

        self.setProviderSettingsContainerVisibility = function (source) {
            if ($(source).prop('checked')) {
                $('#providerSettingsContainer').show();
            } else {
                $('#providerSettingsContainer').hide();
            }
        };

        self.sortDaysOfTheWeek = function (list) {
            var days = new Array("Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday");
            list.sort(function (a, b) { return days.indexOf(a) > days.indexOf(b); });
            return list;
        };

        self.removeElementFromListview = function (source) {
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

            var refreshPromise;

            var force = $('#chkForceRefresh', page).checked();

            if (currentItem.Type == "Artist") {
                refreshPromise = ApiClient.refreshArtist(currentItem.Name, force);
            }
            else if (currentItem.Type == "Genre") {
                refreshPromise = ApiClient.refreshGenre(currentItem.Name, force);
            }
            else if (currentItem.Type == "MusicGenre") {
                refreshPromise = ApiClient.refreshMusicGenre(currentItem.Name, force);
            }
            else if (currentItem.Type == "GameGenre") {
                refreshPromise = ApiClient.refreshGameGenre(currentItem.Name, force);
            }
            else if (currentItem.Type == "Person") {
                refreshPromise = ApiClient.refreshPerson(currentItem.Name, force);
            }
            else if (currentItem.Type == "Studio") {
                refreshPromise = ApiClient.refreshStudio(currentItem.Name, force);
            }
            else {
                refreshPromise = ApiClient.refreshItem(currentItem.Id, force, $('#chkRecursive', page).checked());
            }

            refreshPromise.done(function () {

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