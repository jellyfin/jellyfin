define(['dialogHelper', 'loading', 'jQuery', 'paper-fab', 'paper-input', 'paper-checkbox', 'paper-icon-button-light'], function (dialogHelper, loading, $) {

    var currentItem;
    var currentItemType;
    var currentDeferred;
    var hasChanges = false;
    var currentSearchResult;

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

        $('.popupIdentifyForm', page).hide();
        $('.identificationSearchResults', page).show();
        $('.identifyOptionsForm', page).hide();
        $('.btnIdentifyBack', page).show();

        var html = '';

        for (var i = 0, length = results.length; i < length; i++) {

            var result = results[i];
            html += getSearchResultHtml(result, i);
        }

        var elem = $('.identificationSearchResultList', page).html(html);

        $('.searchImage', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));

            var currentResult = results[index];

            if (currentItem != null) {

                showIdentifyOptions(page, currentResult);
            } else {

                finishFindNewDialog(page, currentResult);
            }
        });
    }

    function finishFindNewDialog(dlg, identifyResult) {
        currentSearchResult = identifyResult;
        hasChanges = true;
        loading.hide();

        dialogHelper.close(dlg);
    }

    function showIdentifyOptions(page, identifyResult) {

        $('.popupIdentifyForm', page).hide();
        $('.identificationSearchResults', page).hide();
        $('.identifyOptionsForm', page).show();
        $('.btnIdentifyBack', page).show();
        $('#chkIdentifyReplaceImages', page).checked(true);

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

        $('.selectedSearchResult', page).html(resultHtml);
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

            html += '<div class="cardImage iconCardImage"><iron-icon icon="search"></iron-icon></div>';
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
            ReplaceAllImages: $('#chkIdentifyReplaceImages', page).checked()
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

                html += '<div>';

                var idLabel = Globalize.translate('LabelDynamicExternalId').replace('{0}', idInfo.Name);

                var value = providerIds[idInfo.Key] || '';

                html += '<paper-input class="txtLookupId" data-providerkey="' + idInfo.Key + '" id="' + id + '" label="' + idLabel + '"></paper-input>';

                html += '</div>';
            }

            $('#txtLookupName', page).val('');

            if (item.Type == "Person" || item.Type == "BoxSet") {

                $('.fldLookupYear', page).hide();
                $('#txtLookupYear', page).val('');
            } else {

                $('.fldLookupYear', page).show();
                $('#txtLookupYear', page).val('');
            }

            $('.identifyProviderIds', page).html(html);

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
                    size: 'medium'
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                var html = '';
                html += Globalize.translateDocument(template);

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('close', onDialogClosed);

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

                $('.btnCancel', dlg).on('click', function () {

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

        $(this).remove();
        loading.hide();
        currentDeferred.resolveWith(null, [hasChanges]);
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

            var deferred = jQuery.Deferred();

            currentDeferred = deferred;
            hasChanges = false;

            showEditor(itemId);
            return deferred.promise();
        },

        showFindNew: function (itemName, itemYear, itemType) {
            return new Promise(function (resolve, reject) {

                hasChanges = false;
                showEditorFindNew(itemName, itemYear, itemType, resolve);
            });
        }
    };
});