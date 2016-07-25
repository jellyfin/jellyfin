define(['dialogHelper', 'loading', 'emby-input', 'emby-checkbox', 'paper-icon-button-light'], function (dialogHelper, loading) {

    var currentItem;
    var currentItemType;
    var currentResolve;
    var currentReject;
    var hasChanges = false;
    var currentSearchResult;

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
                toast(Globalize.translate('MessagePleaseEnterNameOrId'));
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

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl("Items/RemoteSearch/" + currentItemType),
            data: JSON.stringify(lookupInfo),
            contentType: "application/json",
            dataType: 'json'

        }).then(function (results) {

            loading.hide();
            showIdentificationSearchResults(page, results);
        });
    }

    function showIdentificationSearchResults(page, results) {

        page.querySelector('.popupIdentifyForm').classList.add('hide');
        page.querySelector('.identificationSearchResults').classList.remove('hide');
        page.querySelector('.identifyOptionsForm').classList.add('hide');

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

        var searchImages = elem.querySelectorAll('.searchImage');
        for (i = 0, length = searchImages.length; i < length; i++) {

            searchImages[i].addEventListener('click', onSearchImageClick);
        }
    }

    function finishFindNewDialog(dlg, identifyResult) {
        currentSearchResult = identifyResult;
        hasChanges = true;
        loading.hide();

        dialogHelper.close(dlg);
    }

    function showIdentifyOptions(page, identifyResult) {

        page.querySelector('.popupIdentifyForm').classList.add('hide');
        page.querySelector('.identificationSearchResults').classList.add('hide');
        page.querySelector('.identifyOptionsForm').classList.remove('hide');
        page.querySelector('#chkIdentifyReplaceImages').checked = true;

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

            resultHtml = '<img src="' + displayUrl + '" style="max-height:160px;" /><br/>' + resultHtml;
        }

        page.querySelector('.selectedSearchResult').innerHTML = resultHtml;
    }

    function getSearchResultHtml(result, index) {

        var html = '';
        var cssClass = "card";

        if (currentItemType == "Episode") {
            cssClass += " backdropCard";
        }
        else if (currentItemType == "MusicAlbum" || currentItemType == "MusicArtist") {
            cssClass += " squareCard";
        }
        else {
            cssClass += " portraitCard";
        }

        html += '<div class="' + cssClass + '">';
        html += '<div class="cardBox">';
        html += '<div class="cardScalable">';
        html += '<div class="cardPadder"></div>';

        html += '<a class="cardContent searchImage" href="#" data-index="' + index + '">';

        if (result.ImageUrl) {
            var displayUrl = getSearchImageDisplayUrl(result.ImageUrl, result.SearchProviderName);

            html += '<div class="cardImage" style="background-image:url(\'' + displayUrl + '\');"></div>';
        } else {

            html += '<div class="cardImage iconCardImage"><i class="md-icon">search</i></div>';
        }
        html += '</a>';
        html += '</div>';

        html += '<div class="cardFooter outerCardFooter">';
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
        html += '</div>';
        return html;
    }

    function getSearchImageDisplayUrl(url, provider) {
        return ApiClient.getUrl("Items/RemoteSearch/Image", { imageUrl: url, ProviderName: provider });
    }

    function submitIdentficationResult(page) {

        loading.show();

        var options = {
            ReplaceAllImages: page.querySelector('#chkIdentifyReplaceImages').checked
        };

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl("Items/RemoteSearch/Apply/" + currentItem.Id, options),
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

        ApiClient.getJSON(ApiClient.getUrl("Items/" + item.Id + "/ExternalIdInfos")).then(function (idList) {

            var html = '';

            var providerIds = item.ProviderIds || {};

            for (var i = 0, length = idList.length; i < length; i++) {

                var idInfo = idList[i];

                var id = "txtLookup" + idInfo.Key;

                html += '<div class="inputContainer">';

                var idLabel = Globalize.translate('LabelDynamicExternalId').replace('{0}', idInfo.Name);

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

            page.querySelector('.dialogHeaderTitle').innerHTML = Globalize.translate('HeaderIdentify');
        });
    }

    function showEditor(itemId) {

        loading.show();

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/itemidentifier/itemidentifier.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            ApiClient.getItem(ApiClient.getCurrentUserId(), itemId).then(function (item) {

                currentItem = item;
                currentItemType = currentItem.Type;

                var dlg = dialogHelper.createDialog({
                    size: 'medium',
                    removeOnClose: true
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                var html = '';
                html += Globalize.translateDocument(template);

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                // Has to be assigned a z-index after the call to .open() 
                dlg.addEventListener('close', onDialogClosed);

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
        }

        xhr.send();
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

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/itemidentifier/itemidentifier.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;

            var dlg = dialogHelper.createDialog({
                size: 'medium'
            });

            dlg.classList.add('ui-body-a');
            dlg.classList.add('background-theme-a');

            var html = '';
            html += Globalize.translateDocument(template);

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

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
        }

        xhr.send();
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

        dlg.querySelector('.dialogHeaderTitle').innerHTML = Globalize.translate('HeaderSearch');
    }

    return {
        show: function (itemId) {

            return new Promise(function (resolve, reject) {

                currentResolve = resolve;
                currentReject = reject;
                hasChanges = false;

                showEditor(itemId);
            });
        },

        showFindNew: function (itemName, itemYear, itemType) {
            return new Promise(function (resolve, reject) {

                hasChanges = false;
                showEditorFindNew(itemName, itemYear, itemType, resolve);
            });
        }
    };
});