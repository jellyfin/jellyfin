define(['dialogHelper', 'jQuery', 'paper-checkbox', 'paper-fab', 'paper-icon-button-light'], function (dialogHelper, $) {

    var currentItemId;
    var currentItemType;
    var currentDeferred;
    var hasChanges = false;

    // These images can be large and we're seeing memory problems in safari
    var browsableImagePageSize = browserInfo.safari ? 6 : (browserInfo.mobile ? 10 : 40);

    var browsableImageStartIndex = 0;
    var browsableImageType = 'Primary';
    var selectedProvider;

    function getBaseRemoteOptions() {

        var options = {};

        options.itemId = currentItemId;

        return options;
    }

    function reloadBrowsableImages(page) {

        Dashboard.showLoadingMsg();

        var options = getBaseRemoteOptions();

        options.type = browsableImageType;
        options.startIndex = browsableImageStartIndex;
        options.limit = browsableImagePageSize;
        options.IncludeAllLanguages = $('#chkAllLanguages', page).checked();

        var provider = selectedProvider || '';

        if (provider) {
            options.ProviderName = provider;
        }

        ApiClient.getAvailableRemoteImages(options).then(function (result) {

            renderRemoteImages(page, result, browsableImageType, options.startIndex, options.limit);

            $('#selectBrowsableImageType', page).val(browsableImageType);

            var providersHtml = result.Providers.map(function (p) {
                return '<option value="' + p + '">' + p + '</option>';
            });

            $('#selectImageProvider', page).html('<option value="">' + Globalize.translate('LabelAll') + '</option>' + providersHtml).val(provider);

            Dashboard.hideLoadingMsg();
        });

    }

    function renderRemoteImages(page, imagesResult, imageType, startIndex, limit) {
        $('.availableImagesPaging', page).html(getPagingHtml(startIndex, limit, imagesResult.TotalRecordCount));

        var html = '';

        for (var i = 0, length = imagesResult.Images.length; i < length; i++) {

            html += getRemoteImageHtml(imagesResult.Images[i], imageType);
        }

        var availableImagesList = page.querySelector('.availableImagesList');
        availableImagesList.innerHTML = html;
        ImageLoader.lazyChildren(availableImagesList);

        $('.btnNextPage', page).on('click', function () {
            browsableImageStartIndex += browsableImagePageSize;
            reloadBrowsableImages(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            browsableImageStartIndex -= browsableImagePageSize;
            reloadBrowsableImages(page);
        });

        $('.btnDownloadRemoteImage', page).on('click', function () {

            downloadRemoteImage(page, this.getAttribute('data-imageurl'), this.getAttribute('data-imagetype'), this.getAttribute('data-imageprovider'));
        });

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

            html += '<button is="paper-icon-button-light" title="' + Globalize.translate('ButtonPreviousPage') + '" class="btnPreviousPage" ' + (startIndex ? '' : 'disabled') + '><iron-icon icon="arrow-back"></iron-icon></button>';
            html += '<button is="paper-icon-button-light" title="' + Globalize.translate('ButtonNextPage') + '" class="btnNextPage" ' + (startIndex + limit >= totalRecordCount ? 'disabled' : '') + '><iron-icon icon="arrow-forward"></iron-icon></button>';
            html += '</div>';
        }

        html += '</div>';

        return html;
    }

    function downloadRemoteImage(page, url, type, provider) {

        var options = getBaseRemoteOptions();

        options.Type = type;
        options.ImageUrl = url;
        options.ProviderName = provider;

        Dashboard.showLoadingMsg();

        ApiClient.downloadRemoteImage(options).then(function () {

            hasChanges = true;
            var dlg = $(page).parents('.dialog')[0];
            dialogHelper.close(dlg);
        });
    }

    function getDisplayUrl(url) {
        return ApiClient.getUrl("Images/Remote", { imageUrl: url });
    }

    function getRemoteImageHtml(image, imageType) {

        var html = '';

        html += '<div class="remoteImageContainer">';

        var cssClass = "remoteImage lazy";

        if (imageType == "Backdrop" || imageType == "Art" || imageType == "Thumb" || imageType == "Logo") {
            cssClass += " remoteBackdropImage";
        }
        else if (imageType == "Banner") {
            cssClass += " remoteBannerImage";
        }
        else if (imageType == "Disc") {
            cssClass += " remoteDiscImage";
        }
        else {

            if (currentItemType == "Episode") {
                cssClass += " remoteBackdropImage";
            }
            else if (currentItemType == "MusicAlbum" || currentItemType == "MusicArtist") {
                cssClass += " remoteDiscImage";
            }
            else {
                cssClass += " remotePosterImage";
            }
        }

        var displayUrl = getDisplayUrl(image.ThumbnailUrl || image.Url);

        html += '<a target="_blank" href="' + getDisplayUrl(image.Url) + '" class="' + cssClass + '" data-src="' + displayUrl + '">';
        html += '</a>';

        html += '<div class="remoteImageDetails">';

        html += '<div class="remoteImageDetailText">';
        html += image.ProviderName;
        html += '</div>';

        if (image.Width || image.Height || image.Language) {

            html += '<div class="remoteImageDetailText">';

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

            html += '<div class="remoteImageDetailText">';

            if (image.RatingType == "Likes") {
                html += image.CommunityRating + (image.CommunityRating == 1 ? " like" : " likes");
            } else {

                if (image.CommunityRating) {
                    html += image.CommunityRating.toFixed(1);

                    if (image.VoteCount) {
                        html += ' • ' + image.VoteCount + (image.VoteCount == 1 ? " vote" : " votes");
                    }
                } else {
                    html += "Unrated";
                }
            }

            html += '</div>';
        }

        html += '<button is="paper-icon-button-light" class="btnDownloadRemoteImage" raised data-imageprovider="' + image.ProviderName + '" data-imageurl="' + image.Url + '" data-imagetype="' + image.Type + '" title="' + Globalize.translate('ButtonDownload') + '"><iron-icon icon="cloud-download"></iron-icon></button>';

        html += '</div>';
        html += '</div>';

        return html;
    }

    function initEditor(page) {

        $('#selectBrowsableImageType', page).on('change', function () {

            browsableImageType = this.value;
            browsableImageStartIndex = 0;
            selectedProvider = null;

            reloadBrowsableImages(page);
        });

        $('#selectImageProvider', page).on('change', function () {

            browsableImageStartIndex = 0;
            selectedProvider = this.value;

            reloadBrowsableImages(page);
        });

        $('#chkAllLanguages', page).on('change', function () {

            browsableImageStartIndex = 0;

            reloadBrowsableImages(page);
        });
    }

    function showEditor(itemId, itemType) {

        Dashboard.showLoadingMsg();

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/imagedownloader/imagedownloader.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            currentItemId = itemId;
            currentItemType = itemType;

            var dlg = dialogHelper.createDialog({
                size: 'fullscreen-border',
                lockScroll: true
            });

            var theme = 'b';

            dlg.classList.add('ui-body-' + theme);
            dlg.classList.add('background-theme-' + theme);
            dlg.classList.add('popupEditor');

            var html = '';
            html += '<h2 class="dialogHeader">';
            html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog" tabindex="-1"></paper-fab>';
            html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + Globalize.translate('HeaderSearch') + '</div>';
            html += '</h2>';

            html += '<div class="editorContent">';
            html += Globalize.translateDocument(template);
            html += '</div>';

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            // Has to be assigned a z-index after the call to .open() 
            $(dlg).on('close', onDialogClosed);

            dialogHelper.open(dlg);

            var editorContent = dlg.querySelector('.editorContent');
            initEditor(editorContent);

            $('.btnCloseDialog', dlg).on('click', function () {

                dialogHelper.close(dlg);
            });

            reloadBrowsableImages(editorContent);
        }

        xhr.send();
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    return {
        show: function (itemId, itemType, imageType) {

            var deferred = jQuery.Deferred();

            currentDeferred = deferred;
            hasChanges = false;
            browsableImageStartIndex = 0;
            browsableImageType = imageType || 'Primary';
            selectedProvider = null;

            showEditor(itemId, itemType);
            return deferred.promise();
        }
    };
});