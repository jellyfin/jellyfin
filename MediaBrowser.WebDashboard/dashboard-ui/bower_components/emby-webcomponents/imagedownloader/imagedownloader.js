define(['loading', 'apphost', 'dialogHelper', 'connectionManager', 'imageLoader', 'browser', 'layoutManager', 'scrollHelper', 'globalize', 'require', 'emby-checkbox', 'emby-button', 'paper-icon-button-light', 'emby-linkbutton', 'formDialogStyle', 'cardStyle'], function (loading, appHost, dialogHelper, connectionManager, imageLoader, browser, layoutManager, scrollHelper, globalize, require) {
    'use strict';

    var currentItemId;
    var currentItemType;
    var currentResolve;
    var currentReject;
    var hasChanges = false;

    // These images can be large and we're seeing memory problems in safari
    var browsableImagePageSize = browser.slow ? 6 : 30;

    var browsableImageStartIndex = 0;
    var browsableImageType = 'Primary';
    var selectedProvider;

    function getBaseRemoteOptions() {

        var options = {};

        options.itemId = currentItemId;

        return options;
    }

    function reloadBrowsableImages(page, apiClient) {

        loading.show();

        var options = getBaseRemoteOptions();

        options.type = browsableImageType;
        options.startIndex = browsableImageStartIndex;
        options.limit = browsableImagePageSize;
        options.IncludeAllLanguages = page.querySelector('#chkAllLanguages').checked;

        var provider = selectedProvider || '';

        if (provider) {
            options.ProviderName = provider;
        }

        apiClient.getAvailableRemoteImages(options).then(function (result) {

            renderRemoteImages(page, apiClient, result, browsableImageType, options.startIndex, options.limit);

            page.querySelector('#selectBrowsableImageType').value = browsableImageType;

            var providersHtml = result.Providers.map(function (p) {
                return '<option value="' + p + '">' + p + '</option>';
            });

            var selectImageProvider = page.querySelector('#selectImageProvider');
            selectImageProvider.innerHTML = '<option value="">' + globalize.translate('sharedcomponents#All') + '</option>' + providersHtml;
            selectImageProvider.value = provider;

            loading.hide();
        });

    }

    function renderRemoteImages(page, apiClient, imagesResult, imageType, startIndex, limit) {

        page.querySelector('.availableImagesPaging').innerHTML = getPagingHtml(startIndex, limit, imagesResult.TotalRecordCount);

        var html = '';

        for (var i = 0, length = imagesResult.Images.length; i < length; i++) {

            html += getRemoteImageHtml(imagesResult.Images[i], imageType, apiClient);
        }

        var availableImagesList = page.querySelector('.availableImagesList');
        availableImagesList.innerHTML = html;
        imageLoader.lazyChildren(availableImagesList);

        var btnNextPage = page.querySelector('.btnNextPage');
        var btnPreviousPage = page.querySelector('.btnPreviousPage');

        if (btnNextPage) {
            btnNextPage.addEventListener('click', function () {
                browsableImageStartIndex += browsableImagePageSize;
                reloadBrowsableImages(page, apiClient);
            });
        }

        if (btnPreviousPage) {
            btnPreviousPage.addEventListener('click', function () {
                browsableImageStartIndex -= browsableImagePageSize;
                reloadBrowsableImages(page, apiClient);
            });
        }

    }

    function getPagingHtml(startIndex, limit, totalRecordCount) {

        var html = '';

        var recordsEnd = Math.min(startIndex + limit, totalRecordCount);

        // 20 is the minimum page size
        var showControls = totalRecordCount > limit;

        html += '<div class="listPaging">';

        html += '<span style="margin-right: 10px;">';

        var startAtDisplay = totalRecordCount ? startIndex + 1 : 0;
        html += startAtDisplay + '-' + recordsEnd + ' of ' + totalRecordCount;

        html += '</span>';

        if (showControls) {
            html += '<div data-role="controlgroup" data-type="horizontal" style="display:inline-block;">';

            html += '<button is="paper-icon-button-light" title="' + globalize.translate('sharedcomponents#Previous') + '" class="btnPreviousPage autoSize" ' + (startIndex ? '' : 'disabled') + '><i class="md-icon">&#xE5C4;</i></button>';
            html += '<button is="paper-icon-button-light" title="' + globalize.translate('sharedcomponents#Next') + '" class="btnNextPage autoSize" ' + (startIndex + limit >= totalRecordCount ? 'disabled' : '') + '><i class="md-icon">arrow_forward</i></button>';
            html += '</div>';
        }

        html += '</div>';

        return html;
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

    function downloadRemoteImage(page, apiClient, url, type, provider) {

        var options = getBaseRemoteOptions();

        options.Type = type;
        options.ImageUrl = url;
        options.ProviderName = provider;

        loading.show();

        apiClient.downloadRemoteImage(options).then(function () {

            hasChanges = true;
            var dlg = parentWithClass(page, 'dialog');
            dialogHelper.close(dlg);
        });
    }

    function getDisplayUrl(url, apiClient) {
        return apiClient.getUrl("Images/Remote", { imageUrl: url });
    }

    function getRemoteImageHtml(image, imageType, apiClient) {

        var tagName = layoutManager.tv ? 'button' : 'div';
        var enableFooterButtons = !layoutManager.tv;

        var html = '';

        var cssClass = "card scalableCard imageEditorCard";
        var cardBoxCssClass = 'cardBox visualCardBox';

        var shape = 'backdrop';
        if (imageType === "Backdrop" || imageType === "Art" || imageType === "Thumb" || imageType === "Logo") {
            shape = 'backdrop';
        }
        else if (imageType === "Banner") {
            shape = 'banner';
        }
        else if (imageType === "Disc") {
            shape = 'square';
        }
        else {

            if (currentItemType === "Episode") {
                shape = 'backdrop';
            }
            else if (currentItemType === "MusicAlbum" || currentItemType === "MusicArtist") {
                shape = 'square';
            }
            else {
                shape = 'portrait';
            }
        }

        cssClass += ' ' + shape + 'Card ' + shape + 'Card-scalable';
        if (tagName === 'button') {
            cssClass += ' btnImageCard';

            if (layoutManager.tv && !browser.slow) {
                cardBoxCssClass += ' cardBox-focustransform';
            }

            if (layoutManager.tv) {
                cardBoxCssClass += ' card-focuscontent cardBox-withfocuscontent';
            }

            html += '<button type="button" class="' + cssClass + '"';
        } else {
            html += '<div class="' + cssClass + '"';
        }

        html += ' data-imageprovider="' + image.ProviderName + '" data-imageurl="' + image.Url + '" data-imagetype="' + image.Type + '"';

        html += '>';

        html += '<div class="' + cardBoxCssClass + '">';
        html += '<div class="cardScalable visualCardBox-cardScalable" style="background-color:transparent;">';
        html += '<div class="cardPadder-' + shape + '"></div>';
        html += '<div class="cardContent">';

        if (layoutManager.tv || !appHost.supports('externallinks')) {
            html += '<div class="cardImageContainer lazy" data-src="' + getDisplayUrl(image.Url, apiClient) + '" style="background-position:center bottom;"></div>';
        }
        else {
            html += '<a is="emby-linkbutton" target="_blank" href="' + getDisplayUrl(image.Url, apiClient) + '" class="button-link cardImageContainer lazy" data-src="' + getDisplayUrl(image.Url, apiClient) + '" style="background-position:center bottom;"></a>';
        }

        html += '</div>';
        html += '</div>';

        // begin footer
        html += '<div class="cardFooter visualCardBox-cardFooter">';

        html += '<div class="cardText cardTextCentered">' + image.ProviderName + '</div>';

        if (image.Width || image.Height || image.Language) {

            html += '<div class="cardText cardText-secondary cardTextCentered">';

            if (image.Width && image.Height) {
                html += image.Width + ' x ' + image.Height;

                if (image.Language) {

                    html += ' • ' + image.Language;
                }
            } else {
                if (image.Language) {

                    html += image.Language;
                }
            }

            html += '</div>';
        }

        if (image.CommunityRating != null) {

            html += '<div class="cardText cardText-secondary cardTextCentered">';

            if (image.RatingType === "Likes") {
                html += image.CommunityRating + (image.CommunityRating === 1 ? " like" : " likes");
            } else {

                if (image.CommunityRating) {
                    html += image.CommunityRating.toFixed(1);

                    if (image.VoteCount) {
                        html += ' • ' + image.VoteCount + (image.VoteCount === 1 ? " vote" : " votes");
                    }
                } else {
                    html += "Unrated";
                }
            }

            html += '</div>';
        }

        if (enableFooterButtons) {
            html += '<div class="cardText cardTextCentered">';

            html += '<button is="paper-icon-button-light" class="btnDownloadRemoteImage autoSize" raised" title="' + globalize.translate('sharedcomponents#Download') + '"><i class="md-icon">&#xE2C0;</i></button>';
            html += '</div>';
        }

        html += '</div>';
        // end footer

        html += '</div>';
        //html += '</div>';

        html += '</' + tagName + '>';

        return html;
    }

    function initEditor(page, apiClient) {

        page.querySelector('#selectBrowsableImageType').addEventListener('change', function () {
            browsableImageType = this.value;
            browsableImageStartIndex = 0;
            selectedProvider = null;

            reloadBrowsableImages(page, apiClient);
        });

        page.querySelector('#selectImageProvider').addEventListener('change', function () {

            browsableImageStartIndex = 0;
            selectedProvider = this.value;

            reloadBrowsableImages(page, apiClient);
        });

        page.querySelector('#chkAllLanguages').addEventListener('change', function () {

            browsableImageStartIndex = 0;

            reloadBrowsableImages(page, apiClient);
        });

        page.addEventListener('click', function (e) {

            var btnDownloadRemoteImage = parentWithClass(e.target, 'btnDownloadRemoteImage');
            if (btnDownloadRemoteImage) {
                var card = parentWithClass(btnDownloadRemoteImage, 'card');
                downloadRemoteImage(page, apiClient, card.getAttribute('data-imageurl'), card.getAttribute('data-imagetype'), card.getAttribute('data-imageprovider'));
                return;
            }

            var btnImageCard = parentWithClass(e.target, 'btnImageCard');
            if (btnImageCard) {
                downloadRemoteImage(page, apiClient, btnImageCard.getAttribute('data-imageurl'), btnImageCard.getAttribute('data-imagetype'), btnImageCard.getAttribute('data-imageprovider'));
            }
        });
    }

    function showEditor(itemId, serverId, itemType) {

        loading.show();

        require(['text!./imagedownloader.template.html'], function (template) {

            var apiClient = connectionManager.getApiClient(serverId);

            currentItemId = itemId;
            currentItemType = itemType;

            var dialogOptions = {
                removeOnClose: true
            };

            if (layoutManager.tv) {
                dialogOptions.size = 'fullscreen';
            } else {
                dialogOptions.size = 'fullscreen-border';
            }

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.innerHTML = globalize.translateDocument(template, 'sharedcomponents');

            if (layoutManager.tv) {
                scrollHelper.centerFocus.on(dlg, false);
            }

            // Has to be assigned a z-index after the call to .open() 
            dlg.addEventListener('close', onDialogClosed);

            dialogHelper.open(dlg);

            var editorContent = dlg.querySelector('.formDialogContent');
            initEditor(editorContent, apiClient);

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
            });

            reloadBrowsableImages(editorContent, apiClient);
        });
    }

    function onDialogClosed() {

        var dlg = this;

        if (layoutManager.tv) {
            scrollHelper.centerFocus.off(dlg, false);
        }

        loading.hide();
        if (hasChanges) {
            currentResolve();
        } else {
            currentReject();
        }
    }

    return {
        show: function (itemId, serverId, itemType, imageType) {

            return new Promise(function (resolve, reject) {

                currentResolve = resolve;
                currentReject = reject;
                hasChanges = false;
                browsableImageStartIndex = 0;
                browsableImageType = imageType || 'Primary';
                selectedProvider = null;

                showEditor(itemId, serverId, itemType);
            });
        }
    };
});