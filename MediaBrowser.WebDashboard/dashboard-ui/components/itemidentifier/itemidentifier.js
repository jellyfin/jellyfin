define(['components/paperdialoghelper', 'paper-dialog', 'paper-fab', 'paper-input'], function (paperDialogHelper) {

    var currentItem;
    var currentDeferred;
    var hasChanges = false;
    var currentSearchResult;

    function onIdentificationFormSubmitted() {

        var page = $(this).parents('.editorContent');

        searchForIdentificationResults(page);
        return false;
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
            Dashboard.alert(Globalize.translate('MessagePleaseEnterNameOrId'));
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

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl("Items/RemoteSearch/" + currentItem.Type),
            data: JSON.stringify(lookupInfo),
            contentType: "application/json",
            dataType: 'json'

        }).then(function (results) {

            Dashboard.hideLoadingMsg();
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

        var elem = $('.identificationSearchResultList', page).html(html).trigger('create');

        $('.searchImage', elem).on('click', function () {

            var index = parseInt(this.getAttribute('data-index'));

            var currentResult = results[index];

            showIdentifyOptions(page, currentResult);
        });
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

        if (currentItem.Type == "Episode") {
            cssClass += " backdropCard";
        }
        else if (currentItem.Type == "MusicAlbum" || currentItem.Type == "MusicArtist") {
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

    function onIdentificationOptionsSubmit() {

        var page = $(this).parents('.editorContent');

        submitIdentficationResult(page);
        return false;
    }

    function submitIdentficationResult(page) {

        Dashboard.showLoadingMsg();

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
            Dashboard.hideLoadingMsg();

            paperDialogHelper.close(document.querySelector('.identifyDialog'));

        }, function () {

            Dashboard.hideLoadingMsg();

            paperDialogHelper.close(document.querySelector('.identifyDialog'));
        });
    }

    function initEditor(page) {

        $('.popupIdentifyForm', page).off('submit', onIdentificationFormSubmitted).on('submit', onIdentificationFormSubmitted);
        $('.identifyOptionsForm', page).off('submit', onIdentificationOptionsSubmit).on('submit', onIdentificationOptionsSubmit);
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

                html += '<paper-input class="txtLookupId" value="' + value + '" data-providerkey="' + idInfo.Key + '" id="' + id + '" label="' + idLabel + '"></paper-input>';

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

            $('.identificationHeader', page).html(Globalize.translate('HeaderIdentify'));
        });
    }

    function showEditor(itemId) {

        Dashboard.showLoadingMsg();

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/itemidentifier/itemidentifier.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(function (item) {

                currentItem = item;

                var dlg = paperDialogHelper.createDialog();

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog"></paper-fab>';
                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + Globalize.translate('HeaderIdentifyItem') + '</div>';
                html += '</h2>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('iron-overlay-closed', onDialogClosed);

                paperDialogHelper.open(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                initEditor(editorContent);

                $('.btnCloseDialog', dlg).on('click', function () {

                    paperDialogHelper.close(dlg);
                });

                dlg.classList.add('identifyDialog');

                showIdentificationForm(dlg, item);
                Dashboard.hideLoadingMsg();
            });
        }

        xhr.send();
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    window.ItemIdentifier = {
        show: function (itemId) {

            var deferred = DeferredBuilder.Deferred();

            currentDeferred = deferred;
            hasChanges = false;

            showEditor(itemId);
            return deferred.promise();
        }
    };
});