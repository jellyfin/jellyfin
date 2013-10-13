(function ($, document, window) {

    function getNode(item, folderState) {

        var state = item.IsFolder ? folderState : '';

        var name = item.Name;

        if (item.IndexNumber != null && item.Type != "Season") {
            name = item.IndexNumber + " - " + name;
        }

        var cssClass = "editorNode";

        if (item.LocationType == "Offline") {
            cssClass += " offlineEditorNode";
        }

        var htmlName = "<div class='" + cssClass + "'>";

        if (item.EnableInternetProviders === false) {
            htmlName += '<img src="css/images/editor/lock.png" />';
        }

        htmlName += name;

        if (!item.LocalTrailerCount && item.Type == "Movie") {
            htmlName += '<img src="css/images/editor/missingtrailer.png" title="Missing local trailer." />';
        }

        if (!item.ImageTags || !item.ImageTags.Primary) {
            htmlName += '<img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." />';
        }

        if (!item.BackdropImageTags || !item.BackdropImageTags.length) {
            if (item.Type !== "Episode") {
                htmlName += '<img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." />';
            }
        }

        if (!item.ImageTags || !item.ImageTags.Logo) {
            if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Series" || item.Type == "Artist" || item.Type == "MusicArtist" || item.Type == "BoxSet") {
                htmlName += '<img src="css/images/editor/missinglogo.png" title="Missing logo image." />';
            }
        }

        htmlName += "</div>";

        var rel = item.IsFolder ? 'folder' : 'default';

        return { attr: { id: item.Id, rel: rel, itemtype: item.Type }, data: htmlName, state: state };
    }

    function loadNode(page, node, openItems, selectedId, callback) {

        if (node == '-1') {

            ApiClient.getRootFolder(Dashboard.getCurrentUserId()).done(function (folder) {

                callback(getNode(folder, 'open'));

            });

            return;
        }

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: node.attr("id"),
            SortBy: 'SortName',
            Fields: 'MetadataSettings'

        }).done(function (result) {

            var nodes = result.Items.map(function (i) {

                var state = openItems.indexOf(i.Id) == -1 ? 'closed' : 'open';

                return getNode(i, state);

            });

            callback(nodes);

            if (selectedId && result.Items.filter(function (f) {

                return f.Id == selectedId;

            }).length) {

                selectNode(page, selectedId);
            }

        });

    }

    function selectNode(page, id) {

        var elem = $('#' + id, page)[0];

        $.jstree._reference(".libraryTree", page).select_node(elem);

        if (elem) {
            elem.scrollIntoView();
        }

        $(document).scrollTop(0);
    }

    function initializeTree(page, openItems, selectedId) {

        $('.libraryTree', page).jstree({

            "plugins": ["themes", "ui", "json_data"],

            data: function (node, callback) {
                loadNode(page, node, openItems, selectedId, callback);
            },

            json_data: {

                data: function (node, callback) {
                    loadNode(page, node, openItems, selectedId, callback);
                }

            },

            core: { initially_open: [], load_open: true, html_titles: true },
            ui: { initially_select: [] },

            themes: {
                theme: 'mb3',
                url: 'thirdparty/jstree1.0fix2/themes/mb3/style.css?v=' + Dashboard.initialServerVersion
            }

        }).off('select_node.jstree').on('select_node.jstree', function (event, data) {

            var eventData = {
                id: data.rslt.obj.attr("id"),
                itemType: data.rslt.obj.attr("itemtype")
            };

            $(this).trigger('itemclicked', [eventData]);

        });
    }

    $(document).on('pagebeforeshow', ".metadataEditorPage", function () {

        window.MetadataEditor = new metadataEditor();

        var page = this;

        var id = MetadataEditor.currentItemId;

        if (id) {

            ApiClient.getAncestorItems(id, Dashboard.getCurrentUserId()).done(function (ancestors) {

                var ids = ancestors.map(function (i) {
                    return i.Id;
                });

                initializeTree(page, ids, id);
            });

        } else {
            initializeTree(page, []);
        }

    }).on('pagebeforehide', ".metadataEditorPage", function () {

        var page = this;

        $('.libraryTree', page).off('select_node.jstree');

    });

    function metadataEditor() {

        var self = this;

        function ensureInitialValues() {

            if (self.currentItemType || self.currentItemName || self.currentItemId) {
                return;
            }

            var url = window.location.hash || window.location.toString();

            var name = getParameterByName('person', url);

            if (name) {
                self.currentItemType = "Person";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('studio', url);

            if (name) {
                self.currentItemType = "Studio";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('genre', url);

            if (name) {
                self.currentItemType = "Genre";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('musicgenre', url);

            if (name) {
                self.currentItemType = "MusicGenre";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('gamegenre', url);

            if (name) {
                self.currentItemType = "GameGenre";
                self.currentItemName = name;
                return;
            }

            name = getParameterByName('artist', url);

            if (name) {
                self.currentItemType = "Artist";
                self.currentItemName = name;
                return;
            }

            var id = getParameterByName('id', url);

            if (id) {
                self.currentItemId = id;
                self.currentItemType = null;
            }
        };

        self.getItemPromise = function () {

            var currentItemType = self.currentItemType;
            var currentItemName = self.currentItemName;
            var currentItemId = self.currentItemId;

            if (currentItemType == "Person") {
                return ApiClient.getPerson(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "Studio") {
                return ApiClient.getStudio(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "Genre") {
                return ApiClient.getGenre(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "MusicGenre") {
                return ApiClient.getMusicGenre(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "GameGenre") {
                return ApiClient.getGameGenre(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemType == "Artist") {
                return ApiClient.getArtist(currentItemName, Dashboard.getCurrentUserId());
            }

            if (currentItemId) {
                return ApiClient.getItem(Dashboard.getCurrentUserId(), currentItemId);
            }

            return ApiClient.getRootFolder(Dashboard.getCurrentUserId());
        };

        self.getEditQueryString = function (item) {

            var query;

            if (item.Type == "Person" ||
                item.Type == "Studio" ||
                item.Type == "Genre" ||
                item.Type == "MusicGenre" ||
                item.Type == "GameGenre" ||
                item.Type == "Artist") {
                query = item.Type + "=" + ApiClient.encodeName(item.Name);

            } else {
                query = "id=" + item.Id;
            }

            var context = getParameterByName('context');

            if (context) {
                query += "&context=" + context;
            }

            return query;
        };

        ensureInitialValues();
    }


})(jQuery, document, window);

(function ($, document, window) {

    var currentItem;

    function updateTabs(page, item) {

        var query = MetadataEditor.getEditQueryString(item);

        $('#btnEditPeople', page).attr('href', 'edititempeople.html?' + query);
        $('#btnEditImages', page).attr('href', 'edititemimages.html?' + query);
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        MetadataEditor.getItemPromise().done(function (item) {

            if (item.LocationType == "Offline") {
                $('#ulSave', page).hide();
            } else {
                $('#ulSave', page).show();
            }

            $('#btnRefresh', page).button('enable');

            $('#refreshLoading', page).hide();

            currentItem = item;

            if (item.IsFolder) {
                $('#fldRecursive', page).css("display", "inline-block")
            } else {
                $('#fldRecursive', page).hide();
            }

            LibraryBrowser.renderName(item, $('.itemName', page), true);

            updateTabs(page, item);

            setFieldVisibilities(page, item);
            fillItemInfo(page, item);

            if (item.Type == "Person" || item.Type == "Studio" || item.Type == "MusicGenre" || item.Type == "Genre" || item.Type == "Artist") {
                $('#btnEditPeople', page).hide();
            } else {
                $('#btnEditPeople', page).show();
            }

            Dashboard.hideLoadingMsg();
        });
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

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "MusicVideo" || item.Type == "Series" || item.Type == "Game") {
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
            $('#fldZap2It', page).show();
        } else {
            $('#fldZap2It', page).hide();
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
            $('#fldCommunityVoteCount', page).hide();
            $('#fldOfficialRating', page).hide();
            $('#fldCustomRating', page).hide();
            $('#genresCollapsible', page).hide();
            $('#studiosCollapsible', page).hide();
        } else {
            $('#fldCommunityRating', page).show();
            $('#fldCommunityVoteCount', page).show();
            $('#fldOfficialRating', page).show();
            $('#fldCustomRating', page).show();
            $('#genresCollapsible', page).show();
            $('#studiosCollapsible', page).show();
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
        populateInternetProviderSettings(page, item, item.LockedFields);

        $('#txtPath', page).val(item.Path || '');
        $('#txtName', page).val(item.Name || "");
        $('#txtOverview', page).val(item.Overview || "");
        $('#txtSortName', page).val(item.SortName || "");
        $('#txtDisplayMediaType', page).val(item.DisplayMediaType || "");
        $('#txtCommunityRating', page).val(item.CommunityRating || "");
        $('#txtCommunityVoteCount', page).val(item.VoteCount || "");
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
        $('#txtArtist', page).val(artists.join(';'));

        var date;

        if (item.DateCreated) {
            try {
                date = parseISO8601Date(item.DateCreated, { toLocal: true });

                $('#txtDateAdded', page).val(date.toISOString().slice(0, 10));
            }
            catch (e) {
                $('#txtDateAdded', page).val('');
            }
        } else {
            $('#txtDateAdded', page).val('');
        }

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

        var placeofBirth = item.ProductionLocations && item.ProductionLocations.length ? item.ProductionLocations[0] : '';
        $('#txtPlaceOfBirth', page).val(placeofBirth);

        $('#txtOriginalAspectRatio', page).val(item.AspectRatio || "");

        var providerIds = item.ProviderIds || {};

        $('#txtGamesDb', page).val(providerIds.Gamesdb || "");
        $('#txtImdb', page).val(providerIds.Imdb || "");
        $('#txtTmdb', page).val(providerIds.Tmdb || "");
        $('#txtTvdb', page).val(providerIds.Tvdb || "");
        $('#txtTvCom', page).val(providerIds.Tvcom || "");
        $('#txtMusicBrainz', page).val(providerIds.Musicbrainz || "");
        $('#txtMusicBrainzReleaseGroupId', page).val(providerIds.MusicBrainzReleaseGroup || "");
        $('#txtRottenTomatoes', page).val(providerIds.RottenTomatoes || "");
        $('#txtZap2It', page).val(providerIds.Zap2It || "");

        if (item.RunTimeTicks) {

            var minutes = item.RunTimeTicks / 600000000;

            $('#txtSeriesRuntime', page).val(Math.round(minutes));
        } else {
            $('#txtSeriesRuntime', page).val("");
        }

        $('.txtProviderId', page).trigger('change');
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
        items = items || [];
        if (typeof (sortCallback) === 'undefined') {
            items.sort(function (a, b) { return a.toLowerCase().localeCompare(b.toLowerCase()); });
        } else {
            items = sortCallback(items);
        }
        var html = '';
        for (var i = 0; i < items.length; i++) {
            html += '<li><a class="data">' + items[i] + '</a><a href="#" onclick="EditItemMetadataPage.removeElementFromListview(this)"></a></li>';
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
                Id: currentItem.Id,
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
                Artists: $('#txtArtist', form).val().split(';'),
                Overview: $('#txtOverview', form).val(),
                Status: $('#selectStatus', form).val(),
                AirDays: editableListViewValues($("#listAirDays", form)),
                AirTime: convertTo12HourFormat($('#txtAirTime', form).val()),
                Genres: editableListViewValues($("#listGenres", form)),
                Tags: editableListViewValues($("#listTags", form)),
                Studios: editableListViewValues($("#listStudios", form)).map(function (element) { return { Name: element }; }),

                PremiereDate: $('#txtPremiereDate', form).val() || null,
                DateCreated: $('#txtDateAdded', form).val() || null,
                EndDate: $('#txtEndDate', form).val() || null,
                ProductionYear: $('#txtProductionYear', form).val(),
                AspectRatio: $('#txtOriginalAspectRatio', form).val(),
                Video3DFormat: $('#select3dFormat', form).val(),

                Language: $('#selectLanguage', form).val(),
                OfficialRating: $('#selectOfficialRating', form).val(),
                CustomRating: $('#selectCustomRating', form).val(),
                People: currentItem.People,
                EnableInternetProviders: $("#enableInternetProviders", form).prop('checked'),
                LockedFields: $('.selectLockedField', form).map(function () {
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
                    MusicBrainzReleaseGroup: $('#txtMusicBrainzReleaseGroupId', form).val(),
                    RottenTomatoes: $('#txtRottenTomatoes', form).val(),
                    Zap2It: $('#txtZap2It', form).val()
                }
            };

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

        $('#txtGamesDb', this).on('change', function () {

            var val = this.value;

            if (val) {

                $('#btnOpenGamesDb', page).attr('href', 'http://thegamesdb.net/game/' + val);
            } else {
                $('#btnOpenGamesDb', page).attr('href', '#');
            }

        });

        $('#txtImdb', this).on('change', function () {

            var val = this.value;

            if (val) {

                if (currentItem.Type == "Person") {
                    $('#btnOpenImdb', page).attr('href', 'http://www.imdb.com/name/' + val);
                } else {
                    $('#btnOpenImdb', page).attr('href', 'http://www.imdb.com/title/' + val);
                }
            } else {
                $('#btnOpenImdb', page).attr('href', '#');
            }

        });

        $('#txtMusicBrainz', this).on('change', function () {

            var val = this.value;

            if (val) {

                if (currentItem.Type == "MusicArtist" || currentItem.Type == "Artist") {
                    $('#btnOpenMusicbrainz', page).attr('href', 'http://musicbrainz.org/artist/' + val);
                } else {
                    $('#btnOpenMusicbrainz', page).attr('href', 'http://musicbrainz.org/release/' + val);
                }

            } else {
                $('#btnOpenMusicbrainz', page).attr('href', '#');
            }

        });

        $('#txtMusicBrainzReleaseGroupId', this).on('change', function () {

            var val = this.value;

            if (val) {

                $('#btnOpenMusicbrainzReleaseGroup', page).attr('href', 'http://musicbrainz.org/release-group/' + val);
            } else {
                $('#btnOpenMusicbrainzReleaseGroup', page).attr('href', '#');
            }

        });

        $('#txtTmdb', this).on('change', function () {

            var val = this.value;

            if (val) {

                if (currentItem.Type == "Movie" || currentItem.Type == "Trailer" || currentItem.Type == "MusicVideo")
                    $('#btnOpenTmdb', page).attr('href', 'http://www.themoviedb.org/movie/' + val);
                else if (currentItem.Type == "BoxSet")
                    $('#btnOpenTmdb', page).attr('href', 'http://www.themoviedb.org/collection/' + val);
                else if (currentItem.Type == "Person")
                    $('#btnOpenTmdb', page).attr('href', 'http://www.themoviedb.org/person/' + val);

            } else {
                $('#btnOpenTmdb', page).attr('href', '#');
            }

        });

        $('#txtTvdb', this).on('change', function () {

            var val = this.value;

            if (val && currentItem.Type == "Series") {

                $('#btnOpenTvdb', page).attr('href', 'http://thetvdb.com/index.php?tab=series&id=' + val);

            } else {
                $('#btnOpenTvdb', page).attr('href', '#');
            }

        });

        $('#txtZap2It', this).on('change', function () {

            var val = this.value;

            if (val) {

                $('#btnOpenZap2It', page).attr('href', 'http://tvlistings.zap2it.com/tv/dexter/' + val);

            } else {
                $('#btnOpenZap2It', page).attr('href', '#');
            }

        });

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

        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.id != currentItem.Id) {

                MetadataEditor.currentItemId = data.id;
                MetadataEditor.currentItemName = data.itemName;
                MetadataEditor.currentItemType = data.itemType;
                //Dashboard.navigate('edititemmetadata.html?id=' + data.id);

                $.mobile.urlHistory.ignoreNextHashChange = true;
                window.location.hash = 'editItemMetadataPage?id=' + data.id;

                reload(page);
            }
        });

    }).on('pagebeforeshow', "#editItemMetadataPage", function () {

        var page = this;

        reload(page);

    });

})(jQuery, document, window);

