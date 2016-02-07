(function ($, document, window) {

    var currentItem;
    var currentSearchResult;
    var metadataEditorInfo;

    function reload(page) {

        page = $(page)[0];

        unbindItemChanged(page);
        Dashboard.showLoadingMsg();

        var promise1 = MetadataEditor.getItemPromise();
        var promise2 = MetadataEditor.getCurrentItemId() ?
            ApiClient.getJSON(ApiClient.getUrl('Items/' + MetadataEditor.getCurrentItemId() + '/MetadataEditor')) :
            {};

        Promise.all([promise1, promise2]).then(function (responses) {

            var item = responses[0];
            metadataEditorInfo = responses[1];

            currentItem = item;

            if (!LibraryBrowser.supportsEditing(item.Type)) {
                $('.editPageInnerContent', page)[0].style.visibility = 'hidden';
                Dashboard.hideLoadingMsg();
                return;
            } else {
                $('.editPageInnerContent', page)[0].style.visibility = 'visible';
            }

            var languages = metadataEditorInfo.Cultures;
            var countries = metadataEditorInfo.Countries;

            renderContentTypeOptions(page, metadataEditorInfo);

            loadExternalIds(page, item, metadataEditorInfo.ExternalIdInfos);

            populateLanguages(page.querySelector('#selectLanguage'), languages);
            populateCountries(page.querySelector('#selectCountry'), countries);

            LibraryBrowser.renderName(item, $('.itemName', page), true);

            setFieldVisibilities(page, item);
            fillItemInfo(page, item, metadataEditorInfo.ParentalRatingOptions);

            if (item.MediaType == 'Photo') {
                $('#btnEditImages', page).hide();
            } else {
                $('#btnEditImages', page).show();
            }

            if (item.MediaType == "Video" && item.Type != "Episode") {
                $('#fldShortOverview', page).show();
            } else {
                $('#fldShortOverview', page).hide();
            }

            if (item.MediaType == "Video" && item.Type != "Episode") {
                $('#fldTagline', page).show();
            } else {
                $('#fldTagline', page).hide();
            }

            Dashboard.hideLoadingMsg();
            bindItemChanged(page);
        });
    }

    function populateCountries(select, allCountries) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = allCountries.length; i < length; i++) {

            var culture = allCountries[i];

            html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>";
        }

        select.innerHTML = html;
    }

    function populateLanguages(select, languages) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        select.innerHTML = html;
    }

    function renderContentTypeOptions(page, metadataInfo) {

        if (metadataInfo.ContentTypeOptions.length) {
            $('#fldContentType', page).show();
        } else {
            $('#fldContentType', page).hide();
        }

        var html = metadataInfo.ContentTypeOptions.map(function (i) {


            return '<option value="' + i.Value + '">' + i.Name + '</option>';

        }).join('');

        $('#selectContentType', page).html(html).val(metadataInfo.ContentType || '');
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

            var labelText = Globalize.translate('LabelDynamicExternalId').replace('{0}', idInfo.Name);

            html += '<div>';

            var value = providerIds[idInfo.Key] || '';

            html += '<paper-input style="display:inline-block;width:80%;" class="txtExternalId" value="' + value + '" data-providerkey="' + idInfo.Key + '" data-formatstring="' + formatString + '" data-buttonclass="' + buttonId + '" id="' + id + '" label="' + labelText + '"></paper-input>';

            if (formatString) {
                html += '<a class="clearLink ' + buttonId + '" href="#" target="_blank" data-role="none" style="float: none; width: 1.75em"><paper-icon-button icon="open-in-browser"></paper-icon-button></a>';
            }

            html += '</div>';
        }

        var elem = $('.externalIds', page).html(html).trigger('create');

        $('.txtExternalId', elem).on('change', onExternalIdChange).trigger('change');
    }

    function setFieldVisibilities(page, item) {

        if (item.Path && item.LocationType != 'Remote') {
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

        if (item.Type == "Movie" || item.Type == "Trailer") {
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

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
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

        if (item.Type == "Person" || item.Type == "Genre" || item.Type == "Studio" || item.Type == "GameGenre" || item.Type == "MusicGenre" || item.Type == "TvChannel") {
            $('#fldCommunityRating', page).hide();
            $('#fldCommunityVoteCount', page).hide();
            $('#genresCollapsible', page).hide();
            $('#peopleCollapsible', page).hide();
            $('#studiosCollapsible', page).hide();

            if (item.Type == "TvChannel") {
                $('#fldOfficialRating', page).show();
            } else {
                $('#fldOfficialRating', page).hide();
            }
            $('#fldCustomRating', page).hide();
        } else {
            $('#fldCommunityRating', page).show();
            $('#fldCommunityVoteCount', page).show();
            $('#genresCollapsible', page).show();
            $('#peopleCollapsible', page).show();
            $('#studiosCollapsible', page).show();
            $('#fldOfficialRating', page).show();
            $('#fldCustomRating', page).show();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "MusicArtist") {
            $('#countriesCollapsible', page).show();
        } else {
            $('#countriesCollapsible', page).hide();
        }

        if (item.Type == "TvChannel") {
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

        Dashboard.getCurrentUser().then(function (user) {

            if (LibraryBrowser.getMoreCommands(item, user).indexOf('identify') != -1) {

                $('#btnIdentify', page).show();
            } else {
                $('#btnIdentify', page).hide();
            }
        });

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            $('#keywordsCollapsible', page).show();
        } else {
            $('#keywordsCollapsible', page).hide();
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            $('#fldSourceType', page).show();
        } else {
            $('#fldSourceType', page).hide();
        }

        if (item.Type == "Person") {
            page.querySelector('#txtProductionYear').label = Globalize.translate('LabelBirthYear');
            page.querySelector("label[for='txtPremiereDate']").innerHTML = Globalize.translate('LabelBirthDate');
            page.querySelector("label[for='txtEndDate']").innerHTML = Globalize.translate('LabelDeathDate');
            $('#fldPlaceOfBirth', page).show();
        } else {
            page.querySelector('#txtProductionYear').label = Globalize.translate('LabelYear');
            page.querySelector("label[for='txtPremiereDate']").innerHTML = Globalize.translate('LabelReleaseDate');
            page.querySelector("label[for='txtEndDate']").innerHTML = Globalize.translate('LabelEndDate');
            $('#fldPlaceOfBirth', page).hide();
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            $('#fldOriginalAspectRatio', page).show();
        } else {
            $('#fldOriginalAspectRatio', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode" || item.Type == "Season") {
            $('#fldIndexNumber', page).show();

            if (item.Type == "Episode") {
                page.querySelector('#txtIndexNumber').label = Globalize.translate('LabelEpisodeNumber');
            } else if (item.Type == "Season") {
                page.querySelector('#txtIndexNumber').label = Globalize.translate('LabelSeasonNumber');
            } else if (item.Type == "Audio") {
                page.querySelector('#txtIndexNumber').label = Globalize.translate('LabelTrackNumber');
            } else {
                page.querySelector('#txtIndexNumber').label = Globalize.translate('LabelNumber');
            }
        } else {
            $('#fldIndexNumber', page).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode") {
            $('#fldParentIndexNumber', page).show();

            if (item.Type == "Episode") {
                page.querySelector('#txtParentIndexNumber').label = Globalize.translate('LabelSeasonNumber');
            } else if (item.Type == "Audio") {
                page.querySelector('#txtParentIndexNumber').label = Globalize.translate('LabelDiscNumber');
            } else {
                page.querySelector('#txtParentIndexNumber').label = Globalize.translate('LabelParentNumber');
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

            $('#labelDisplayOrder', page).html(Globalize.translate('LabelTitleDisplayOrder'));
            $('#selectDisplayOrder', page).html('<option value="SortName">' + Globalize.translate('OptionSortName') + '</option><option value="PremiereDate">' + Globalize.translate('OptionReleaseDate') + '</option>');
        } else {
            $('#selectDisplayOrder', page).html('');
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

    function fillItemInfo(page, item, parentalRatingOptions) {

        var select = $('#selectOfficialRating', page);

        populateRatings(parentalRatingOptions, select, item.OfficialRating);

        select.val(item.OfficialRating || "");

        select = $('#selectCustomRating', page);

        populateRatings(parentalRatingOptions, select, item.CustomRating);

        select.val(item.CustomRating || "");

        var selectStatus = $('#selectStatus', page);
        populateStatus(selectStatus);
        selectStatus.val(item.Status || "");

        $('#select3dFormat', page).val(item.Video3DFormat || "");

        $('.chkAirDay', page).each(function () {

            this.checked = (item.AirDays || []).indexOf(this.getAttribute('data-day')) != -1;

        });

        populateListView($('#listCountries', page), item.ProductionLocations || []);
        populateListView($('#listGenres', page), item.Genres);
        populatePeople(page, item.People || []);

        populateListView($('#listStudios', page), (item.Studios || []).map(function (element) { return element.Name || ''; }));

        populateListView($('#listTags', page), item.Tags);
        populateListView($('#listKeywords', page), item.Keywords);

        var lockData = (item.LockData || false);
        var chkLockData = page.querySelector("#chkLockData");
        chkLockData.checked = lockData;
        if (chkLockData.checked) {
            $('#providerSettingsContainer', page).hide();
        } else {
            $('#providerSettingsContainer', page).show();
        }
        populateInternetProviderSettings(page, item, item.LockedFields);

        page.querySelector('#chkDisplaySpecialsInline').checked = item.DisplaySpecialsWithSeasons || false;

        $('#txtPath', page).val(item.Path || '');
        $('#txtName', page).val(item.Name || "");
        page.querySelector('#txtOverview').value = item.Overview || '';
        $('#txtShortOverview', page).val(item.ShortOverview || "");
        $('#txtTagline', page).val((item.Taglines && item.Taglines.length ? item.Taglines[0] : ''));
        $('#txtSortName', page).val(item.ForcedSortName || "");
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

        $('#txtAlbumArtist', page).val((item.AlbumArtists || []).map(function (a) {

            return a.Name;

        }).join(';'));

        $('#selectDisplayOrder', page).val(item.DisplayOrder);

        $('#txtArtist', page).val((item.ArtistItems || []).map(function (a) {

            return a.Name;

        }).join(';'));

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

        $('#txtAirTime', page).val(item.AirTime || '');

        var placeofBirth = item.ProductionLocations && item.ProductionLocations.length ? item.ProductionLocations[0] : '';
        $('#txtPlaceOfBirth', page).val(placeofBirth);

        $('#txtOriginalAspectRatio', page).val(item.AspectRatio || "");

        $('#selectLanguage', page).val(item.PreferredMetadataLanguage || "");
        $('#selectCountry', page).val(item.PreferredMetadataCountryCode || "");

        if (item.RunTimeTicks) {

            var minutes = item.RunTimeTicks / 600000000;

            $('#txtSeriesRuntime', page).val(Math.round(minutes));
        } else {
            $('#txtSeriesRuntime', page).val("");
        }
    }

    function populatePeople(page, people) {

        var lastType = '';
        var html = '';

        var elem = $('#peopleList', page);

        for (var i = 0, length = people.length; i < length; i++) {

            var person = people[i];

            var type = person.Type || Globalize.translate('PersonTypePerson');

            if (type != lastType) {
                html += '<li data-role="list-divider">' + type + '</li>';
                lastType = type;
            }

            html += '<li><a class="btnEditPerson" href="#" data-index="' + i + '">';

            html += '<h3>' + (person.Name || '') + '</h3>';

            if (person.Role && person.Role != lastType) {
                html += '<p>' + (person.Role) + '</p>';
            }
            html += '</a>';

            html += '<a class="btnDeletePerson" href="#" data-icon="delete" data-index="' + i + '">' + Globalize.translate('Delete') + '</a>';

            html += '</li>';
        }

        elem.html(html);

        $('.btnDeletePerson', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));
            currentItem.People.splice(index, 1);

            populatePeople(page, currentItem.People);
        });

        $('.btnEditPerson', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));

            editPerson(page, currentItem.People[index], index);
        });
    }

    function editPerson(page, person, index) {

        $('#popupEditPerson', page).popup("open");

        $('#txtPersonName', page).val(person.Name || '');
        $('#selectPersonType', page).val(person.Type || '');
        $('#txtPersonRole', page).val(person.Role || '');

        if (index == null) {
            index = '';
        }

        $("#fldPersonIndex", page).val(index);
    }

    function savePersonInfo(page) {

        $('#popupEditPerson', page).popup("close");

        var index = $("#fldPersonIndex", page).val();
        var person;

        var isNew = true;

        if (index) {

            isNew = false;
            index = parseInt(index);

            person = currentItem.People[index];

        } else {
            person = {};
        }

        person.Name = $('#txtPersonName', page).val();
        person.Type = $('#selectPersonType', page).val();
        person.Role = $('#txtPersonRole', page).val();

        if (isNew) {
            currentItem.People.push(person);
        }

        populatePeople(page, currentItem.People);
    }

    function populateRatings(allParentalRatings, select, currentValue) {

        var html = "";

        html += "<option value=''></option>";

        var ratings = [];
        var i, length, rating;

        var currentValueFound = false;

        for (i = 0, length = allParentalRatings.length; i < length; i++) {

            rating = allParentalRatings[i];

            ratings.push({ Name: rating.Name, Value: rating.Name });

            if (rating.Name == currentValue) {
                currentValueFound = true;
            }
        }

        if (currentValue && !currentValueFound) {
            ratings.push({ Name: currentValue, Value: currentValue });
        }

        for (i = 0, length = ratings.length; i < length; i++) {

            rating = ratings[i];

            html += "<option value='" + rating.Value + "'>" + rating.Name + "</option>";
        }

        select.html(html);
    }

    function populateStatus(select) {
        var html = "";

        html += "<option value=''></option>";
        html += "<option value='Continuing'>" + Globalize.translate('OptionContinuing') + "</option>";
        html += "<option value='Ended'>" + Globalize.translate('OptionEnded') + "</option>";
        select.html(html);
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
        list.html(html);
    }

    function editableListViewValues(list) {
        return list.find('a.data').map(function () { return $(this).text(); }).get();
    }

    function generateSliders(fields, currentFields) {

        var html = '';
        for (var i = 0; i < fields.length; i++) {

            var field = fields[i];
            var name = field.name;
            var value = field.value || field.name;
            var checkedHtml = currentFields.indexOf(value) == -1 ? ' checked' : '';
            html += '<paper-checkbox class="selectLockedField" data-value="' + value + '" style="display:block;margin:1em 0;"' + checkedHtml + '>' + name + '</paper-checkbox>';
        }
        return html;
    }

    function populateInternetProviderSettings(page, item, lockedFields) {
        var container = $('#providerSettingsContainer', page);
        lockedFields = lockedFields || new Array();

        var metadatafields = [
            { name: Globalize.translate('OptionName'), value: "Name" },
            { name: Globalize.translate('OptionOverview'), value: "Overview" },
            { name: Globalize.translate('OptionGenres'), value: "Genres" },
            { name: Globalize.translate('OptionParentalRating'), value: "OfficialRating" },
            { name: Globalize.translate('OptionPeople'), value: "Cast" }
        ];

        if (item.Type == "Person") {
            metadatafields.push({ name: Globalize.translate('OptionBirthLocation'), value: "ProductionLocations" });
        } else {
            metadatafields.push({ name: Globalize.translate('OptionProductionLocations'), value: "ProductionLocations" });
        }

        if (item.Type == "Series") {
            metadatafields.push({ name: Globalize.translate('OptionRuntime'), value: "Runtime" });
        }

        metadatafields.push({ name: Globalize.translate('OptionStudios'), value: "Studios" });
        metadatafields.push({ name: Globalize.translate('OptionTags'), value: "Tags" });
        metadatafields.push({ name: Globalize.translate('OptionKeywords'), value: "Keywords" });
        metadatafields.push({ name: Globalize.translate('OptionImages'), value: "Images" });
        metadatafields.push({ name: Globalize.translate('OptionBackdrops'), value: "Backdrops" });

        if (item.Type == "Game") {
            metadatafields.push({ name: Globalize.translate('OptionScreenshots'), value: "Screenshots" });
        }

        var html = '';

        html += "<h1>" + Globalize.translate('HeaderEnabledFields') + "</h1>";
        html += "<p>" + Globalize.translate('HeaderEnabledFieldsHelp') + "</p>";
        html += generateSliders(metadatafields, lockedFields);
        container.html(html);
    }

    function getSelectedAirDays(form) {
        return $('.chkAirDay:checked', form).map(function () {
            return this.getAttribute('data-day');
        }).get();
    }

    function onDeleted(id) {

        var elem = $('#' + id)[0];

        $('.libraryTree').jstree("select_node", elem, true)
            .jstree("delete_node", '#' + id);
    }

    function getAlbumArtists(form) {

        return $('#txtAlbumArtist', form).val().trim().split(';').filter(function (s) {

            return s.length > 0;

        }).map(function (a) {

            return {
                Name: a
            };
        });
    }

    function getArtists(form) {

        return $('#txtArtist', form).val().trim().split(';').filter(function (s) {

            return s.length > 0;

        }).map(function (a) {

            return {
                Name: a
            };
        });
    }

    function editItemMetadataPage() {

        var self = this;

        self.onSubmit = function () {

            var form = this;

            var item = {
                Id: currentItem.Id,
                Name: $('#txtName', form).val(),
                ForcedSortName: $('#txtSortName', form).val(),
                DisplayMediaType: $('#txtDisplayMediaType', form).val(),
                CommunityRating: $('#txtCommunityRating', form).val(),
                VoteCount: $('#txtCommunityVoteCount', form).val(),
                HomePageUrl: $('#txtHomePageUrl', form).val(),
                Budget: $('#txtBudget', form).val(),
                Revenue: $('#txtRevenue', form).val(),
                CriticRating: $('#txtCriticRating', form).val(),
                CriticRatingSummary: $('#txtCriticRatingSummary', form).val(),
                IndexNumber: $('#txtIndexNumber', form).val() || null,
                DisplaySpecialsWithSeasons: form.querySelector('#chkDisplaySpecialsInline').checked,
                AbsoluteEpisodeNumber: $('#txtAbsoluteEpisodeNumber', form).val(),
                DvdEpisodeNumber: $('#txtDvdEpisodeNumber', form).val(),
                DvdSeasonNumber: $('#txtDvdSeasonNumber', form).val(),
                AirsBeforeSeasonNumber: $('#txtAirsBeforeSeason', form).val(),
                AirsAfterSeasonNumber: $('#txtAirsAfterSeason', form).val(),
                AirsBeforeEpisodeNumber: $('#txtAirsBeforeEpisode', form).val(),
                ParentIndexNumber: $('#txtParentIndexNumber', form).val() || null,
                DisplayOrder: $('#selectDisplayOrder', form).val(),
                Players: $('#txtPlayers', form).val(),
                Album: $('#txtAlbum', form).val(),
                AlbumArtist: getAlbumArtists(form),
                ArtistItems: getArtists(form),
                Metascore: $('#txtMetascore', form).val(),
                AwardSummary: $('#txtAwardSummary', form).val(),
                Overview: $('#txtOverview', form).val(),
                ShortOverview: $('#txtShortOverview', form).val(),
                Status: $('#selectStatus', form).val(),
                AirDays: getSelectedAirDays(form),
                AirTime: $('#txtAirTime', form).val(),
                Genres: editableListViewValues($("#listGenres", form)),
                ProductionLocations: editableListViewValues($("#listCountries", form)),
                Tags: editableListViewValues($("#listTags", form)),
                Keywords: editableListViewValues($("#listKeywords", form)),
                Studios: editableListViewValues($("#listStudios", form)).map(function (element) { return { Name: element }; }),

                PremiereDate: EditItemMetadataPage.getDateFromForm(form, '#txtPremiereDate', 'PremiereDate'),
                DateCreated: EditItemMetadataPage.getDateFromForm(form, '#txtDateAdded', 'DateCreated'),
                EndDate: EditItemMetadataPage.getDateFromForm(form, '#txtEndDate', 'EndDate'),
                ProductionYear: $('#txtProductionYear', form).val(),
                AspectRatio: $('#txtOriginalAspectRatio', form).val(),
                Video3DFormat: $('#select3dFormat', form).val(),

                OfficialRating: $('#selectOfficialRating', form).val(),
                CustomRating: $('#selectCustomRating', form).val(),
                People: currentItem.People,
                LockData: form.querySelector("#chkLockData").checked,
                LockedFields: $('.selectLockedField', form).get().filter(function (c) {
                    return !c.checked;
                }).map(function (c) {
                    return c.getAttribute('data-value');
                })
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

            var tagline = $('#txtTagline', form).val();
            item.Taglines = tagline ? [tagline] : [];

            self.submitUpdatedItem(form, item);

            return false;
        };

        self.submitUpdatedItem = function (form, item) {

            var page = $(form).parents('.page');
            unbindItemChanged(page);

            function afterContentTypeUpdated() {

                Dashboard.alert(Globalize.translate('MessageItemSaved'));

                MetadataEditor.getItemPromise().then(function (i) {
                    page.trigger('itemsaved', [i]);
                    bindItemChanged(page);
                });
            }

            ApiClient.updateItem(item).then(function () {

                var newContentType = $('#selectContentType', form).val() || '';

                if ((metadataEditorInfo.ContentType || '') != newContentType) {

                    ApiClient.ajax({

                        url: ApiClient.getUrl('Items/' + item.Id + '/ContentType', {
                            ContentType: newContentType
                        }),

                        type: 'POST'

                    }).then(function () {
                        afterContentTypeUpdated();
                    });

                } else {
                    afterContentTypeUpdated();
                }

            });
        };

        self.getDateFromForm = function (form, element, property) {

            var val = $(element, form).val();

            if (!val) {
                return null;
            }

            if (currentItem[property]) {

                var date = parseISO8601Date(currentItem[property], { toLocal: true });

                var parts = date.toISOString().split('T');

                // If the date is the same, preserve the time
                if (parts[0].indexOf(val) == 0) {

                    var iso = parts[1];

                    val += 'T' + iso;
                }
            }

            return val;
        };

        self.addElementToEditableListview = function (source, sortCallback) {

            var parent = $(source).parents('*[data-role="editableListviewContainer"]');
            var input = parent.find('.txtEditableListview, select');
            var text = input.val();

            if (text == '') return;
            var list = parent.find('ul[data-role="listview"]');
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
        };

        self.onRefreshFormSubmit = function () {
            var page = $(this).parents('.page');

            refreshFromPopupOptions(page);
            return false;
        };

        self.onPersonInfoFormSubmit = function () {

            var page = $(this).parents('.page');

            savePersonInfo(page);
            return false;
        };
    }

    window.EditItemMetadataPage = new editItemMetadataPage();

    function performAdvancedRefresh(page) {

        $('.popupAdvancedRefresh', page).popup('open');

        $('#selectMetadataRefreshMode', page).val('all');
        $('#selectImageRefreshMode', page).val('missing');
    }

    function performSimpleRefresh(page) {

        refreshWithOptions(page, {

            Recursive: true,
            ImageRefreshMode: 'FullRefresh',
            MetadataRefreshMode: 'FullRefresh',
            ReplaceAllMetadata: true
        });
    }

    function refreshFromPopupOptions(page) {

        var metadataRefreshMode = $('#selectMetadataRefreshMode', page).val();
        var imageRefreshMode = $('#selectImageRefreshMode', page).val();

        refreshWithOptions(page, {

            Recursive: true,
            ImageRefreshMode: imageRefreshMode == 'none' ? 'None' : 'FullRefresh',
            MetadataRefreshMode: metadataRefreshMode == 'none' ? 'None' : (metadataRefreshMode == 'local' ? 'ValidationOnly' : 'FullRefresh'),
            ReplaceAllImages: imageRefreshMode == 'all',
            ReplaceAllMetadata: metadataRefreshMode == 'all'
        });

        $('.popupAdvancedRefresh', page).popup('close');
    }

    function refreshWithOptions(page, options) {

        Dashboard.showLoadingMsg();

        ApiClient.refreshItem(currentItem.Id, options);

        if (!ApiClient.isWebSocketOpen()) {

            // For now this is a hack
            setTimeout(function () {
                Dashboard.hideLoadingMsg();
            }, 5000);
        }
    }

    function onWebSocketMessageReceived(e, data) {

        var msg = data;

        if (msg.MessageType === "LibraryChanged") {

            if (msg.Data.ItemsUpdated.indexOf(currentItem.Id) != -1) {

                var page = $.mobile.activePage;

                console.log('Item updated - reloading metadata');
                reload(page);
            }
        }
    }

    function bindItemChanged(page) {

        Events.on(ApiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    function unbindItemChanged(page) {

        Events.off(ApiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    function onItemDeleted(e, itemId) {

        if (currentItem && currentItem.Id == itemId) {

            if (currentItem.ParentId) {
                Dashboard.navigate('edititemmetadata.html?id=' + currentItem.ParentId);
            } else {
                Dashboard.navigate('edititemmetadata.html');
            }
        }
    }

    function showMoreMenu(page, elem) {

        Dashboard.getCurrentUser().then(function (user) {

            var moreCommands = LibraryBrowser.getMoreCommands(currentItem, user);

            var menuItems = [];

            menuItems.push({
                name: Globalize.translate('ButtonAdvancedRefresh'),
                id: 'refresh',
                ironIcon: 'refresh'
            });

            if (moreCommands.indexOf('delete') != -1) {
                menuItems.push({
                    name: Globalize.translate('ButtonDelete'),
                    id: 'delete',
                    ironIcon: 'delete'
                });
            }

            menuItems.push({
                name: Globalize.translate('ButtonEditImages'),
                id: 'editimages',
                ironIcon: 'photo'
            });

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: menuItems,
                    positionTo: elem,
                    callback: function (id) {

                        switch (id) {

                            case 'refresh':
                                performAdvancedRefresh(page);
                                break;
                            case 'delete':
                                LibraryBrowser.deleteItems([currentItem.Id]);
                                break;
                            case 'editimages':
                                LibraryBrowser.editImages(currentItem.Id);
                                break;
                            default:
                                break;
                        }
                    }
                });

            });

        });
    }

    $(document).on('pageinit', "#editItemMetadataPage", function () {

        var page = this;

        $('.btnSimpleRefresh', this).on('click', function () {

            performAdvancedRefresh(page);
        });

        $('.btnEditImages', page).on('click', function () {

            LibraryBrowser.editImages(currentItem.Id);
        });

        $('#btnIdentify', page).on('click', function () {

            LibraryBrowser.identifyItem(currentItem.Id);
        });

        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.id != currentItem.Id) {

                //$.mobile.urlHistory.ignoreNextHashChange = true;
                window.location.hash = 'editItemMetadataPage?id=' + data.id;
                reload(page);
            }
        });

        $("#btnAddPerson", page).on('click', function (event, data) {

            editPerson(page, {});
        });

        $('.editItemMetadataForm').off('submit', EditItemMetadataPage.onSubmit).on('submit', EditItemMetadataPage.onSubmit);
        $('.popupEditPersonForm').off('submit', EditItemMetadataPage.onPersonInfoFormSubmit).on('submit', EditItemMetadataPage.onPersonInfoFormSubmit);
        $('.popupAdvancedRefreshForm').off('submit', EditItemMetadataPage.onRefreshFormSubmit).on('submit', EditItemMetadataPage.onRefreshFormSubmit);

        $('.btnMore', page).on('click', function () {
            showMoreMenu(page, this);
        });

    }).on('pageshow', "#editItemMetadataPage", function () {

        var page = this;

        Events.on(LibraryBrowser, 'itemdeleting', onItemDeleted);
        reload(page);

    }).on('pagebeforehide', "#editItemMetadataPage", function () {

        var page = this;
        Events.off(LibraryBrowser, 'itemdeleting', onItemDeleted);

        unbindItemChanged(page);
    });

})(jQuery, document, window);

