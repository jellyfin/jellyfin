define(["dialogHelper", "loading", "connectionManager", "require", "globalize", "scrollHelper", "layoutManager", "focusManager", "browser", "emby-input", "emby-checkbox", "paper-icon-button-light", "css!./../formdialog", "material-icons", "cardStyle"], function(dialogHelper, loading, connectionManager, require, globalize, scrollHelper, layoutManager, focusManager, browser) {
    "use strict";

    function getApiClient() {
        return connectionManager.getApiClient(currentServerId)
    }

    function searchForIdentificationResults(page) {
        var i, length, value, lookupInfo = {
                ProviderIds: {}
            },
            identifyField = page.querySelectorAll(".identifyField");
        for (i = 0, length = identifyField.length; i < length; i++)(value = identifyField[i].value) && ("number" === identifyField[i].type && (value = parseInt(value)), lookupInfo[identifyField[i].getAttribute("data-lookup")] = value);
        var hasId = !1,
            txtLookupId = page.querySelectorAll(".txtLookupId");
        for (i = 0, length = txtLookupId.length; i < length; i++) value = txtLookupId[i].value, value && (hasId = !0), lookupInfo.ProviderIds[txtLookupId[i].getAttribute("data-providerkey")] = value;
        if (!hasId && !lookupInfo.Name) return void require(["toast"], function(toast) {
            toast(globalize.translate("sharedcomponents#PleaseEnterNameOrId"))
        });
        currentItem && currentItem.GameSystem && (lookupInfo.GameSystem = currentItem.GameSystem), lookupInfo = {
            SearchInfo: lookupInfo
        }, currentItem && currentItem.Id ? lookupInfo.ItemId = currentItem.Id : lookupInfo.IncludeDisabledProviders = !0, loading.show();
        var apiClient = getApiClient();
        apiClient.ajax({
            type: "POST",
            url: apiClient.getUrl("Items/RemoteSearch/" + currentItemType),
            data: JSON.stringify(lookupInfo),
            contentType: "application/json",
            dataType: "json"
        }).then(function(results) {
            loading.hide(), showIdentificationSearchResults(page, results)
        })
    }

    function showIdentificationSearchResults(page, results) {
        function onSearchImageClick() {
            var index = parseInt(this.getAttribute("data-index")),
                currentResult = results[index];
            null != currentItem ? showIdentifyOptions(page, currentResult) : finishFindNewDialog(page, currentResult)
        }
        var identificationSearchResults = page.querySelector(".identificationSearchResults");
        page.querySelector(".popupIdentifyForm").classList.add("hide"), identificationSearchResults.classList.remove("hide"), page.querySelector(".identifyOptionsForm").classList.add("hide"), page.querySelector(".dialogContentInner").classList.remove("dialog-content-centered");
        var i, length, html = "";
        for (i = 0, length = results.length; i < length; i++) {
            html += getSearchResultHtml(results[i], i)
        }
        var elem = page.querySelector(".identificationSearchResultList");
        elem.innerHTML = html;
        var searchImages = elem.querySelectorAll(".card");
        for (i = 0, length = searchImages.length; i < length; i++) searchImages[i].addEventListener("click", onSearchImageClick);
        layoutManager.tv && focusManager.autoFocus(identificationSearchResults)
    }

    function finishFindNewDialog(dlg, identifyResult) {
        currentSearchResult = identifyResult, hasChanges = !0, loading.hide(), dialogHelper.close(dlg)
    }

    function showIdentifyOptions(page, identifyResult) {
        var identifyOptionsForm = page.querySelector(".identifyOptionsForm");
        page.querySelector(".popupIdentifyForm").classList.add("hide"), page.querySelector(".identificationSearchResults").classList.add("hide"), identifyOptionsForm.classList.remove("hide"), page.querySelector("#chkIdentifyReplaceImages").checked = !0, page.querySelector(".dialogContentInner").classList.add("dialog-content-centered"), currentSearchResult = identifyResult;
        var lines = [];
        lines.push(identifyResult.Name), identifyResult.ProductionYear && lines.push(identifyResult.ProductionYear), identifyResult.GameSystem && lines.push(identifyResult.GameSystem);
        var resultHtml = lines.join("<br/>");
        if (identifyResult.ImageUrl) {
            resultHtml = '<div style="display:flex;align-items:center;"><img src="' + getSearchImageDisplayUrl(identifyResult.ImageUrl, identifyResult.SearchProviderName) + '" style="max-height:240px;" /><div style="margin-left:1em;">' + resultHtml + "</div>"
        }
        page.querySelector(".selectedSearchResult").innerHTML = resultHtml, focusManager.focus(identifyOptionsForm.querySelector(".btnSubmit"))
    }

    function getSearchResultHtml(result, index) {
        var padderClass, html = "",
            cssClass = "card scalableCard",
            cardBoxCssClass = "cardBox";
        if ("Episode" === currentItemType ? (cssClass += " backdropCard backdropCard-scalable", padderClass = "cardPadder-backdrop") : "MusicAlbum" === currentItemType || "MusicArtist" === currentItemType ? (cssClass += " squareCard squareCard-scalable", padderClass = "cardPadder-square") : (cssClass += " portraitCard portraitCard-scalable", padderClass = "cardPadder-portrait"), layoutManager.tv && !browser.slow && (cardBoxCssClass += " cardBox-focustransform"), cardBoxCssClass += " cardBox-bottompadded", layoutManager.tv && (cardBoxCssClass += " card-focuscontent cardBox-withfocuscontent"), html += '<button type="button" class="' + cssClass + '" data-index="' + index + '">', html += '<div class="' + cardBoxCssClass + '">', html += '<div class="cardScalable">', html += '<div class="' + padderClass + '"></div>', html += '<div class="cardContent searchImage">', result.ImageUrl) {
            html += '<div class="cardImageContainer coveredImage" style="background-image:url(\'' + getSearchImageDisplayUrl(result.ImageUrl, result.SearchProviderName) + "');\"></div>"
        } else html += '<div class="cardImageContainer coveredImage defaultCardBackground defaultCardBackground1"><div class="cardText cardCenteredText">' + result.Name + "</div></div>";
        html += "</div>", html += "</div>";
        var numLines = 2;
        "MusicAlbum" === currentItemType ? numLines++ : "Game" === currentItemType && numLines++;
        var lines = [result.Name];
        result.AlbumArtist && lines.push(result.AlbumArtist.Name), result.ProductionYear && lines.push(result.ProductionYear), result.GameSystem && lines.push(result.GameSystem);
        for (var i = 0; i < numLines; i++) html += 0 === i ? '<div class="cardText cardText-first cardTextCentered">' : '<div class="cardText cardText-secondary cardTextCentered">', html += lines[i] || "&nbsp;", html += "</div>";
        return html += "</div>", html += "</button>"
    }

    function getSearchImageDisplayUrl(url, provider) {
        return getApiClient().getUrl("Items/RemoteSearch/Image", {
            imageUrl: url,
            ProviderName: provider
        })
    }

    function submitIdentficationResult(page) {
        loading.show();
        var options = {
                ReplaceAllImages: page.querySelector("#chkIdentifyReplaceImages").checked
            },
            apiClient = getApiClient();
        apiClient.ajax({
            type: "POST",
            url: apiClient.getUrl("Items/RemoteSearch/Apply/" + currentItem.Id, options),
            data: JSON.stringify(currentSearchResult),
            contentType: "application/json"
        }).then(function() {
            hasChanges = !0, loading.hide(), dialogHelper.close(page)
        }, function() {
            loading.hide(), dialogHelper.close(page)
        })
    }

    function showIdentificationForm(page, item) {
        var apiClient = getApiClient();
        apiClient.getJSON(apiClient.getUrl("Items/" + item.Id + "/ExternalIdInfos")).then(function(idList) {
            for (var html = "", providerIds = item.ProviderIds || {}, i = 0, length = idList.length; i < length; i++) {
                var idInfo = idList[i],
                    id = "txtLookup" + idInfo.Key;
                html += '<div class="inputContainer">';
                var idLabel = globalize.translate("sharedcomponents#LabelDynamicExternalId").replace("{0}", idInfo.Name);
                idInfo.Key;
                html += '<input is="emby-input" class="txtLookupId" data-providerkey="' + idInfo.Key + '" id="' + id + '" label="' + idLabel + '"/>', html += "</div>"
            }
            page.querySelector("#txtLookupName").value = "", "Person" === item.Type || "BoxSet" === item.Type ? (page.querySelector(".fldLookupYear").classList.add("hide"), page.querySelector("#txtLookupYear").value = "") : (page.querySelector(".fldLookupYear").classList.remove("hide"), page.querySelector("#txtLookupYear").value = ""), page.querySelector(".identifyProviderIds").innerHTML = html, page.querySelector(".formDialogHeaderTitle").innerHTML = globalize.translate("sharedcomponents#Identify")
        })
    }

    function showEditor(itemId) {
        loading.show(), require(["text!./itemidentifier.template.html"], function(template) {
            var apiClient = getApiClient();
            apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function(item) {
                currentItem = item, currentItemType = currentItem.Type;
                var dialogOptions = {
                    size: "fullscreen-border",
                    removeOnClose: !0,
                    scrollY: !1
                };
                layoutManager.tv && (dialogOptions.size = "fullscreen");
                var dlg = dialogHelper.createDialog(dialogOptions);
                dlg.classList.add("formDialog"), dlg.classList.add("recordingDialog");
                var html = "";
                html += globalize.translateDocument(template, "sharedcomponents"), dlg.innerHTML = html, dlg.addEventListener("close", onDialogClosed), layoutManager.tv && scrollHelper.centerFocus.on(dlg.querySelector(".formDialogContent"), !1), dialogHelper.open(dlg), dlg.querySelector(".popupIdentifyForm").addEventListener("submit", function(e) {
                    return e.preventDefault(), searchForIdentificationResults(dlg), !1
                }), dlg.querySelector(".identifyOptionsForm").addEventListener("submit", function(e) {
                    return e.preventDefault(), submitIdentficationResult(dlg), !1
                }), dlg.querySelector(".btnCancel").addEventListener("click", function(e) {
                    dialogHelper.close(dlg)
                }), dlg.classList.add("identifyDialog"), showIdentificationForm(dlg, item), loading.hide()
            })
        })
    }

    function onDialogClosed() {
        loading.hide(), hasChanges ? currentResolve() : currentReject()
    }

    function showEditorFindNew(itemName, itemYear, itemType, resolveFunc) {
        currentItem = null, currentItemType = itemType, require(["text!./itemidentifier.template.html"], function(template) {
            var dialogOptions = {
                size: "fullscreen-border",
                removeOnClose: !0,
                scrollY: !1
            };
            layoutManager.tv && (dialogOptions.size = "fullscreen");
            var dlg = dialogHelper.createDialog(dialogOptions);
            dlg.classList.add("formDialog"), dlg.classList.add("recordingDialog");
            var html = "";
            html += globalize.translateDocument(template, "sharedcomponents"), dlg.innerHTML = html, layoutManager.tv && scrollHelper.centerFocus.on(dlg.querySelector(".formDialogContent"), !1), dialogHelper.open(dlg), dlg.querySelector(".btnCancel").addEventListener("click", function(e) {
                dialogHelper.close(dlg)
            }), dlg.querySelector(".popupIdentifyForm").addEventListener("submit", function(e) {
                return e.preventDefault(), searchForIdentificationResults(dlg), !1
            }), dlg.addEventListener("close", function() {
                loading.hide(), resolveFunc(hasChanges ? currentSearchResult : null)
            }), dlg.classList.add("identifyDialog"), showIdentificationFormFindNew(dlg, itemName, itemYear, itemType)
        })
    }

    function showIdentificationFormFindNew(dlg, itemName, itemYear, itemType) {
        dlg.querySelector("#txtLookupName").value = itemName, "Person" === itemType || "BoxSet" === itemType ? (dlg.querySelector(".fldLookupYear").classList.add("hide"), dlg.querySelector("#txtLookupYear").value = "") : (dlg.querySelector(".fldLookupYear").classList.remove("hide"), dlg.querySelector("#txtLookupYear").value = itemYear), dlg.querySelector(".formDialogHeaderTitle").innerHTML = globalize.translate("sharedcomponents#Search")
    }
    var currentItem, currentItemType, currentServerId, currentResolve, currentReject, currentSearchResult, hasChanges = !1;
    return {
        show: function(itemId, serverId) {
            return new Promise(function(resolve, reject) {
                currentResolve = resolve, currentReject = reject, currentServerId = serverId, hasChanges = !1, showEditor(itemId)
            })
        },
        showFindNew: function(itemName, itemYear, itemType, serverId) {
            return new Promise(function(resolve, reject) {
                currentServerId = serverId, hasChanges = !1, showEditorFindNew(itemName, itemYear, itemType, resolve)
            })
        }
    }
});