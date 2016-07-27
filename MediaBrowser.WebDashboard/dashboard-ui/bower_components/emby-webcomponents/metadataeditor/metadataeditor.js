define(['itemHelper', 'dom', 'layoutManager', 'dialogHelper', 'datetime', 'loading', 'focusManager', 'connectionManager', 'globalize', 'require', 'emby-checkbox', 'emby-input', 'emby-select', 'listViewStyle', 'emby-textarea', 'emby-button', 'paper-icon-button-light', 'css!./../formdialog'], function (itemHelper, dom, layoutManager, dialogHelper, datetime, loading, focusManager, connectionManager, globalize, require) {

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
                toast(globalize.translate('sharedcomponents#MessageItemSaved'));
            });

            loading.hide();
            closeDialog(true);
        }

        var apiClient = getApiClient();

        apiClient.updateItem(item).then(function () {

            var newContentType = form.querySelector('#selectContentType').value || '';

            if ((metadataEditorInfo.ContentType || '') != newContentType) {

                apiClient.ajax({

                    url: apiClient.getUrl('Items/' + item.Id + '/ContentType', {
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
        var checkedItems = form.querySelectorAll('.chkAirDay:checked') || [];
        return Array.prototype.map.call(checkedItems, function (c) {
            return c.getAttribute('data-day');
        });
    }

    function getAlbumArtists(form) {

        return form.querySelector('#txtAlbumArtist').value.trim().split(';').filter(function (s) {

            return s.length > 0;

        }).map(function (a) {

            return {
                Name: a
            };
        });
    }

    function getArtists(form) {

        return form.querySelector('#txtArtist').value.trim().split(';').filter(function (s) {

            return s.length > 0;

        }).map(function (a) {

            return {
                Name: a
            };
        });
    }

    function getDateFromForm(form, element, property) {

        var val = form.querySelector(element).value;

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

    function onSubmit(e) {

        loading.show();

        var form = this;

        try {
            var item = {
                Id: currentItem.Id,
                Name: form.querySelector('#txtName').value,
                OriginalTitle: form.querySelector('#txtOriginalName').value,
                ForcedSortName: form.querySelector('#txtSortName').value,
                DisplayMediaType: form.querySelector('#txtDisplayMediaType').value,
                CommunityRating: form.querySelector('#txtCommunityRating').value,
                VoteCount: form.querySelector('#txtCommunityVoteCount').value,
                HomePageUrl: form.querySelector('#txtHomePageUrl').value,
                Budget: form.querySelector('#txtBudget').value,
                Revenue: form.querySelector('#txtRevenue').value,
                CriticRating: form.querySelector('#txtCriticRating').value,
                CriticRatingSummary: form.querySelector('#txtCriticRatingSummary').value,
                IndexNumber: form.querySelector('#txtIndexNumber').value || null,
                AbsoluteEpisodeNumber: form.querySelector('#txtAbsoluteEpisodeNumber').value,
                DvdEpisodeNumber: form.querySelector('#txtDvdEpisodeNumber').value,
                DvdSeasonNumber: form.querySelector('#txtDvdSeasonNumber').value,
                AirsBeforeSeasonNumber: form.querySelector('#txtAirsBeforeSeason').value,
                AirsAfterSeasonNumber: form.querySelector('#txtAirsAfterSeason').value,
                AirsBeforeEpisodeNumber: form.querySelector('#txtAirsBeforeEpisode').value,
                ParentIndexNumber: form.querySelector('#txtParentIndexNumber').value || null,
                DisplayOrder: form.querySelector('#selectDisplayOrder').value,
                Players: form.querySelector('#txtPlayers').value,
                Album: form.querySelector('#txtAlbum').value,
                AlbumArtist: getAlbumArtists(form),
                ArtistItems: getArtists(form),
                Metascore: form.querySelector('#txtMetascore').value,
                AwardSummary: form.querySelector('#txtAwardSummary').value,
                Overview: form.querySelector('#txtOverview').value,
                ShortOverview: form.querySelector('#txtShortOverview').value,
                Status: form.querySelector('#selectStatus').value,
                AirDays: getSelectedAirDays(form),
                AirTime: form.querySelector('#txtAirTime').value,
                Genres: editableListViewValues(form.querySelector("#listGenres")),
                ProductionLocations: editableListViewValues(form.querySelector("#listCountries")),
                Tags: editableListViewValues(form.querySelector("#listTags")),
                Keywords: editableListViewValues(form.querySelector("#listKeywords")),
                Studios: editableListViewValues(form.querySelector("#listStudios")).map(function (element) { return { Name: element }; }),

                PremiereDate: getDateFromForm(form, '#txtPremiereDate', 'PremiereDate'),
                DateCreated: getDateFromForm(form, '#txtDateAdded', 'DateCreated'),
                EndDate: getDateFromForm(form, '#txtEndDate', 'EndDate'),
                ProductionYear: form.querySelector('#txtProductionYear').value,
                AspectRatio: form.querySelector('#txtOriginalAspectRatio').value,
                Video3DFormat: form.querySelector('#select3dFormat').value,

                OfficialRating: form.querySelector('#selectOfficialRating').value,
                CustomRating: form.querySelector('#selectCustomRating').value,
                People: currentItem.People,
                LockData: form.querySelector("#chkLockData").checked,
                LockedFields: Array.prototype.filter.call(form.querySelectorAll('.selectLockedField'), function (c) {
                    return !c.checked;
                }).map(function (c) {
                    return c.getAttribute('data-value');
                })
            };

            item.ProviderIds = Object.assign({}, currentItem.ProviderIds);

            var idElements = form.querySelectorAll('.txtExternalId');
            Array.prototype.map.call(idElements, function (idElem) {
                var providerKey = idElem.getAttribute('data-providerkey');
                item.ProviderIds[providerKey] = idElem.value;
            });

            item.PreferredMetadataLanguage = form.querySelector('#selectLanguage').value;
            item.PreferredMetadataCountryCode = form.querySelector('#selectCountry').value;

            if (currentItem.Type == "Person") {

                var placeOfBirth = form.querySelector('#txtPlaceOfBirth').value;

                item.ProductionLocations = placeOfBirth ? [placeOfBirth] : [];
            }

            if (currentItem.Type == "Series") {

                // 600000000
                var seriesRuntime = form.querySelector('#txtSeriesRuntime').value;
                item.RunTimeTicks = seriesRuntime ? (seriesRuntime * 600000000) : null;
            }

            var tagline = form.querySelector('#txtTagline').value;
            item.Taglines = tagline ? [tagline] : [];

            submitUpdatedItem(form, item);

        } catch (err) {
            alert(err);
        }

        e.preventDefault();
        e.stopPropagation();

        // Disable default form submission
        return false;
    }

    function editableListViewValues(list) {
        return Array.prototype.map.call(list.querySelectorAll('.textValue'), function (el) { return el.textContent; });
    }

    function addElementToEditableListview(source, sortCallback) {
        require(['prompt'], function (prompt) {

            prompt({
                label: 'Value:'
            }).then(function (text) {
                var list = dom.parentWithClass(source, 'editableListviewContainer').querySelector('.paperList');
                var items = editableListViewValues(list);
                items.push(text);
                populateListView(list, items, sortCallback);
            });
        });
    }

    function removeElementFromListview(source) {
        var el = dom.parentWithClass(source, 'listItem');
        el.parentNode.removeChild(el);
    }

    function editPerson(context, person, index) {

        require(['personEditor'], function (personEditor) {

            personEditor.show(person).then(function (updatedPerson) {

                var isNew = index == -1;

                if (isNew) {
                    currentItem.People.push(updatedPerson);
                }

                populatePeople(context, currentItem.People);
            });
        });
    }

    function showMoreMenu(context, button, user) {

        require(['itemContextMenu'], function (itemContextMenu) {
            itemContextMenu.show({

                item: currentItem,
                positionTo: button,
                edit: false,
                editImages: true,
                editSubtitles: true,
                sync: false,
                share: false

            }).then(function (result) {

                if (result.deleted) {
                    Emby.Page.goHome();

                } else if (result.updated) {
                    reload(context, currentItem.Id, currentItem.ServerId);
                }
            });
        });
    }

    function onWebSocketMessageReceived(e, data) {

        var msg = data;

        if (msg.MessageType === "LibraryChanged") {

            if (msg.Data.ItemsUpdated.indexOf(currentItem.Id) != -1) {

                console.log('Item updated - reloading metadata');
                reload(currentContext, currentItem.Id, currentItem.ServerId);
            }
        }
    }

    function bindItemChanged(context, apiClient) {

        Events.on(apiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    function unbindItemChanged(context, apiClient) {

        Events.off(apiClient, "websocketmessage", onWebSocketMessageReceived);
    }

    function onEditorClick(e) {

        var btnRemoveFromEditorList = dom.parentWithClass(e.target, 'btnRemoveFromEditorList');
        if (btnRemoveFromEditorList) {
            removeElementFromListview(btnRemoveFromEditorList);
            return;
        }

        var btnAddTextItem = dom.parentWithClass(e.target, 'btnAddTextItem');
        if (btnAddTextItem) {
            addElementToEditableListview(btnAddTextItem);
        }
    }

    function getApiClient() {
        return connectionManager.getApiClient(currentItem.ServerId);
    }

    function init(context, apiClient) {

        context.querySelector('.btnCancel').addEventListener('click', function () {

            closeDialog(false);
        });

        context.querySelector('.btnMore').addEventListener('click', function (e) {

            getApiClient().getCurrentUser().then(function (user) {
                showMoreMenu(context, e.target, user);
            });

        });

        context.querySelector('.btnHeaderSave').addEventListener('click', function (e) {

            context.querySelector('.btnSave').click();
        });

        context.querySelector('#chkLockData').addEventListener('click', function (e) {

            if (!e.target.checked) {
                showElement('.providerSettingsContainer');
            } else {
                hideElement('.providerSettingsContainer');
            }
        });

        context.removeEventListener('click', onEditorClick);
        context.addEventListener('click', onEditorClick);

        var form = context.querySelector('form');
        form.removeEventListener('submit', onSubmit);
        form.addEventListener('submit', onSubmit);

        context.querySelector("#btnAddPerson").addEventListener('click', function (event, data) {

            editPerson(context, {}, -1);
        });

        context.querySelector('#peopleList').addEventListener('click', function(e) {

            var btnDeletePerson = dom.parentWithClass(e.target, 'btnDeletePerson');
            if (btnDeletePerson) {
                var index = parseInt(btnDeletePerson.getAttribute('data-index'));
                currentItem.People.splice(index, 1);
                populatePeople(context, currentItem.People);
            }

            var btnEditPerson = dom.parentWithClass(e.target, 'btnEditPerson');
            if (btnEditPerson) {
                var index = parseInt(btnEditPerson.getAttribute('data-index'));
                editPerson(context, currentItem.People[index], index);
            }
        });

        // For now this is only supported in dialog mode because we have a way of knowing when it closes
        if (isDialog()) {
            bindItemChanged(context, apiClient);
        }
    }

    function getItem(itemId, serverId) {

        var apiClient = connectionManager.getApiClient(serverId);

        if (itemId) {
            return apiClient.getItem(apiClient.getCurrentUserId(), itemId);
        }

        return apiClient.getRootFolder(apiClient.getCurrentUserId());
    }

    function getEditorConfig(itemId, serverId) {

        var apiClient = connectionManager.getApiClient(serverId);

        if (itemId) {
            return apiClient.getJSON(apiClient.getUrl('Items/' + itemId + '/MetadataEditor'));
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

        if (!metadataInfo.ContentTypeOptions.length) {
            hideElement('#fldContentType', context);
        } else {
            showElement('#fldContentType', context);
        }

        var html = metadataInfo.ContentTypeOptions.map(function (i) {


            return '<option value="' + i.Value + '">' + i.Name + '</option>';

        }).join('');

        var selectEl = context.querySelector('#selectContentType');
        selectEl.innerHTML = html;
        selectEl.value = metadataInfo.ContentType || '';
    }

    function onExternalIdChange() {

        var formatString = this.getAttribute('data-formatstring');
        var buttonClass = this.getAttribute('data-buttonclass');

        if (this.value) {
            document.querySelector('.' + buttonClass).setAttribute('href', formatString.replace('{0}', this.value));
        } else {
            document.querySelector('.' + buttonClass).setAttribute('href', '#');
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

            var labelText = globalize.translate('sharedcomponents#LabelDynamicExternalId').replace('{0}', idInfo.Name);

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

        var elem = context.querySelector('.externalIds', context);
        elem.innerHTML = html;

        var extIdEls = elem.querySelector('.txtExternalId') || [];
        Array.prototype.forEach.call(extIdEls, function (el) {
            el.addEventListener('change', onExternalIdChange.bind(el));
            el.dispatchEvent(new Event('change'));
        });
    }

    // Function to hide the element by selector or raw element
    // Selector can be an element or a selector string
    // Context is optional and restricts the querySelector to the context
    function hideElement(selector, context, multiple) {
        context = context || document;
        if (typeof selector == 'string') {

            var elements = multiple ? context.querySelectorAll(selector) : [context.querySelector(selector)];

            Array.prototype.forEach.call(elements, function (el) {
                if (el) {
                    el.classList.add('hide');
                }
            });
        } else {
            selector.classList.add('hide');
        }
    }

    // Function to show the element by selector or raw element
    // Selector can be an element or a selector string
    // Context is optional and restricts the querySelector to the context
    function showElement(selector, context, multiple) {
        context = context || document;
        if (typeof selector == 'string') {

            var elements = multiple ? context.querySelectorAll(selector) : [context.querySelector(selector)];

            Array.prototype.forEach.call(elements, function (el) {
                if (el) {
                    el.classList.remove('hide');
                }
            });
        } else {
            selector.classList.remove('hide');
        }
    }

    function setFieldVisibilities(context, item) {
        if (item.Path && item.LocationType != 'Remote') {
            showElement('#fldPath', context);
        } else {
            hideElement('#fldPath', context);
        }

        if (item.Type == "Series" || item.Type == "Movie" || item.Type == "Trailer") {
            showElement('#fldOriginalName', context);
        } else {
            hideElement('#fldOriginalName', context);
        }

        if (item.Type == "Series") {
            showElement('#fldSeriesRuntime', context);
        } else {
            hideElement('#fldSeriesRuntime', context);
        }

        if (item.Type == "Series" || item.Type == "Person") {
            showElement('#fldEndDate', context);
        } else {
            hideElement('#fldEndDate', context);
        }

        if (item.Type == "Movie" || item.MediaType == "Game" || item.MediaType == "Trailer" || item.Type == "MusicVideo") {
            showElement('#fldBudget', context);
            showElement('#fldRevenue', context);
        } else {
            hideElement('#fldBudget', context);
            hideElement('#fldRevenue', context);
        }

        if (item.Type == "MusicAlbum") {
            showElement('#albumAssociationMessage', context);
        } else {
            hideElement('#albumAssociationMessage', context)
        }

        if (item.MediaType == "Game") {
            showElement('#fldPlayers', context);
        } else {
            hideElement('#fldPlayers', context);
        }

        if (item.Type == "Movie" || item.Type == "Trailer") {
            showElement('#fldCriticRating', context);
            showElement('#fldCriticRatingSummary', context);
        } else {
            hideElement('#fldCriticRating', context);
            hideElement('#fldCriticRatingSummary', context);
        }

        if (item.Type == "Movie") {
            showElement('#fldAwardSummary', context);
        } else {
            hideElement('#fldAwardSummary', context);
        }

        if (item.Type == "Movie" || item.Type == "Trailer") {
            showElement('#fldMetascore', context);
        } else {
            hideElement('#fldMetascore', context);
        }

        if (item.Type == "Series") {
            showElement('#fldStatus', context);
            showElement('#fldAirDays', context);
            showElement('#fldAirTime', context);
        } else {
            hideElement('#fldStatus', context);
            hideElement('#fldAirDays', context);
            hideElement('#fldAirTime', context);
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            showElement('#fld3dFormat', context);
        } else {
            hideElement('#fld3dFormat', context);
        }

        if (item.Type == "Audio") {
            showElement('#fldAlbumArtist', context);
        } else {
            hideElement('#fldAlbumArtist', context);
        }

        if (item.Type == "Audio" || item.Type == "MusicVideo") {
            showElement('#fldArtist', context);
            showElement('#fldAlbum', context);
        } else {
            hideElement('#fldArtist', context);
            hideElement('#fldAlbum', context);
        }

        if (item.Type == "Episode") {
            showElement('#collapsibleDvdEpisodeInfo', context);
        } else {
            hideElement('#collapsibleDvdEpisodeInfo', context);
        }

        if (item.Type == "Episode" && item.ParentIndexNumber == 0) {
            showElement('#collapsibleSpecialEpisodeInfo', context);
        } else {
            hideElement('#collapsibleSpecialEpisodeInfo', context);
        }

        if (item.Type == "Person" || item.Type == "Genre" || item.Type == "Studio" || item.Type == "GameGenre" || item.Type == "MusicGenre" || item.Type == "TvChannel") {
            hideElement('#fldCommunityRating', context);
            hideElement('#fldCommunityVoteCount', context);
            hideElement('#genresCollapsible', context);
            hideElement('#peopleCollapsible', context);
            hideElement('#studiosCollapsible', context);

            if (item.Type == "TvChannel") {
                showElement('#fldOfficialRating', context);
            } else {
                hideElement('#fldOfficialRating', context);
            }
            hideElement('#fldCustomRating', context);
        } else {
            showElement('#fldCommunityRating', context);
            showElement('#fldCommunityVoteCount', context);
            showElement('#genresCollapsible', context);
            showElement('#peopleCollapsible', context);
            showElement('#studiosCollapsible', context);
            showElement('#fldOfficialRating', context);
            showElement('#fldCustomRating', context);
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "MusicArtist") {
            showElement('#countriesCollapsible', context);
        } else {
            hideElement('#countriesCollapsible', context);
        }

        if (item.Type == "TvChannel") {
            hideElement('#tagsCollapsible', context);
            hideElement('#metadataSettingsCollapsible', context);
            hideElement('#fldPremiereDate', context);
            hideElement('#fldDateAdded', context);
            hideElement('#fldYear', context);
        } else {
            showElement('#tagsCollapsible', context);
            showElement('#metadataSettingsCollapsible', context);
            showElement('#fldPremiereDate', context);
            showElement('#fldDateAdded', context);
            showElement('#fldYear', context);
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            showElement('#keywordsCollapsible', context);
        } else {
            hideElement('#keywordsCollapsible', context);
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            showElement('#fldSourceType', context);
        } else {
            hideElement('#fldSourceType', context);
        }

        if (item.Type == "Person") {
            //todo
            context.querySelector('#txtProductionYear').label(globalize.translate('sharedcomponents#LabelBirthYear'));
            context.querySelector("#txtPremiereDate").label(globalize.translate('sharedcomponents#LabelBirthDate'));
            context.querySelector("#txtEndDate").label(globalize.translate('sharedcomponents#LabelDeathDate'));
            showElement('#fldPlaceOfBirth');
        } else {
            context.querySelector('#txtProductionYear').label(globalize.translate('sharedcomponents#LabelYear'));
            context.querySelector("#txtPremiereDate").label(globalize.translate('sharedcomponents#LabelReleaseDate'));
            context.querySelector("#txtEndDate").label(globalize.translate('sharedcomponents#LabelEndDate'));
            hideElement('#fldPlaceOfBirth');
        }

        if (item.MediaType == "Video" && item.Type != "TvChannel") {
            showElement('#fldOriginalAspectRatio');
        } else {
            hideElement('#fldOriginalAspectRatio');
        }

        if (item.Type == "Audio" || item.Type == "Episode" || item.Type == "Season") {
            showElement('#fldIndexNumber');

            if (item.Type == "Episode") {
                context.querySelector('#txtIndexNumber').label(globalize.translate('sharedcomponents#LabelEpisodeNumber'));
            } else if (item.Type == "Season") {
                context.querySelector('#txtIndexNumber').label(globalize.translate('sharedcomponents#LabelSeasonNumber'));
            } else if (item.Type == "Audio") {
                context.querySelector('#txtIndexNumber').label(globalize.translate('sharedcomponents#LabelTrackNumber'));
            } else {
                context.querySelector('#txtIndexNumber').label(globalize.translate('sharedcomponents#LabelNumber'));
            }
        } else {
            hideElement('#fldIndexNumber');
        }

        if (item.Type == "Audio" || item.Type == "Episode") {
            showElement('#fldParentIndexNumber');

            if (item.Type == "Episode") {
                context.querySelector('#txtParentIndexNumber').label(globalize.translate('LabelSeasonNumber'));
            } else if (item.Type == "Audio") {
                context.querySelector('#txtParentIndexNumber').label(globalize.translate('LabelDiscNumber'));
            } else {
                context.querySelector('#txtParentIndexNumber').label(globalize.translate('LabelParentNumber'));
            }
        } else {
            hideElement('#fldParentIndexNumber', context);
        }

        if (item.Type == "BoxSet") {
            showElement('#fldDisplayOrder', context);

            context.querySelector('#selectDisplayOrder').innerHTML = '<option value="SortName">' + globalize.translate('sharedcomponents#SortName') + '</option><option value="PremiereDate">' + globalize.translate('sharedcomponents#ReleaseDate') + '</option>';
        } else {
            context.querySelector('#selectDisplayOrder').innerHTML = '';
            hideElement('#fldDisplayOrder', context);
        }

        var displaySettingFields = context.querySelectorAll('.fldDisplaySetting');
        var hiddenDisplaySettingFields = Array.prototype.filter.call(displaySettingFields, function (field) {
            return field.style.display != 'none';

        });
        if (hiddenDisplaySettingFields.length) {
            showElement('#collapsibleDisplaySettings', context);
        } else {
            hideElement('#collapsibleDisplaySettings', context);
        }
    }

    function fillItemInfo(context, item, parentalRatingOptions) {

        var select = context.querySelector('#selectOfficialRating');

        populateRatings(parentalRatingOptions, select, item.OfficialRating);

        select.value = item.OfficialRating || "";

        select = context.querySelector('#selectCustomRating');

        populateRatings(parentalRatingOptions, select, item.CustomRating);

        select.value = item.CustomRating || "";

        var selectStatus = context.querySelector('#selectStatus');
        populateStatus(selectStatus);
        selectStatus.value = item.Status || "";

        context.querySelector('#select3dFormat', context).value = item.Video3DFormat || "";

        Array.prototype.forEach.call(context.querySelectorAll('.chkAirDay', context), function (el) {
            el.checked = (item.AirDays || []).indexOf(el.getAttribute('data-day')) != -1;
        });

        populateListView(context.querySelector('#listCountries'), item.ProductionLocations || []);
        populateListView(context.querySelector('#listGenres'), item.Genres);
        populatePeople(context, item.People || []);

        populateListView(context.querySelector('#listStudios'), (item.Studios || []).map(function (element) { return element.Name || ''; }));

        populateListView(context.querySelector('#listTags'), item.Tags);
        populateListView(context.querySelector('#listKeywords'), item.Keywords);

        var lockData = (item.LockData || false);
        var chkLockData = context.querySelector("#chkLockData");
        chkLockData.checked = lockData;
        if (chkLockData.checked) {
            hideElement('.providerSettingsContainer', context)
        } else {
            showElement('.providerSettingsContainer', context)
        }
        populateInternetProviderSettings(context, item, item.LockedFields);

        context.querySelector('#txtPath').value = item.Path || '';
        context.querySelector('#txtName').value = item.Name || "";
        context.querySelector('#txtOriginalName').value = item.OriginalTitle || "";
        context.querySelector('#txtOverview').value = item.Overview || '';
        context.querySelector('#txtShortOverview').value = item.ShortOverview || "";
        context.querySelector('#txtTagline').value = (item.Taglines && item.Taglines.length ? item.Taglines[0] : '');
        context.querySelector('#txtSortName').value = item.ForcedSortName || "";
        context.querySelector('#txtDisplayMediaType').value = item.DisplayMediaType || "";
        context.querySelector('#txtCommunityRating').value = item.CommunityRating || "";
        context.querySelector('#txtCommunityVoteCount').value = item.VoteCount || "";
        context.querySelector('#txtHomePageUrl').value = item.HomePageUrl || "";

        context.querySelector('#txtAwardSummary').value = item.AwardSummary || "";
        context.querySelector('#txtMetascore').value = item.Metascore || "";

        context.querySelector('#txtBudget').value = item.Budget || "";
        context.querySelector('#txtRevenue').value = item.Revenue || "";

        context.querySelector('#txtCriticRating').value = item.CriticRating || "";
        context.querySelector('#txtCriticRatingSummary').value = item.CriticRatingSummary || "";

        context.querySelector('#txtIndexNumber').value = ('IndexNumber' in item) ? item.IndexNumber : "";
        context.querySelector('#txtParentIndexNumber').value = ('ParentIndexNumber' in item) ? item.ParentIndexNumber : "";
        context.querySelector('#txtPlayers').value = item.Players || "";

        context.querySelector('#txtAbsoluteEpisodeNumber').value = ('AbsoluteEpisodeNumber' in item) ? item.AbsoluteEpisodeNumber : "";
        context.querySelector('#txtDvdEpisodeNumber').value = ('DvdEpisodeNumber' in item) ? item.DvdEpisodeNumber : "";
        context.querySelector('#txtDvdSeasonNumber').value = ('DvdSeasonNumber' in item) ? item.DvdSeasonNumber : "";
        context.querySelector('#txtAirsBeforeSeason').value = ('AirsBeforeSeasonNumber' in item) ? item.AirsBeforeSeasonNumber : "";
        context.querySelector('#txtAirsAfterSeason').value = ('AirsAfterSeasonNumber' in item) ? item.AirsAfterSeasonNumber : "";
        context.querySelector('#txtAirsBeforeEpisode').value = ('AirsBeforeEpisodeNumber' in item) ? item.AirsBeforeEpisodeNumber : "";

        context.querySelector('#txtAlbum').value = item.Album || "";

        context.querySelector('#txtAlbumArtist').value = (item.AlbumArtists || []).map(function (a) {
            return a.Name;
        }).join(';');

        context.querySelector('#selectDisplayOrder').value = item.DisplayOrder;

        context.querySelector('#txtArtist').value = (item.ArtistItems || []).map(function (a) {
            return a.Name;
        }).join(';');

        var date;

        if (item.DateCreated) {
            try {
                date = datetime.parseISO8601Date(item.DateCreated, true);

                context.querySelector('#txtDateAdded').value = date.toISOString().slice(0, 10);
            } catch (e) {
                context.querySelector('#txtDateAdded').value = '';
            }
        } else {
            context.querySelector('#txtDateAdded').value = '';
        }

        if (item.PremiereDate) {
            try {
                date = datetime.parseISO8601Date(item.PremiereDate, true);

                context.querySelector('#txtPremiereDate').value = date.toISOString().slice(0, 10);
            } catch (e) {
                context.querySelector('#txtPremiereDate').value = '';
            }
        } else {
            context.querySelector('#txtPremiereDate').value = '';
        }

        if (item.EndDate) {
            try {
                date = datetime.parseISO8601Date(item.EndDate, true);

                context.querySelector('#txtEndDate').value = date.toISOString().slice(0, 10);
            } catch (e) {
                context.querySelector('#txtEndDate').value = '';
            }
        } else {
            context.querySelector('#txtEndDate').value = '';
        }

        context.querySelector('#txtProductionYear').value = item.ProductionYear || "";

        context.querySelector('#txtAirTime').value = item.AirTime || '';

        var placeofBirth = item.ProductionLocations && item.ProductionLocations.length ? item.ProductionLocations[0] : '';
        context.querySelector('#txtPlaceOfBirth').value = placeofBirth;

        context.querySelector('#txtOriginalAspectRatio').value = item.AspectRatio || "";

        context.querySelector('#selectLanguage').value = item.PreferredMetadataLanguage || "";
        context.querySelector('#selectCountry').value = item.PreferredMetadataCountryCode || "";

        if (item.RunTimeTicks) {

            var minutes = item.RunTimeTicks / 600000000;

            context.querySelector('#txtSeriesRuntime').value = Math.round(minutes);
        } else {
            context.querySelector('#txtSeriesRuntime', context).value = "";
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

        select.innerHTML = html;
    }

    function populateStatus(select) {
        var html = "";

        html += "<option value=''></option>";
        html += "<option value='Continuing'>" + globalize.translate('sharedcomponents#Continuing') + "</option>";
        html += "<option value='Ended'>" + globalize.translate('sharedcomponents#Ended') + "</option>";
        select.innerHTML = html;
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

            html += '<i class="md-icon listItemIcon" style="background-color:#333;">live_tv</i>';

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

            html += '<i class="md-icon listItemIcon" style="background-color:#333;">person</i>';

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
        var container = context.querySelector('.providerSettingsContainer');
        lockedFields = lockedFields || new Array();

        var metadatafields = [
            { name: globalize.translate('sharedcomponents#Name'), value: "Name" },
            { name: globalize.translate('sharedcomponents#Overview'), value: "Overview" },
            { name: globalize.translate('sharedcomponents#Genres'), value: "Genres" },
            { name: globalize.translate('sharedcomponents#ParentalRating'), value: "OfficialRating" },
            { name: globalize.translate('sharedcomponents#People'), value: "Cast" }
        ];

        if (item.Type == "Person") {
            metadatafields.push({ name: globalize.translate('sharedcomponents#BirthLocation'), value: "ProductionLocations" });
        } else {
            metadatafields.push({ name: globalize.translate('sharedcomponents#ProductionLocations'), value: "ProductionLocations" });
        }

        if (item.Type == "Series") {
            metadatafields.push({ name: globalize.translate('Runtime'), value: "Runtime" });
        }

        metadatafields.push({ name: globalize.translate('sharedcomponents#Studios'), value: "Studios" });
        metadatafields.push({ name: globalize.translate('sharedcomponents#Tags'), value: "Tags" });
        metadatafields.push({ name: globalize.translate('sharedcomponents#Keywords'), value: "Keywords" });
        metadatafields.push({ name: globalize.translate('sharedcomponents#Images'), value: "Images" });
        metadatafields.push({ name: globalize.translate('sharedcomponents#Backdrops'), value: "Backdrops" });

        if (item.Type == "Game") {
            metadatafields.push({ name: globalize.translate('sharedcomponents#Screenshots'), value: "Screenshots" });
        }

        var html = '';

        html += "<h1>" + globalize.translate('sharedcomponents#HeaderEnabledFields') + "</h1>";
        html += "<p>" + globalize.translate('sharedcomponents#HeaderEnabledFieldsHelp') + "</p>";
        html += generateSliders(metadatafields, lockedFields);
        container.innerHTML = html;
    }

    function reload(context, itemId, serverId) {

        loading.show();

        Promise.all([getItem(itemId, serverId), getEditorConfig(itemId, serverId)]).then(function (responses) {

            var item = responses[0];
            metadataEditorInfo = responses[1];

            currentItem = item;

            var languages = metadataEditorInfo.Cultures;
            var countries = metadataEditorInfo.Countries;

            renderContentTypeOptions(context, metadataEditorInfo);

            loadExternalIds(context, item, metadataEditorInfo.ExternalIdInfos);

            populateLanguages(context.querySelector('#selectLanguage'), languages);
            populateCountries(context.querySelector('#selectCountry'), countries);

            setFieldVisibilities(context, item);
            fillItemInfo(context, item, metadataEditorInfo.ParentalRatingOptions);

            if (item.MediaType == "Video" && item.Type != "Episode") {
                showElement('#fldShortOverview', context);
            } else {
                hideElement('#fldShortOverview', context);
            }

            if (item.MediaType == "Video" && item.Type != "Episode") {
                showElement('#fldTagline', context);
            } else {
                hideElement('#fldTagline', context);
            }

            loading.hide();
        });
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function show(itemId, serverId, resolve, reject) {
        loading.show();

        require(['text!./metadataeditor.template.html'], function (template) {

            var dialogOptions = {
                removeOnClose: true,
                scrollY: false
            };

            if (layoutManager.tv) {
                dialogOptions.size = 'fullscreen';
            } else {
                dialogOptions.size = 'medium';
            }

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.classList.add('ui-body-b');
            dlg.classList.add('background-theme-b');

            dlg.classList.add('formDialog');

            var html = '';

            html += globalize.translateDocument(template, 'sharedcomponents');

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.dialogContent'), false, true);
            }

            dialogHelper.open(dlg);

            dlg.addEventListener('close', function () {
                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.dialogContent'), false, false);
                }

                unbindItemChanged(dlg, connectionManager.getApiClient(serverId));
                resolve();
            });

            currentContext = dlg;

            init(dlg, connectionManager.getApiClient(serverId));

            reload(dlg, itemId, serverId);
        });
    }

    return {
        show: function (itemId, serverId) {
            return new Promise(function (resolve, reject) {
                return show(itemId, serverId, resolve, reject);
            });
        },

        embed: function (elem, itemId, serverId) {
            return new Promise(function (resolve, reject) {

                loading.show();

                require(['text!./metadataeditor.template.html'], function (template) {

                    elem.innerHTML = globalize.translateDocument(template, 'sharedcomponents');

                    elem.querySelector('.btnCancel').classList.add('hide');

                    currentContext = elem;

                    init(elem, connectionManager.getApiClient(serverId));
                    reload(elem, itemId, serverId);

                    focusManager.autoFocus(elem);
                });
            });
        }
    };
});