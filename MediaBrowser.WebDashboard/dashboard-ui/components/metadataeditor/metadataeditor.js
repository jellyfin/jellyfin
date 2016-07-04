define(['dialogHelper', 'datetime', 'jQuery', 'emby-checkbox', 'emby-input', 'emby-select', 'listViewStyle', 'emby-textarea', 'emby-button', 'paper-icon-button-light'], function (dialogHelper, datetime, $) {

    var currentContext;
    var metadataEditorInfo;
    var currentItem;

    function isDialog() {
        return currentContext.classList.contains('dialog');
    }

    function closeDialog(isSubmitted) {

        if (isDialog()) {
            dialogHelper.close(currentContext);
        }
    }

    function submitUpdatedItem(form, item) {

        function afterContentTypeUpdated() {

            require(['toast'], function (toast) {
                toast(Globalize.translate('MessageItemSaved'));
            });

            Dashboard.hideLoadingMsg();
            closeDialog(true);
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
    }

    function getSelectedAirDays(form) {
        return $('.chkAirDay:checked', form).map(function () {
            return this.getAttribute('data-day');
        }).get();
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

    function getDateFromForm(form, element, property) {

        var val = $(element, form).val();

        if (!val) {
            return null;
        }

        if (currentItem[property]) {

            var date = datetime.parseISO8601Date(currentItem[property], true);

            var parts = date.toISOString().split('T');

            // If the date is the same, preserve the time
            if (parts[0].indexOf(val) == 0) {

                var iso = parts[1];

                val += 'T' + iso;
            }
        }

        return val;
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        try {
            var item = {
                Id: currentItem.Id,
                Name: $('#txtName', form).val(),
                OriginalTitle: $('#txtOriginalName', form).val(),
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

                PremiereDate: getDateFromForm(form, '#txtPremiereDate', 'PremiereDate'),
                DateCreated: getDateFromForm(form, '#txtDateAdded', 'DateCreated'),
                EndDate: getDateFromForm(form, '#txtEndDate', 'EndDate'),
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

            submitUpdatedItem(form, item);
        } catch (err) {
            alert(err);
        }

        // Disable default form submission
        return false;
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function editableListViewValues(list) {
        return list.find('.textValue').map(function () { return $(this).text(); }).get();
    }

    function addElementToEditableListview(source, sortCallback) {

        require(['prompt'], function (prompt) {

            prompt({
                label: 'Value:'
            }).then(function (text) {
                var list = $(source).parents('.editableListviewContainer').find('.paperList');
                var items = editableListViewValues(list);
                items.push(text);
                populateListView(list[0], items, sortCallback);
            });
        });
    }

    function removeElementFromListview(source) {
        $(source).parents('paper-icon-item').remove();
    }

    function editPerson(context, person, index) {

        require(['components/metadataeditor/personeditor'], function (personeditor) {

            personeditor.show(person).then(function (updatedPerson) {

                var isNew = index == -1;

                if (isNew) {
                    currentItem.People.push(updatedPerson);
                }

                populatePeople(context, currentItem.People);
            });
        });
    }

    function showRefreshMenu(context, button) {

        require(['refreshDialog'], function (refreshDialog) {
            new refreshDialog({
                itemIds: [currentItem.Id],
                serverId: ApiClient.serverInfo().Id
            }).show();
        });
    }

    function showMoreMenu(context, button, user) {

        var items = [];

        items.push({
            name: Globalize.translate('ButtonEditImages'),
            id: 'images'
        });

        if (LibraryBrowser.canIdentify(user, currentItem.Type)) {
            items.push({
                name: Globalize.translate('ButtonIdentify'),
                id: 'identify'
            });
        }

        items.push({
            name: Globalize.translate('ButtonRefresh'),
            id: 'refresh'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: items,
                positionTo: button,
                callback: function (id) {

                    switch (id) {

                        case 'identify':
                            LibraryBrowser.identifyItem(currentItem.Id).then(function () {
                                reload(context, currentItem.Id);
                            });
                            break;
                        case 'refresh':
                            showRefreshMenu(context, button);
                            break;
                        case 'images':
                            LibraryBrowser.editImages(currentItem.Id);
                            break;
                        default:
                            break;
                    }
                }
            });

        });

    }

    function onWebSocketMessageReceived(e, data) {

        var msg = data;

        if (msg.MessageType === "LibraryChanged") {

            if (msg.Data.ItemsUpdated.indexOf(currentItem.Id) != -1) {

                console.log('Item updated - reloading metadata');
                reload(currentContext, currentItem.Id);
            }
        }
    }

    function bindItemChanged(context) {

        Events.on(ApiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    function unbindItemChanged(context) {

        Events.off(ApiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    function onEditorClick(e) {
        var btnRemoveFromEditorList = parentWithClass(e.target, 'btnRemoveFromEditorList');
        if (btnRemoveFromEditorList) {
            removeElementFromListview(btnRemoveFromEditorList);
        }

        var btnAddTextItem = parentWithClass(e.target, 'btnAddTextItem');
        if (btnAddTextItem) {
            addElementToEditableListview(btnAddTextItem);
        }
    }

    function init(context) {

        $('.btnCancel', context).on('click', function () {

            closeDialog(false);
        });

        context.querySelector('.btnMore').addEventListener('click', function (e) {

            Dashboard.getCurrentUser().then(function (user) {
                showMoreMenu(context, e.target, user);
            });

        });

        context.querySelector('.btnHeaderSave').addEventListener('click', function (e) {

            context.querySelector('.btnSave').click();
        });

        context.querySelector('#chkLockData').addEventListener('click', function (e) {

            if (!e.target.checked) {
                $('.providerSettingsContainer').show();
            } else {
                $('.providerSettingsContainer').hide();
            }
        });

        context.removeEventListener('click', onEditorClick);
        context.addEventListener('click', onEditorClick);

        $('form', context).off('submit', onSubmit).on('submit', onSubmit);

        $("#btnAddPerson", context).on('click', function (event, data) {

            editPerson(context, {}, -1);
        });

        // For now this is only supported in dialog mode because we have a way of knowing when it closes
        if (isDialog()) {
            bindItemChanged(context);
        }
    }

    function getItem(itemId) {
        if (itemId) {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), itemId);
        }

        return ApiClient.getRootFolder(Dashboard.getCurrentUserId());
    }

    function getEditorConfig(itemId) {
        if (itemId) {
            return ApiClient.getJSON(ApiClient.getUrl('Items/' + itemId + '/MetadataEditor'));
        }

        return Promise.resolve({});
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

    function renderContentTypeOptions(context, metadataInfo) {

        if (metadataInfo.ContentTypeOptions.length) {
            $('#fldContentType', context).show();
        } else {
            $('#fldContentType', context).hide();
        }

        var html = metadataInfo.ContentTypeOptions.map(function (i) {


            return '<option value="' + i.Value + '">' + i.Name + '</option>';

        }).join('');

        $('#selectContentType', context).html(html).val(metadataInfo.ContentType || '');
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

    function loadExternalIds(context, item, externalIds) {

        var html = '';

        var providerIds = item.ProviderIds || {};

        for (var i = 0, length = externalIds.length; i < length; i++) {

            var idInfo = externalIds[i];

            var id = "txt1" + idInfo.Key;
            var buttonId = "btnOpen1" + idInfo.Key;
            var formatString = idInfo.UrlFormatString || '';

            var labelText = Globalize.translate('LabelDynamicExternalId').replace('{0}', idInfo.Name);

            html += '<div class="inputContainer">';
            html += '<div style="display: flex; align-items: center;">';

            var value = providerIds[idInfo.Key] || '';

            html += '<div style="flex-grow:1;">';
            html += '<input is="emby-input" class="txtExternalId" value="' + value + '" data-providerkey="' + idInfo.Key + '" data-formatstring="' + formatString + '" data-buttonclass="' + buttonId + '" id="' + id + '" label="' + labelText + '"/>';
            html += '</div>';

            if (formatString) {
                html += '<a class="clearLink ' + buttonId + '" href="#" target="_blank" data-role="none" style="float: none; width: 1.75em"><button type="button" is="paper-icon-button-light" class="autoSize"><i class="md-icon">open_in_browser</i></button></a>';
            }
            html += '</div>';

            html += '</div>';
        }

        var elem = $('.externalIds', context).html(html);

        $('.txtExternalId', elem).on('change', onExternalIdChange).trigger('change');
    }

    function setFieldVisibilities(context, item) {

        if (item.Path && item.LocationType != 'Remote') {
            $('#fldPath', context).show();
        } else {
            $('#fldPath', context).hide();
        }

        if (item.Type == "Series" || item.Type == "Movie" || item.Type == "Trailer") {
            $('#fldOriginalName', context).show();
        } else {
            $('#fldOriginalName', context).hide();
        }

        if (item.Type == "Series") {
            $('#fldSeriesRuntime', context).show();
        } else {
            $('#fldSeriesRuntime', context).hide();
        }

        if (item.Type == "Series" || item.Type == "Person") {
            $('#fldEndDate', context).show();
        } else {
            $('#fldEndDate', context).hide();
        }

        if (item.Type == "Movie" || item.MediaType == "Game" || item.MediaType == "Trailer" || item.Type == "MusicVideo") {
            $('#fldBudget', context).show();
            $('#fldRevenue', context).show();
        } else {
            $('#fldBudget', context).hide();
            $('#fldRevenue', context).hide();
        }

        if (item.Type == "MusicAlbum") {
            $('#albumAssociationMessage', context).show();
        } else {
            $('#albumAssociationMessage', context).hide();
        }

        if (item.MediaType == "Game") {
            $('#fldPlayers', context).show();
        } else {
            $('#fldPlayers', context).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer") {
            $('#fldCriticRating', context).show();
            $('#fldCriticRatingSummary', context).show();
        } else {
            $('#fldCriticRating', context).hide();
            $('#fldCriticRatingSummary', context).hide();
        }

        if (item.Type == "Movie") {
            $('#fldAwardSummary', context).show();
        } else {
            $('#fldAwardSummary', context).hide();
        }

        if (item.Type == "Movie" || item.Type == "Trailer") {
            $('#fldMetascore', context).show();
        } else {
            $('#fldMetascore', context).hide();
        }

        if (item.Type == "Series") {
            $('#fldStatus', context).show();
            $('#fldAirDays', context).show();
            $('#fldAirTime', context).show();
        } else {
            $('#fldStatus', context).hide();
            $('#fldAirDays', context).hide();
            $('#fldAirTime', context).hide();
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            $('#fld3dFormat', context).show();
        } else {
            $('#fld3dFormat', context).hide();
        }

        if (item.Type == "Audio") {
            $('#fldAlbumArtist', context).show();
        } else {
            $('#fldAlbumArtist', context).hide();
        }

        if (item.Type == "Audio" || item.Type == "MusicVideo") {
            $('#fldArtist', context).show();
            $('#fldAlbum', context).show();
        } else {
            $('#fldArtist', context).hide();
            $('#fldAlbum', context).hide();
        }

        if (item.Type == "Episode") {
            $('#collapsibleDvdEpisodeInfo', context).show();
        } else {
            $('#collapsibleDvdEpisodeInfo', context).hide();
        }

        if (item.Type == "Episode" && item.ParentIndexNumber == 0) {
            $('#collapsibleSpecialEpisodeInfo', context).show();
        } else {
            $('#collapsibleSpecialEpisodeInfo', context).hide();
        }

        if (item.Type == "Person" || item.Type == "Genre" || item.Type == "Studio" || item.Type == "GameGenre" || item.Type == "MusicGenre" || item.Type == "TvChannel") {
            $('#fldCommunityRating', context).hide();
            $('#fldCommunityVoteCount', context).hide();
            $('#genresCollapsible', context).hide();
            $('#peopleCollapsible', context).hide();
            $('#studiosCollapsible', context).hide();

            if (item.Type == "TvChannel") {
                $('#fldOfficialRating', context).show();
            } else {
                $('#fldOfficialRating', context).hide();
            }
            $('#fldCustomRating', context).hide();
        } else {
            $('#fldCommunityRating', context).show();
            $('#fldCommunityVoteCount', context).show();
            $('#genresCollapsible', context).show();
            $('#peopleCollapsible', context).show();
            $('#studiosCollapsible', context).show();
            $('#fldOfficialRating', context).show();
            $('#fldCustomRating', context).show();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "MusicArtist") {
            $('#countriesCollapsible', context).show();
        } else {
            $('#countriesCollapsible', context).hide();
        }

        if (item.Type == "TvChannel") {
            $('#tagsCollapsible', context).hide();
            $('#metadataSettingsCollapsible', context).hide();
            $('#fldPremiereDate', context).hide();
            $('#fldDateAdded', context).hide();
            $('#fldYear', context).hide();
        } else {
            $('#tagsCollapsible', context).show();
            $('#metadataSettingsCollapsible', context).show();
            $('#fldPremiereDate', context).show();
            $('#fldDateAdded', context).show();
            $('#fldYear', context).show();
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            $('#keywordsCollapsible', context).show();
        } else {
            $('#keywordsCollapsible', context).hide();
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            $('#fldSourceType', context).show();
        } else {
            $('#fldSourceType', context).hide();
        }

        if (item.Type == "Person") {
            context.querySelector('#txtProductionYear').label(Globalize.translate('LabelBirthYear'));
            context.querySelector("#txtPremiereDate").label(Globalize.translate('LabelBirthDate'));
            context.querySelector("#txtEndDate").label(Globalize.translate('LabelDeathDate'));
            $('#fldPlaceOfBirth', context).show();
        } else {
            context.querySelector('#txtProductionYear').label(Globalize.translate('LabelYear'));
            context.querySelector("#txtPremiereDate").label(Globalize.translate('LabelReleaseDate'));
            context.querySelector("#txtEndDate").label(Globalize.translate('LabelEndDate'));
            $('#fldPlaceOfBirth', context).hide();
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            $('#fldOriginalAspectRatio', context).show();
        } else {
            $('#fldOriginalAspectRatio', context).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode" || item.Type == "Season") {
            $('#fldIndexNumber', context).show();

            if (item.Type == "Episode") {
                context.querySelector('#txtIndexNumber').label(Globalize.translate('LabelEpisodeNumber'));
            } else if (item.Type == "Season") {
                context.querySelector('#txtIndexNumber').label(Globalize.translate('LabelSeasonNumber'));
            } else if (item.Type == "Audio") {
                context.querySelector('#txtIndexNumber').label(Globalize.translate('LabelTrackNumber'));
            } else {
                context.querySelector('#txtIndexNumber').label(Globalize.translate('LabelNumber'));
            }
        } else {
            $('#fldIndexNumber', context).hide();
        }

        if (item.Type == "Audio" || item.Type == "Episode") {
            $('#fldParentIndexNumber', context).show();

            if (item.Type == "Episode") {
                context.querySelector('#txtParentIndexNumber').label(Globalize.translate('LabelSeasonNumber'));
            } else if (item.Type == "Audio") {
                context.querySelector('#txtParentIndexNumber').label(Globalize.translate('LabelDiscNumber'));
            } else {
                context.querySelector('#txtParentIndexNumber').label(Globalize.translate('LabelParentNumber'));
            }
        } else {
            $('#fldParentIndexNumber', context).hide();
        }

        if (item.Type == "BoxSet") {
            $('#fldDisplayOrder', context).show();

            $('#selectDisplayOrder', context).html('<option value="SortName">' + Globalize.translate('OptionSortName') + '</option><option value="PremiereDate">' + Globalize.translate('OptionReleaseDate') + '</option>');
        } else {
            $('#selectDisplayOrder', context).html('');
            $('#fldDisplayOrder', context).hide();
        }

        var displaySettingFields = $('.fldDisplaySetting', context);
        if (displaySettingFields.filter(function (index) {

            return displaySettingFields[index].style.display != 'none';

        }).length) {
            $('#collapsibleDisplaySettings', context).show();
        } else {
            $('#collapsibleDisplaySettings', context).hide();
        }
    }

    function fillItemInfo(context, item, parentalRatingOptions) {

        var select = $('#selectOfficialRating', context);

        populateRatings(parentalRatingOptions, select, item.OfficialRating);

        select.val(item.OfficialRating || "");

        select = $('#selectCustomRating', context);

        populateRatings(parentalRatingOptions, select, item.CustomRating);

        select.val(item.CustomRating || "");

        var selectStatus = $('#selectStatus', context);
        populateStatus(selectStatus);
        selectStatus.val(item.Status || "");

        $('#select3dFormat', context).val(item.Video3DFormat || "");

        $('.chkAirDay', context).each(function () {

            this.checked = (item.AirDays || []).indexOf(this.getAttribute('data-day')) != -1;

        });

        populateListView($('#listCountries', context)[0], item.ProductionLocations || []);
        populateListView($('#listGenres', context)[0], item.Genres);
        populatePeople(context, item.People || []);

        populateListView($('#listStudios', context)[0], (item.Studios || []).map(function (element) { return element.Name || ''; }));

        populateListView($('#listTags', context)[0], item.Tags);
        populateListView($('#listKeywords', context)[0], item.Keywords);

        var lockData = (item.LockData || false);
        var chkLockData = context.querySelector("#chkLockData");
        chkLockData.checked = lockData;
        if (chkLockData.checked) {
            $('.providerSettingsContainer', context).hide();
        } else {
            $('.providerSettingsContainer', context).show();
        }
        populateInternetProviderSettings(context, item, item.LockedFields);

        $('#txtPath', context).val(item.Path || '');
        $('#txtName', context).val(item.Name || "");
        $('#txtOriginalName', context).val(item.OriginalTitle || "");
        context.querySelector('#txtOverview').value = item.Overview || '';
        $('#txtShortOverview', context).val(item.ShortOverview || "");
        $('#txtTagline', context).val((item.Taglines && item.Taglines.length ? item.Taglines[0] : ''));
        $('#txtSortName', context).val(item.ForcedSortName || "");
        $('#txtDisplayMediaType', context).val(item.DisplayMediaType || "");
        $('#txtCommunityRating', context).val(item.CommunityRating || "");
        $('#txtCommunityVoteCount', context).val(item.VoteCount || "");
        $('#txtHomePageUrl', context).val(item.HomePageUrl || "");

        $('#txtAwardSummary', context).val(item.AwardSummary || "");
        $('#txtMetascore', context).val(item.Metascore || "");

        $('#txtBudget', context).val(item.Budget || "");
        $('#txtRevenue', context).val(item.Revenue || "");

        $('#txtCriticRating', context).val(item.CriticRating || "");
        $('#txtCriticRatingSummary', context).val(item.CriticRatingSummary || "");

        $('#txtIndexNumber', context).val(('IndexNumber' in item) ? item.IndexNumber : "");
        $('#txtParentIndexNumber', context).val(('ParentIndexNumber' in item) ? item.ParentIndexNumber : "");
        $('#txtPlayers', context).val(item.Players || "");

        $('#txtAbsoluteEpisodeNumber', context).val(('AbsoluteEpisodeNumber' in item) ? item.AbsoluteEpisodeNumber : "");
        $('#txtDvdEpisodeNumber', context).val(('DvdEpisodeNumber' in item) ? item.DvdEpisodeNumber : "");
        $('#txtDvdSeasonNumber', context).val(('DvdSeasonNumber' in item) ? item.DvdSeasonNumber : "");
        $('#txtAirsBeforeSeason', context).val(('AirsBeforeSeasonNumber' in item) ? item.AirsBeforeSeasonNumber : "");
        $('#txtAirsAfterSeason', context).val(('AirsAfterSeasonNumber' in item) ? item.AirsAfterSeasonNumber : "");
        $('#txtAirsBeforeEpisode', context).val(('AirsBeforeEpisodeNumber' in item) ? item.AirsBeforeEpisodeNumber : "");

        $('#txtAlbum', context).val(item.Album || "");

        $('#txtAlbumArtist', context).val((item.AlbumArtists || []).map(function (a) {

            return a.Name;

        }).join(';'));

        $('#selectDisplayOrder', context).val(item.DisplayOrder);

        $('#txtArtist', context).val((item.ArtistItems || []).map(function (a) {

            return a.Name;

        }).join(';'));

        var date;

        if (item.DateCreated) {
            try {
                date = datetime.parseISO8601Date(item.DateCreated, true);

                $('#txtDateAdded', context).val(date.toISOString().slice(0, 10));
            } catch (e) {
                $('#txtDateAdded', context).val('');
            }
        } else {
            $('#txtDateAdded', context).val('');
        }

        if (item.PremiereDate) {
            try {
                date = datetime.parseISO8601Date(item.PremiereDate, true);

                $('#txtPremiereDate', context).val(date.toISOString().slice(0, 10));
            } catch (e) {
                $('#txtPremiereDate', context).val('');
            }
        } else {
            $('#txtPremiereDate', context).val('');
        }

        if (item.EndDate) {
            try {
                date = datetime.parseISO8601Date(item.EndDate, true);

                $('#txtEndDate', context).val(date.toISOString().slice(0, 10));
            } catch (e) {
                $('#txtEndDate', context).val('');
            }
        } else {
            $('#txtEndDate', context).val('');
        }

        $('#txtProductionYear', context).val(item.ProductionYear || "");

        $('#txtAirTime', context).val(item.AirTime || '');

        var placeofBirth = item.ProductionLocations && item.ProductionLocations.length ? item.ProductionLocations[0] : '';
        $('#txtPlaceOfBirth', context).val(placeofBirth);

        $('#txtOriginalAspectRatio', context).val(item.AspectRatio || "");

        $('#selectLanguage', context).val(item.PreferredMetadataLanguage || "");
        $('#selectCountry', context).val(item.PreferredMetadataCountryCode || "");

        if (item.RunTimeTicks) {

            var minutes = item.RunTimeTicks / 600000000;

            $('#txtSeriesRuntime', context).val(Math.round(minutes));
        } else {
            $('#txtSeriesRuntime', context).val("");
        }
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
            html += '<div class="listItem">';

            html += '<button type="button" is="emby-button" data-index="' + i + '" class="fab autoSize mini"><i class="md-icon">live_tv</i></button>';

            html += '<div class="listItemBody">';

            html += '<div class="textValue">';
            html += items[i];
            html += '</div>';

            html += '</div>';

            html += '<button type="button" is="paper-icon-button-light" data-index="' + i + '" class="btnRemoveFromEditorList autoSize"><i class="md-icon">delete</i></button>';

            html += '</div>';
        }

        list.innerHTML = html;
    }

    function populatePeople(context, people) {

        var lastType = '';
        var html = '';

        var elem = context.querySelector('#peopleList');

        for (var i = 0, length = people.length; i < length; i++) {

            var person = people[i];

            html += '<div class="listItem">';

            html += '<button type="button" is="emby-button" data-index="' + i + '" class="btnEditPerson fab autoSize mini"><i class="md-icon">person</i></button>';

            html += '<div class="listItemBody">';
            html += '<a class="btnEditPerson clearLink" href="#" data-index="' + i + '">';

            html += '<div class="textValue">';
            html += (person.Name || '');
            html += '</div>';

            if (person.Role && person.Role != lastType) {
                html += '<div class="secondary">' + (person.Role) + '</div>';
            }

            html += '</a>';
            html += '</div>';

            html += '<button type="button" is="paper-icon-button-light" data-index="' + i + '" class="btnDeletePerson autoSize"><i class="md-icon">delete</i></button>';

            html += '</div>';
        }

        elem.innerHTML = html;

        $('.btnDeletePerson', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));
            currentItem.People.splice(index, 1);

            populatePeople(context, currentItem.People);
        });

        $('.btnEditPerson', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));

            editPerson(context, currentItem.People[index], index);
        });
    }

    function generateSliders(fields, currentFields) {

        var html = '';
        for (var i = 0; i < fields.length; i++) {

            var field = fields[i];
            var name = field.name;
            var value = field.value || field.name;
            var checkedHtml = currentFields.indexOf(value) == -1 ? ' checked' : '';
            html += '<label>';
            html += '<input type="checkbox" is="emby-checkbox" class="selectLockedField" data-value="' + value + '"' + checkedHtml + '/>';
            html += '<span>' + name + '</span>';
            html += '</label>';
        }
        return html;
    }

    function populateInternetProviderSettings(context, item, lockedFields) {
        var container = $('.providerSettingsContainer', context);
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

    function reload(context, itemId) {

        Dashboard.showLoadingMsg();

        Promise.all([getItem(itemId), getEditorConfig(itemId)]).then(function (responses) {

            var item = responses[0];
            metadataEditorInfo = responses[1];

            currentItem = item;

            var languages = metadataEditorInfo.Cultures;
            var countries = metadataEditorInfo.Countries;

            renderContentTypeOptions(context, metadataEditorInfo);

            loadExternalIds(context, item, metadataEditorInfo.ExternalIdInfos);

            populateLanguages(context.querySelector('#selectLanguage'), languages);
            populateCountries(context.querySelector('#selectCountry'), countries);

            LibraryBrowser.renderName(item, $('.itemName', context), true);

            setFieldVisibilities(context, item);
            fillItemInfo(context, item, metadataEditorInfo.ParentalRatingOptions);

            if (item.MediaType == 'Photo') {
                $('#btnEditImages', context).hide();
            } else {
                $('#btnEditImages', context).show();
            }

            if (item.MediaType == "Video" && item.Type != "Episode") {
                $('#fldShortOverview', context).show();
            } else {
                $('#fldShortOverview', context).hide();
            }

            if (item.MediaType == "Video" && item.Type != "Episode") {
                $('#fldTagline', context).show();
            } else {
                $('#fldTagline', context).hide();
            }

            Dashboard.hideLoadingMsg();
        });
    }

    return {
        show: function (itemId) {
            return new Promise(function (resolve, reject) {

                Dashboard.showLoadingMsg();

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/metadataeditor/metadataeditor.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = dialogHelper.createDialog({
                        removeOnClose: true,
                        size: 'medium'
                    });

                    dlg.classList.add('ui-body-b');
                    dlg.classList.add('background-theme-b');

                    dlg.classList.add('formDialog');

                    var html = '';

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;
                    document.body.appendChild(dlg);

                    dialogHelper.open(dlg);

                    dlg.addEventListener('close', function () {
                        unbindItemChanged(dlg);
                        resolve();
                    });

                    currentContext = dlg;

                    init(dlg);

                    reload(dlg, itemId);
                }

                xhr.send();
            });
        },

        embed: function (elem, itemId) {
            return new Promise(function (resolve, reject) {

                Dashboard.showLoadingMsg();

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/metadataeditor/metadataeditor.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;

                    elem.innerHTML = Globalize.translateDocument(template);

                    elem.querySelector('.btnCancel').classList.add('hide');

                    currentContext = elem;

                    init(elem);
                    reload(elem, itemId);
                }

                xhr.send();
            });
        }
    };
});