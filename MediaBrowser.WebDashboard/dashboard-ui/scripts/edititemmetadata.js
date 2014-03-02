(function ($, document, window) {

    var currentItem;

    var languagesPromise;
    var countriesPromise;

    function ensureLanguagePromises() {
        languagesPromise = languagesPromise || ApiClient.getCultures();
        countriesPromise = countriesPromise || ApiClient.getCountries();
    }

    function updateTabs(page, item) {

        var query = MetadataEditor.getEditQueryString(item);

        $('#btnEditPeople', page).attr('href', 'edititempeople.html?' + query);
        $('#btnEditImages', page).attr('href', 'edititemimages.html?' + query);
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ensureLanguagePromises();

        var promise1 = MetadataEditor.getItemPromise();
        var promise2 = languagesPromise;
        var promise3 = countriesPromise;

        $.when(promise1, promise2, promise3).done(function (response1, response2, response3) {

            var item = response1[0];

            currentItem = item;

            if (item.Type == "UserRootFolder") {
                $('.editPageInnerContent', page).hide();
                return;
            } else {
                $('.editPageInnerContent', page).show();
            }
            var languages = response2[0];
            var countries = response3[0];

            $.getJSON(ApiClient.getUrl("Items/" + item.Id + "/ExternalIdInfos")).done(function (idList) {
                loadExternalIds(page, item, idList);
            });

            Dashboard.populateLanguages($('#selectLanguage', page), languages);
            Dashboard.populateCountries($('#selectCountry', page), countries);

            if (item.LocationType == "Offline") {
                $('.saveButtonContainer', page).hide();
            } else {
                $('.saveButtonContainer', page).show();
            }

            $('#btnRefresh', page).buttonEnabled(true);
            $('#btnDelete', page).buttonEnabled(true);
            $('.btnSave', page).buttonEnabled(true);

            $('#refreshLoading', page).hide();

            if (item.Type != "Channel" &&
                item.Type != "Genre" &&
                item.Type != "Studio" &&
                item.Type != "MusicGenre" &&
                item.Type != "GameGenre" &&
                item.Type != "Person" &&
                item.Type != "MusicArtist" &&
                item.Type != "CollectionFolder") {
                $('#fldDelete', page).show();
            } else {
                $('#fldDelete', page).hide();
            }

            LibraryBrowser.renderName(item, $('.itemName', page), true);

            updateTabs(page, item);

            setFieldVisibilities(page, item);
            fillItemInfo(page, item);

            if (item.Type == "Person" || item.Type == "Studio" || item.Type == "MusicGenre" || item.Type == "Genre" || item.Type == "MusicArtist" || item.Type == "GameGenre" || item.Type == "Channel") {
                $('#btnEditPeople', page).hide();
            } else {
                $('#btnEditPeople', page).show();
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function onExternalIdChange() {

        var formatString = this.getAttribute('data-formatstring');
        var buttonClass = this.getAttribute('data-buttonclass');

        if (this.value) {
            $('.' + buttonClass).attr('href', formatString.replace('{0}', this.value));
        } else {
            $('.' + buttonClass).attr('href', '#');
        }
    }

    function loadExternalIds(page, item, externalIds) {

        var html = '';

        var providerIds = item.ProviderIds || {};

        for (var i = 0, length = externalIds.length; i < length; i++) {

            var idInfo = externalIds[i];

            var id = "txt1" + idInfo.Key;
            var buttonId = "btnOpen1" + idInfo.Key;
            var formatString = idInfo.UrlFormatString || '';

            html += '<div data-role="fieldcontain">';
            html += '<label for="' + id + '">' + idInfo.Name + ' Id:</label>';

            html += '<div style="display: inline-block; width: 250px;">';

            var value = providerIds[idInfo.Key] || '';

            html += '<input class="txtExternalId" value="' + value + '" data-providerkey="' + idInfo.Key + '" data-formatstring="' + formatString + '" data-buttonclass="' + buttonId + '" id="' + id + '" data-mini="true" />';

            html += '</div>';

            if (formatString) {
                html += '<a class="' + buttonId + '" href="#" target="_blank" data-icon="arrow-r" data-inline="true" data-iconpos="notext" data-role="button" style="float: none; width: 1.75em"></a>';
            }

            html += '</div>';
        }

        var elem = $('.externalIds', page).html(html).trigger('create');

        $('.txtExternalId', elem).on('change', onExternalIdChange).trigger('change');
    }

    function setFieldVisibilities(page, item) {

        if (item.Path) {
            $('#fldPath', page).show();
        } else {
            $('#fldPath', page).hide();
        }

        if (item.Type == "Series") {
            $('#fldSeriesRuntime', page).show();
        } else {
            $('#fldSeriesRuntime', page).hide();
        }

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

        if (item.MediaType == "Game") {
            $('#fldPlayers', page).show();
        } else {
            $('#fldPlayers', page).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "MusicVideo" || item.Type == "Series" || item.Type == "Game") {
            $('#fldCriticRating', page).show();
            $('#fldCriticRatingSummary', page).show();
        } else {
            $('#fldCriticRating', page).hide();
            $('#fldCriticRatingSummary', page).hide();
        }

        if (item.Type == "Movie") {
            $('#fldAwardSummary', page).show();
        } else {
            $('#fldAwardSummary', page).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer") {
            $('#fldMetascore', page).show();
        } else {
            $('#fldMetascore', page).hide();
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

        if (item.MediaType == "Video" && item.Type != "Channel") {
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

        if (item.Type == "Episode") {
            $('#collapsibleDvdEpisodeInfo', page).show();
        } else {
            $('#collapsibleDvdEpisodeInfo', page).hide();
        }

        if (item.Type == "Episode" && item.ParentIndexNumber == 0) {
            $('#collapsibleSpecialEpisodeInfo', page).show();
        } else {
            $('#collapsibleSpecialEpisodeInfo', page).hide();
        }

        if (item.Type == "Person" || item.Type == "Genre" || item.Type == "Studio" || item.Type == "GameGenre" || item.Type == "MusicGenre" || item.Type == "Channel") {
            $('#fldCommunityRating', page).hide();
            $('#fldCommunityVoteCount', page).hide();
            $('#genresCollapsible', page).hide();
            $('#studiosCollapsible', page).hide();

            if (item.Type == "Channel") {
                $('#fldOfficialRating', page).show();
            } else {
                $('#fldOfficialRating', page).hide();
            }
            $('#fldCustomRating', page).hide();
        } else {
            $('#fldCommunityRating', page).show();
            $('#fldCommunityVoteCount', page).show();
            $('#genresCollapsible', page).show();
            $('#studiosCollapsible', page).show();
            $('#fldOfficialRating', page).show();
            $('#fldCustomRating', page).show();
        }

        if (item.Type == "Channel") {
            $('#tagsCollapsible', page).hide();
            $('#metadataSettingsCollapsible', page).hide();
            $('#fldPremiereDate', page).hide();
            $('#fldSortName', page).hide();
            $('#fldDateAdded', page).hide();
            $('#fldYear', page).hide();
        } else {
            $('#tagsCollapsible', page).show();
            $('#metadataSettingsCollapsible', page).show();
            $('#fldPremiereDate', page).show();
            $('#fldSortName', page).show();
            $('#fldDateAdded', page).show();
            $('#fldYear', page).show();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "AdultVideo" || item.Type == "Series" || item.Type == "Game" || item.Type == "BoxSet" || item.Type == "Person" || item.Type == "Book") {
            $('#btnIdentify', page).show();
        } else {
            $('#btnIdentify', page).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            $('#keywordsCollapsible', page).show();
        } else {
            $('#keywordsCollapsible', page).hide();
        }

        if (item.MediaType == "Video" && item.Type != "Channel") {
            $('#fldSourceType', page).show();
        } else {
            $('#fldSourceType', page).hide();
        }

        if (item.Type == "Person") {
            $('#lblPremiereDate', page).html('Date of birth');
            $('#lblYear', page).html('Birth year');
            $('#lblEndDate', page).html('Death date');
            $('#fldPlaceOfBirth', page).show();
        } else {
            $('#lblPremiereDate', page).html('Release date');
            $('#lblYear', page).html('Year');
            $('#lblEndDate', page).html('End date');
            $('#fldPlaceOfBirth', page).hide();
        }

        if (item.MediaType == "Video" && item.Type != "Channel") {
            $('#fldOriginalAspectRatio', page).show();
        } else {
            $('#fldOriginalAspectRatio', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode" || item.Type == "Season") {
            $('#fldIndexNumber', page).show();

            if (item.Type == "Episode") {
                $('#lblIndexNumber', page).html('Episode number');
            } else if (item.Type == "Season") {
                $('#lblIndexNumber', page).html('Season number');
            } else if (item.Type == "Audio") {
                $('#lblIndexNumber', page).html('Track number');
            } else {
                $('#lblIndexNumber', page).html('Number');
            }
        } else {
            $('#fldIndexNumber', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode") {
            $('#fldParentIndexNumber', page).show();

            if (item.Type == "Episode") {
                $('#lblParentIndexNumber', page).html('Season number');
            } else if (item.Type == "Audio") {
                $('#lblParentIndexNumber', page).html('Disc number');
            } else {
                $('#lblParentIndexNumber', page).html('Parent number');
            }
        } else {
            $('#fldParentIndexNumber', page).hide();
        }

        if (item.Type == "Series") {
            $('#fldDisplaySpecialsInline', page).show();
        } else {
            $('#fldDisplaySpecialsInline', page).hide();
        }

        if (item.Type == "BoxSet") {
            $('#fldDisplayOrder', page).show();

            $('#labelDisplayOrder', page).html('Title display order:');
            $('#selectDisplayOrder', page).html('<option value="SortName">Sort Name</option><option value="PremiereDate">Release Date</option>').selectmenu('refresh');
        } else {
            $('#selectDisplayOrder', page).html('').selectmenu('refresh');
            $('#fldDisplayOrder', page).hide();
        }

        var displaySettingFields = $('.fldDisplaySetting', page);
        if (displaySettingFields.filter(function (index) {

            return displaySettingFields[index].style.display != 'none';

        }).length) {
            $('#collapsibleDisplaySettings', page).show();
        } else {
            $('#collapsibleDisplaySettings', page).hide();
        }
    }

    function fillItemInfo(page, item) {

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

        $('.chkAirDay', page).each(function () {

            this.checked = (item.AirDays || []).indexOf(this.getAttribute('data-day')) != -1;

        }).checkboxradio('refresh');

        populateListView($('#listGenres', page), item.Genres);

        populateListView($('#listStudios', page), (item.Studios || []).map(function (element) { return element.Name || ''; }));

        populateListView($('#listTags', page), item.Tags);
        populateListView($('#listKeywords', page), item.Keywords);

        var lockData = (item.LockData || false);
        var chkLockData = $("#chkLockData", page).attr('checked', lockData).checkboxradio('refresh');
        if (chkLockData.checked()) {
            $('#providerSettingsContainer', page).hide();
        } else {
            $('#providerSettingsContainer', page).show();
        }
        populateInternetProviderSettings(page, item, item.LockedFields);

        $("#chkDisplaySpecialsInline", page).checked(item.DisplaySpecialsWithSeasons || false).checkboxradio('refresh');

        $('#txtPath', page).val(item.Path || '');
        $('#txtName', page).val(item.Name || "");
        $('#txtOverview', page).val(item.Overview || "");
        $('#txtSortName', page).val(item.SortName || "");
        $('#txtDisplayMediaType', page).val(item.DisplayMediaType || "");
        $('#txtCommunityRating', page).val(item.CommunityRating || "");
        $('#txtCommunityVoteCount', page).val(item.VoteCount || "");
        $('#txtHomePageUrl', page).val(item.HomePageUrl || "");

        $('#txtAwardSummary', page).val(item.AwardSummary || "");
        $('#txtMetascore', page).val(item.Metascore || "");

        $('#txtBudget', page).val(item.Budget || "");
        $('#txtRevenue', page).val(item.Revenue || "");

        $('#txtCriticRating', page).val(item.CriticRating || "");
        $('#txtCriticRatingSummary', page).val(item.CriticRatingSummary || "");

        $('#txtIndexNumber', page).val(('IndexNumber' in item) ? item.IndexNumber : "");
        $('#txtParentIndexNumber', page).val(('ParentIndexNumber' in item) ? item.ParentIndexNumber : "");
        $('#txtPlayers', page).val(item.Players || "");

        $('#txtAbsoluteEpisodeNumber', page).val(('AbsoluteEpisodeNumber' in item) ? item.AbsoluteEpisodeNumber : "");
        $('#txtDvdEpisodeNumber', page).val(('DvdEpisodeNumber' in item) ? item.DvdEpisodeNumber : "");
        $('#txtDvdSeasonNumber', page).val(('DvdSeasonNumber' in item) ? item.DvdSeasonNumber : "");
        $('#txtAirsBeforeSeason', page).val(('AirsBeforeSeasonNumber' in item) ? item.AirsBeforeSeasonNumber : "");
        $('#txtAirsAfterSeason', page).val(('AirsAfterSeasonNumber' in item) ? item.AirsAfterSeasonNumber : "");
        $('#txtAirsBeforeEpisode', page).val(('AirsBeforeEpisodeNumber' in item) ? item.AirsBeforeEpisodeNumber : "");

        $('#txtAlbum', page).val(item.Album || "");
        $('#txtAlbumArtist', page).val(item.AlbumArtist || "");

        $('#selectDisplayOrder', page).val(item.DisplayOrder).selectmenu('refresh');

        var artists = item.Artists || [];
        $('#txtArtist', page).val(artists.join(';'));

        var date;

        if (item.DateCreated) {
            try {
                date = parseISO8601Date(item.DateCreated, { toLocal: true });

                $('#txtDateAdded', page).val(date.toISOString().slice(0, 10));
            } catch (e) {
                $('#txtDateAdded', page).val('');
            }
        } else {
            $('#txtDateAdded', page).val('');
        }

        if (item.PremiereDate) {
            try {
                date = parseISO8601Date(item.PremiereDate, { toLocal: true });

                $('#txtPremiereDate', page).val(date.toISOString().slice(0, 10));
            } catch (e) {
                $('#txtPremiereDate', page).val('');
            }
        } else {
            $('#txtPremiereDate', page).val('');
        }

        if (item.EndDate) {
            try {
                date = parseISO8601Date(item.EndDate, { toLocal: true });

                $('#txtEndDate', page).val(date.toISOString().slice(0, 10));
            } catch (e) {
                $('#txtEndDate', page).val('');
            }
        } else {
            $('#txtEndDate', page).val('');
        }

        $('#txtProductionYear', page).val(item.ProductionYear || "");
        $('#txtAirTime', page).val(convertTo24HourFormat(item.AirTime || ""));

        var placeofBirth = item.ProductionLocations && item.ProductionLocations.length ? item.ProductionLocations[0] : '';
        $('#txtPlaceOfBirth', page).val(placeofBirth);

        $('#txtOriginalAspectRatio', page).val(item.AspectRatio || "");

        $('#selectLanguage', page).val(item.PreferredMetadataLanguage || "").selectmenu('refresh');
        $('#selectCountry', page).val(item.PreferredMetadataCountryCode || "").selectmenu('refresh');

        if (item.RunTimeTicks) {

            var minutes = item.RunTimeTicks / 600000000;

            $('#txtSeriesRuntime', page).val(Math.round(minutes));
        } else {
            $('#txtSeriesRuntime', page).val("");
        }
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
        items = items || [];
        if (typeof (sortCallback) === 'undefined') {
            items.sort(function (a, b) { return a.toLowerCase().localeCompare(b.toLowerCase()); });
        } else {
            items = sortCallback(items);
        }
        var html = '';
        for (var i = 0; i < items.length; i++) {
            html += '<li data-mini="true"><a class="data">' + items[i] + '</a><a href="#" onclick="EditItemMetadataPage.removeElementFromListview(this)" class="btnRemoveFromEditorList"></a></li>';
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
            var name = field.name;
            var value = field.value || field.name;
            var fieldTitle = $.trim(name.replace(/([A-Z])/g, ' $1'));
            html += '<div data-role="fieldcontain">';
            html += '<label for="lock' + value + '">' + fieldTitle + ':</label>';
            html += '<select class="selectLockedField" id="lock' + value + '" data-role="slider" data-mini="true">';
            html += '<option value="' + value + '">Off</option>';
            html += '<option value="" selected="selected">On</option>';
            html += '</select>';
            html += '</div>';
        }
        return html;
    }

    function populateInternetProviderSettings(page, item, lockedFields) {
        var container = $('#providerSettingsContainer', page);
        lockedFields = lockedFields || new Array();

        var metadatafields = [
            { name: "Name" },
            { name: "Overview" },
            { name: "Genres" },
            { name: "Parental Rating", value: "OfficialRating" },
            { name: "People", value: "Cast" }
        ];

        if (item.Type == "Person") {
            metadatafields.push({ name: "Birth location", value: "ProductionLocations" });
        } else {
            metadatafields.push({ name: "Production Locations", value: "ProductionLocations" });
        }

        if (item.Type == "Series") {
            metadatafields.push({ name: "Runtime" });
        }

        metadatafields.push({ name: "Studios" });
        metadatafields.push({ name: "Tags" });
        metadatafields.push({ name: "Keywords" });
        metadatafields.push({ name: "Images" });
        metadatafields.push({ name: "Backdrops" });

        if (item.Type == "Game") {
            metadatafields.push({ name: "Screenshots" });
        }

        var html = '';

        html += "<h1>Fields</h1>";
        html += "<p>Slide a field to 'off' to lock it and prevent it's data from being changed.</p>";
        html += generateSliders(metadatafields, 'Fields');
        container.html(html).trigger('create');
        for (var fieldIndex = 0; fieldIndex < lockedFields.length; fieldIndex++) {
            var field = lockedFields[fieldIndex];
            $('#lock' + field).val(field).slider('refresh');
        }
    }

    function getSelectedAirDays(form) {
        return $('.chkAirDay:checked', form).map(function () {
            return this.getAttribute('data-day');
        }).get();
    }

    function performDelete(page) {

        $('#btnDelete', page).buttonEnabled(false);
        $('#btnRefresh', page).buttonEnabled(false);
        $('.btnSave', page).buttonEnabled(false);

        $('#refreshLoading', page).show();

        var parentId = currentItem.ParentId;

        ApiClient.deleteItem(currentItem.Id).done(function () {

            var elem = $('#' + parentId)[0];

            $('.libraryTree').jstree("select_node", elem, true)
                .jstree("delete_node", '#' + currentItem.Id);
        });
    }

    function editItemMetadataPage() {

        var self = this;

        self.onSubmit = function () {

            var form = this;

            var item = {
                Id: currentItem.Id,
                Name: $('#txtName', form).val(),
                SortName: $('#txtSortName', form).val(),
                DisplayMediaType: $('#txtDisplayMediaType', form).val(),
                CommunityRating: $('#txtCommunityRating', form).val(),
                VoteCount: $('#txtCommunityVoteCount', form).val(),
                HomePageUrl: $('#txtHomePageUrl', form).val(),
                Budget: $('#txtBudget', form).val(),
                Revenue: $('#txtRevenue', form).val(),
                CriticRating: $('#txtCriticRating', form).val(),
                CriticRatingSummary: $('#txtCriticRatingSummary', form).val(),
                IndexNumber: $('#txtIndexNumber', form).val(),
                DisplaySpecialsWithSeasons: $('#chkDisplaySpecialsInline', form).checked(),
                AbsoluteEpisodeNumber: $('#txtAbsoluteEpisodeNumber', form).val(),
                DvdEpisodeNumber: $('#txtDvdEpisodeNumber', form).val(),
                DvdSeasonNumber: $('#txtDvdSeasonNumber', form).val(),
                AirsBeforeSeasonNumber: $('#txtAirsBeforeSeason', form).val(),
                AirsAfterSeasonNumber: $('#txtAirsAfterSeason', form).val(),
                AirsBeforeEpisodeNumber: $('#txtAirsBeforeEpisode', form).val(),
                ParentIndexNumber: $('#txtParentIndexNumber', form).val(),
                DisplayOrder: $('#selectDisplayOrder', form).val(),
                Players: $('#txtPlayers', form).val(),
                Album: $('#txtAlbum', form).val(),
                AlbumArtist: $('#txtAlbumArtist', form).val(),
                Artists: $('#txtArtist', form).val().split(';'),
                Metascore: $('#txtMetascore', form).val(),
                AwardSummary: $('#txtAwardSummary', form).val(),
                Overview: $('#txtOverview', form).val(),
                Status: $('#selectStatus', form).val(),
                AirDays: getSelectedAirDays(form),
                AirTime: convertTo12HourFormat($('#txtAirTime', form).val()),
                Genres: editableListViewValues($("#listGenres", form)),
                Tags: editableListViewValues($("#listTags", form)),
                Keywords: editableListViewValues($("#listKeywords", form)),
                Studios: editableListViewValues($("#listStudios", form)).map(function (element) { return { Name: element }; }),

                PremiereDate: $('#txtPremiereDate', form).val() || null,
                DateCreated: $('#txtDateAdded', form).val() || null,
                EndDate: $('#txtEndDate', form).val() || null,
                ProductionYear: $('#txtProductionYear', form).val(),
                AspectRatio: $('#txtOriginalAspectRatio', form).val(),
                Video3DFormat: $('#select3dFormat', form).val(),

                OfficialRating: $('#selectOfficialRating', form).val(),
                CustomRating: $('#selectCustomRating', form).val(),
                People: currentItem.People,
                LockData: $("#chkLockData", form).prop('checked'),
                LockedFields: $('.selectLockedField', form).map(function () {
                    var value = $(this).val();
                    if (value != '') return value;
                }).get()
            };

            item.ProviderIds = $.extend({}, currentItem.ProviderIds || {});

            $('.txtExternalId', form).each(function () {

                var providerkey = this.getAttribute('data-providerkey');

                item.ProviderIds[providerkey] = this.value;
            });

            item.PreferredMetadataLanguage = $('#selectLanguage', form).val();
            item.PreferredMetadataCountryCode = $('#selectCountry', form).val();

            if (currentItem.Type == "Person") {

                var placeOfBirth = $('#txtPlaceOfBirth', form).val();

                item.ProductionLocations = placeOfBirth ? [placeOfBirth] : [];
            }

            if (currentItem.Type == "Series") {

                // 600000000
                var seriesRuntime = $('#txtSeriesRuntime', form).val();
                item.RunTimeTicks = seriesRuntime ? (seriesRuntime * 600000000) : null;
            }

            var updatePromise;

            if (currentItem.Type == "MusicArtist") {
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
            if (!$(source).prop('checked')) {
                $('#providerSettingsContainer').show();
            } else {
                $('#providerSettingsContainer').hide();
            }
        };

        self.removeElementFromListview = function (source) {
            var list = $(source).parents('ul[data-role="listview"]');
            $(source).parent().remove();
            list.listview('refresh');
        };

        self.onDeleteFormSubmitted = function () {

            var page = $(this).parents('.page');

            if ($('#fldChallengeValue', page).val() != $('#txtDeleteTest', page).val()) {
                Dashboard.alert('The value entered is not correct. Please try again.');
            } else {
                performDelete(page);
            }

            return false;
        };

        self.onIdentificationFormSubmitted = function () {

            var page = $(this).parents('.page');

            searchForIdentificationResults(page);
            return false;
        };
    }

    window.EditItemMetadataPage = new editItemMetadataPage();

    function showIdentificationForm(page) {

        var item = currentItem;

        $.getJSON(ApiClient.getUrl("Items/" + item.Id + "/ExternalIdInfos")).done(function (idList) {

            var html = '';

            var providerIds = item.ProviderIds || {};

            for (var i = 0, length = idList.length; i < length; i++) {

                var idInfo = idList[i];

                var id = "txtLookup" + idInfo.Key;

                html += '<div data-role="fieldcontain">';
                html += '<label for="' + id + '">' + idInfo.Name + ' Id:</label>';

                var value = providerIds[idInfo.Key] || '';

                html += '<input class="txtLookupId" value="' + value + '" data-providerkey="' + idInfo.Key + '" id="' + id + '" data-mini="true" />';

                html += '</div>';
            }

            $('#txtLookupName', page).val(item.Name);

            if (item.Type == "Person" || item.Type == "BoxSet") {

                $('.fldLookupYear', page).hide();
                $('#txtLookupYear', page).val('');
            } else {

                $('.fldLookupYear', page).show();
                $('#txtLookupYear', page).val(item.ProductionYear);
            }

            $('.identifyProviderIds', page).html(html).trigger('create');

            var friendlyName = item.Type == "BoxSet" ? "Collection" : item.Type;

            $('.identificationHeader', page).html('Identify ' + friendlyName);

            $('.popupIdentifyForm', page).show();
            $('.identificationSearchResults', page).hide();
            $('.btnSearchAgain', page).hide();

            $('.popupIdentify', page).popup('open');
        });
    }

    function searchForIdentificationResults(page) {

        var lookupInfo = {
            ProviderIds: {}
        };

        $('.identifyField', page).each(function () {

            var value = this.value;

            if (value) {

                if (this.type == 'number') {
                    value = parseInt(value);
                }

                lookupInfo[this.getAttribute('data-lookup')] = value;
            }

        });

        var hasId = false;

        $('.txtLookupId', page).each(function () {

            var value = this.value;

            if (value) {
                hasId = true;
            }
            lookupInfo.ProviderIds[this.getAttribute('data-providerkey')] = value;

        });

        if (!hasId && !lookupInfo.Name) {
            Dashboard.alert('Please enter a name or an external Id.');
            return;
        }
        
        if (currentItem.GameSystem) {
            lookupInfo.GameSystem = currentItem.GameSystem;
        }

        lookupInfo = {
            SearchInfo: lookupInfo,
            IncludeDisabledProviders: true
        };

        Dashboard.showLoadingMsg();

        $.ajax({
            type: "POST",
            url: ApiClient.getUrl("Items/RemoteSearch/" + currentItem.Type),
            data: JSON.stringify(lookupInfo),
            contentType: "application/json"

        }).done(function (results) {

            Dashboard.hideLoadingMsg();
            showIdentificationSearchResults(page, results);
        });
    }

    function getSearchImageDisplayUrl(url, provider) {
        return ApiClient.getUrl("Items/RemoteSearch/Image", { imageUrl: url, ProviderName: provider });
    }

    function showIdentificationSearchResults(page, results) {

        $('.popupIdentifyForm', page).hide();
        $('.identificationSearchResults', page).show();
        $('.btnSearchAgain', page).show();

        var html = '';

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];

            var cssClass = "searchImageContainer remoteImageContainer";

            if (currentItem.Type == "Episode") {
                cssClass += " searchBackdropImageContainer";
            }
            else if (currentItem.Type == "MusicAlbum" || currentItem.Type == "MusicArtist") {
                cssClass += " searchDiscImageContainer";
            }
            else {
                cssClass += " searchPosterImageContainer";
            }

            html += '<div class="' + cssClass + '">';

            if (result.ImageUrl) {
                var displayUrl = getSearchImageDisplayUrl(result.ImageUrl, result.SearchProviderName);

                html += '<a href="#" class="searchImage" data-index="' + i + '" style="background-image:url(\'' + displayUrl + '\');">';
            } else {

                html += '<a href="#" class="searchImage" data-index="' + i + '" style="background-image:url(\'css/images/items/list/remotesearch.png\');background-position: center center;">';
            }
            html += '</a>';

            html += '<div class="remoteImageDetails">';
            html += result.Name;
            html += '</div>';

            html += '<div class="remoteImageDetails">';
            html += result.ProductionYear || '&nbsp;';
            html += '</div>';

            html += '</div>';
        }

        var elem = $('.identificationSearchResultList', page).html(html).trigger('create');

        $('.searchImage', elem).on('click', function () {

            Dashboard.showLoadingMsg();
            
            var index = parseInt(this.getAttribute('data-index'));

            var currentResult = results[index];

            $.ajax({
                type: "POST",
                url: ApiClient.getUrl("Items/RemoteSearch/Apply/" + currentItem.Id),
                data: JSON.stringify(currentResult),
                contentType: "application/json"

            }).done(function () {

                Dashboard.hideLoadingMsg();

                $('.popupIdentify', page).popup('close');

                reload(page);
            });
        });
    }

    $(document).on('pageinit', "#editItemMetadataPage", function () {

        var page = this;

        $('#btnRefresh', this).on('click', function () {

            $('#btnDelete', page).buttonEnabled(false);
            $('#btnRefresh', page).buttonEnabled(false);
            $('.btnSave', page).buttonEnabled(false);

            $('#refreshLoading', page).show();

            var refreshPromise;

            var force = $('#selectRefreshMode', page).val() == 'all';

            if (currentItem.Type == "MusicArtist") {
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
                refreshPromise = ApiClient.refreshItem(currentItem.Id, force, true);
            }

            refreshPromise.done(function () {

                reload(page);

            });
        });

        $('#btnIdentify', page).on('click', function () {

            showIdentificationForm(page);
        });

        $('.btnSearchAgain', page).on('click', function () {

            $('.popupIdentifyForm', page).show();
            $('.identificationSearchResults', page).hide();
            $('.btnSearchAgain', page).hide();

        });

        function getRandomInt(min, max) {
            return Math.floor(Math.random() * (max - min + 1) + min);
        }

        $('#btnDelete', this).on('click', function () {

            if (currentItem.LocationType != "Remote" && currentItem.LocationType != "Virtual") {
                $('.deletePath', page).html((currentItem.Path || ''));

                var val1 = getRandomInt(6, 12);
                var val2 = getRandomInt(8, 16);

                $('#challengeValueText', page).html(val1 + ' * ' + val2 + ':');

                var val = val1 * val2;

                $('#fldChallengeValue', page).val(val);

                $('#popupConfirmDelete', page).popup('open');

            } else {

                var msg = "<p>Are you sure you wish to delete this item from your library?</p>";

                Dashboard.confirm(msg, "Confirm Deletion", function (result) {

                    if (result) {

                        performDelete(page);
                    }

                });
            }
        });

        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.itemType == "libraryreport") {
                Dashboard.navigate('libraryreport.html');
                return;
            }

            if (data.itemType == "livetvservice") {
                return;
            }

            if (data.id != currentItem.Id) {

                MetadataEditor.currentItemId = data.id;
                MetadataEditor.currentItemName = data.itemName;
                MetadataEditor.currentItemType = data.itemType;
                //Dashboard.navigate('edititemmetadata.html?id=' + data.id);

                //$.mobile.urlHistory.ignoreNextHashChange = true;
                window.location.hash = 'editItemMetadataPage?id=' + data.id;

                reload(page);
            }
        });

    }).on('pagebeforeshow', "#editItemMetadataPage", function () {

        var page = this;

        reload(page);

    });

})(jQuery, document, window);

