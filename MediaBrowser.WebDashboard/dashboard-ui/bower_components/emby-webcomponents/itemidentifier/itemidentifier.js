define(['dialogHelper', 'loading', 'cardBuilder', 'connectionManager', 'require', 'globalize', 'scrollHelper', 'layoutManager', 'focusManager', 'emby-input', 'emby-checkbox', 'paper-icon-button-light', 'css!./../formdialog', 'material-icons', 'cardStyle'], function (dialogHelper, loading, cardBuilder, connectionManager, require, globalize, scrollHelper, layoutManager, focusManager) {

    var currentItem;
    var currentItemType;
    var currentServerId;
    var currentResolve;
    var currentReject;
    var hasChanges = false;
    var currentSearchResult;

    function getApiClient() {
        return connectionManager.getApiClient(currentServerId);
    }

    function searchForIdentificationResults(page) {

        var lookupInfo = {
            ProviderIds: {}
        };

        var i, length;
        var identifyField = page.querySelectorAll('.identifyField');
        for (i = 0, length = identifyField.length; i < length; i++) {

            var value = identifyField[i].value;

            if (value) {

                if (identifyField[i].type == 'number') {
                    value = parseInt(value);
                }

                lookupInfo[identifyField[i].getAttribute('data-lookup')] = value;
            }
        }

        var hasId = false;

        var txtLookupId = page.querySelectorAll('.txtLookupId');
        for (i = 0, length = txtLookupId.length; i < length; i++) {

            var value = txtLookupId[i].value;

            if (value) {
                hasId = true;
            }
            lookupInfo.ProviderIds[txtLookupId[i].getAttribute('data-providerkey')] = value;
        }

        if (!hasId && !lookupInfo.Name) {
            require(['toast'], function (toast) {
                toast(globalize.translate('sharedcomponents#PleaseEnterNameOrId'));
            });
            return;
        }

        if (currentItem && currentItem.GameSystem) {
            lookupInfo.GameSystem = currentItem.GameSystem;
        }

        lookupInfo = {
            SearchInfo: lookupInfo,
            IncludeDisabledProviders: true
        };

        loading.show();

        var apiClient = getApiClient();

        apiClient.ajax({
            type: "POST",
            url: apiClient.getUrl("Items/RemoteSearch/" + currentItemType),
            data: JSON.stringify(lookupInfo),
            contentType: "application/json",
            dataType: 'json'

        }).then(function (results) {

            loading.hide();
            showIdentificationSearchResults(page, results);
        });
    }

    function showIdentificationSearchResults(page, results) {

        var identificationSearchResults = page.querySelector('.identificationSearchResults');

        page.querySelector('.popupIdentifyForm').classList.add('hide');
        identificationSearchResults.classList.remove('hide');
        page.querySelector('.identifyOptionsForm').classList.add('hide');
        page.querySelector('.dialogContentInner').classList.remove('centeredContent');

        var html = '';
        var i, length;
        for (i = 0, length = results.length; i < length; i++) {

            var result = results[i];
            html += getSearchResultHtml(result, i);
        }

        var elem = page.querySelector('.identificationSearchResultList');
        elem.innerHTML = html;

        function onSearchImageClick() {
            var index = parseInt(this.getAttribute('data-index'));

            var currentResult = results[index];

            if (currentItem != null) {

                showIdentifyOptions(page, currentResult);
            } else {

                finishFindNewDialog(page, currentResult);
            }
        }

        var searchImages = elem.querySelectorAll('.card');
        for (i = 0, length = searchImages.length; i < length; i++) {

            searchImages[i].addEventListener('click', onSearchImageClick);
        }

        if (layoutManager.tv) {
            focusManager.autoFocus(identificationSearchResults);
        }
    }

    function finishFindNewDialog(dlg, identifyResult) {
        currentSearchResult = identifyResult;
        hasChanges = true;
        loading.hide();

        dialogHelper.close(dlg);
    }

    function showIdentifyOptions(page, identifyResult) {

        var identifyOptionsForm = page.querySelector('.identifyOptionsForm');

        page.querySelector('.popupIdentifyForm').classList.add('hide');
        page.querySelector('.identificationSearchResults').classList.add('hide');
        identifyOptionsForm.classList.remove('hide');
        page.querySelector('#chkIdentifyReplaceImages').checked = true;
        page.querySelector('.dialogContentInner').classList.add('centeredContent');

        currentSearchResult = identifyResult;

        var lines = [];
        lines.push(identifyResult.Name);

        if (identifyResult.ProductionYear) {
            lines.push(identifyResult.ProductionYear);
        }

        if (identifyResult.GameSystem) {
            lines.push(identifyResult.GameSystem);
        }

        var resultHtml = lines.join('<br/>');

        if (identifyResult.ImageUrl) {
            var displayUrl = getSearchImageDisplayUrl(identifyResult.ImageUrl, identifyResult.SearchProviderName);

            resultHtml = '<div style="display:flex;align-items:center;"><img src="' + displayUrl + '" style="max-height:240px;" /><div style="margin-left:1em;">' + resultHtml + '</div>';
        }

        page.querySelector('.selectedSearchResult').innerHTML = resultHtml;

        focusManager.focus(identifyOptionsForm.querySelector('.btnSubmit'));
    }

    function getSearchResultHtml(result, index) {

        var html = '';
        var cssClass = "card scalableCard";

        if (currentItemType == "Episode") {
            cssClass += " backdropCard";
        }
        else if (currentItemType == "MusicAlbum" || currentItemType == "MusicArtist") {
            cssClass += " squareCard";
        }
        else {
            cssClass += " portraitCard";
        }

        html += '<button type="button" class="' + cssClass + '" data-index="' + index + '">';
        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';
        html += '<div class="cardPadder"></div>';

        html += '<div class="cardContent searchImage">';

        if (result.ImageUrl) {
            var displayUrl = getSearchImageDisplayUrl(result.ImageUrl, result.SearchProviderName);

            html += '<div class="cardImageContainer coveredImage" style="background-image:url(\'' + displayUrl + '\');"></div>';
        } else {

            html += '<div class="cardImageContainer coveredImage ' + cardBuilder.getDefaultColorClass(result.Name) + '"><div class="cardText cardCenteredText">' + result.Name + '</div></div>';
        }
        html += '</div>';
        html += '</div>';

        html += '<div class="cardFooter">';
        html += '<div class="cardText cardTextCentered">' + result.Name + '</div>';

        html += '<div class="cardText cardTextCentered">';
        html += result.ProductionYear || '&nbsp;';
        html += '</div>';

        if (result.GameSystem) {
            html += '<div class="cardText cardTextCentered">';
            html += result.GameSystem;
            html += '</div>';
        }
        html += '</div>';
        html += '</div>';
        html += '</button>';
        return html;
    }

    function getSearchImageDisplayUrl(url, provider) {
        var apiClient = getApiClient();

        return apiClient.getUrl("Items/RemoteSearch/Image", { imageUrl: url, ProviderName: provider });
    }

    function submitIdentficationResult(page) {

        loading.show();

        var options = {
            ReplaceAllImages: page.querySelector('#chkIdentifyReplaceImages').checked
        };

        var apiClient = getApiClient();

        apiClient.ajax({
            type: "POST",
            url: apiClient.getUrl("Items/RemoteSearch/Apply/" + currentItem.Id, options),
            data: JSON.stringify(currentSearchResult),
            contentType: "application/json"

        }).then(function () {

            hasChanges = true;
            loading.hide();

            dialogHelper.close(page);

        }, function () {

            loading.hide();

            dialogHelper.close(page);
        });
    }

    function showIdentificationForm(page, item) {

        var apiClient = getApiClient();

        apiClient.getJSON(apiClient.getUrl("Items/" + item.Id + "/ExternalIdInfos")).then(function (idList) {

            var html = '';

            var providerIds = item.ProviderIds || {};

            for (var i = 0, length = idList.length; i < length; i++) {

                var idInfo = idList[i];

                var id = "txtLookup" + idInfo.Key;

                html += '<div class="inputContainer">';

                var idLabel = globalize.translate('sharedcomponents#LabelDynamicExternalId').replace('{0}', idInfo.Name);

                var value = providerIds[idInfo.Key] || '';

                html += '<input is="emby-input" class="txtLookupId" data-providerkey="' + idInfo.Key + '" id="' + id + '" label="' + idLabel + '"/>';

                html += '</div>';
            }

            page.querySelector('#txtLookupName').value = '';

            if (item.Type == "Person" || item.Type == "BoxSet") {

                page.querySelector('.fldLookupYear').classList.add('hide');
                page.querySelector('#txtLookupYear').value = '';
            } else {

                page.querySelector('.fldLookupYear').classList.remove('hide');
                page.querySelector('#txtLookupYear').value = '';
            }

            page.querySelector('.identifyProviderIds').innerHTML = html;

            page.querySelector('.dialogHeaderTitle').innerHTML = globalize.translate('sharedcomponents#Identify');
        });
    }

    function showEditor(itemId) {

        loading.show();

        require(['text!./itemidentifier.template.html'], function (template) {

            var apiClient = getApiClient();

            apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {

                currentItem = item;
                currentItemType = currentItem.Type;

                var dialogOptions = {
                    size: 'medium',
                    removeOnClose: true,
                    scrollY: false
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('formDialog');
                dlg.classList.add('recordingDialog');

                var html = '';
                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                // Has to be assigned a z-index after the call to .open() 
                dlg.addEventListener('close', onDialogClosed);

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.dialogContent'), false);
                }

                dialogHelper.open(dlg);

                dlg.querySelector('.popupIdentifyForm').addEventListener('submit', function (e) {

                    e.preventDefault();
                    searchForIdentificationResults(dlg);
                    return false;
                });

                dlg.querySelector('.identifyOptionsForm').addEventListener('submit', function (e) {

                    e.preventDefault();
                    submitIdentficationResult(dlg);
                    return false;
                });

                dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                    dialogHelper.close(dlg);
                });

                dlg.classList.add('identifyDialog');

                showIdentificationForm(dlg, item);
                loading.hide();
            });
        });
    }

    function onDialogClosed() {

        loading.hide();
        if (hasChanges) {
            currentResolve();
        } else {
            currentReject();
        }
    }

    function showEditorFindNew(itemName, itemYear, itemType, resolveFunc) {

        currentItem = null;
        currentItemType = itemType;

        require(['text!./itemidentifier.template.html'], function (template) {

            var dialogOptions = {
                size: 'medium',
                removeOnClose: true,
                scrollY: false
            };

            if (layoutManager.tv) {
                dialogOptions.size = 'fullscreen';
            }

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.classList.add('formDialog');
            dlg.classList.add('recordingDialog');

            var html = '';
            html += globalize.translateDocument(template, 'sharedcomponents');

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            if (layoutManager.tv) {
                scrollHelper.centerFocus.on(dlg.querySelector('.dialogContent'), false);
            }

            dialogHelper.open(dlg);

            dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                dialogHelper.close(dlg);
            });

            dlg.querySelector('.popupIdentifyForm').addEventListener('submit', function (e) {

                e.preventDefault();
                searchForIdentificationResults(dlg);
                return false;
            });

            dlg.addEventListener('close', function () {

                loading.hide();
                var foundItem = hasChanges ? currentSearchResult : null;

                resolveFunc(foundItem);
            });

            dlg.classList.add('identifyDialog');

            showIdentificationFormFindNew(dlg, itemName, itemYear, itemType);
        });
    }

    function showIdentificationFormFindNew(dlg, itemName, itemYear, itemType) {

        dlg.querySelector('#txtLookupName').value = itemName;

        if (itemType == "Person" || itemType == "BoxSet") {

            dlg.querySelector('.fldLookupYear').classList.add('hide');
            dlg.querySelector('#txtLookupYear').value = '';

        } else {

            dlg.querySelector('.fldLookupYear').classList.remove('hide');
            dlg.querySelector('#txtLookupYear').value = itemYear;
        }

        dlg.querySelector('.dialogHeaderTitle').innerHTML = globalize.translate('sharedcomponents#Search');
    }

    return {
        show: function (itemId, serverId) {

            return new Promise(function (resolve, reject) {

                currentResolve = resolve;
                currentReject = reject;
                currentServerId = serverId;
                hasChanges = false;

                showEditor(itemId);
            });
        },

        showFindNew: function (itemName, itemYear, itemType, serverId) {

            return new Promise(function (resolve, reject) {

                currentServerId = serverId;

                hasChanges = false;
                showEditorFindNew(itemName, itemYear, itemType, resolve);
            });
        }
    };
});