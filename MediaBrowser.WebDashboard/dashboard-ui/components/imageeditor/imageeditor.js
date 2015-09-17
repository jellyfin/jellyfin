(function ($, document, window, FileReader, escape) {

    var currentItem;

    var browsableImagePageSize = 10;
    var browsableImageStartIndex = 0;
    var browsableImageType = 'Primary';
    var selectedProvider;

    function getBaseRemoteOptions() {

        var options = {};

        options.itemId = currentItem.Id;

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

        ApiClient.getAvailableRemoteImages(options).done(function (result) {

            renderRemoteImages(page, currentItem, result, browsableImageType, options.startIndex, options.limit);

            $('#selectBrowsableImageType', page).val(browsableImageType);

            var providersHtml = result.Providers.map(function (p) {
                return '<option value="' + p + '">' + p + '</option>';
            });

            $('#selectImageProvider', page).html('<option value="">' + Globalize.translate('LabelAll') + '</option>' + providersHtml).val(provider);

            Dashboard.hideLoadingMsg();
        });

    }

    function renderRemoteImages(page, item, imagesResult, imageType, startIndex, limit) {
        $('.availableImagesPaging', page).html(getPagingHtml(startIndex, limit, imagesResult.TotalRecordCount)).trigger('create');

        var html = '';

        for (var i = 0, length = imagesResult.Images.length; i < length; i++) {

            html += getRemoteImageHtml(imagesResult.Images[i], imageType);
        }

        $('.availableImagesList', page).html(html).trigger('create');

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

    function downloadRemoteImage(page, url, type, provider) {

        var options = getBaseRemoteOptions();

        options.Type = type;
        options.ImageUrl = url;
        options.ProviderName = provider;

        Dashboard.showLoadingMsg();

        ApiClient.downloadRemoteImage(options).done(function () {

            $('.popupDownload', page).popup("close");
            reload(page);
        });
    }

    function getDisplayUrl(url) {
        return ApiClient.getUrl("Images/Remote", { imageUrl: url });
    }

    function getRemoteImageHtml(image, imageType) {

        var html = '';

        html += '<div class="remoteImageContainer">';

        var cssClass = "remoteImage";

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

            if (currentItem.Type == "Episode") {
                cssClass += " remoteBackdropImage";
            }
            else if (currentItem.Type == "MusicAlbum" || currentItem.Type == "MusicArtist") {
                cssClass += " remoteDiscImage";
            }
            else {
                cssClass += " remotePosterImage";
            }
        }

        var displayUrl = getDisplayUrl(image.ThumbnailUrl || image.Url);

        html += '<a target="_blank" href="' + getDisplayUrl(image.Url) + '" class="' + cssClass + '" style="background-image:url(\'' + displayUrl + '\');">';
        html += '</a>';

        html += '<div class="remoteImageDetails">';
        html += image.ProviderName;
        html += '</div>';

        if (image.Width || image.Height || image.Language) {

            html += '<div class="remoteImageDetails">';

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

            html += '<div class="remoteImageDetails">';

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

        html += '<div><button class="btnDownloadRemoteImage" data-imageprovider="' + image.ProviderName + '" data-imageurl="' + image.Url + '" data-imagetype="' + image.Type + '" type="button" data-icon="arrow-d" data-mini="true">' + Globalize.translate('ButtonDownload') + '</button></div>';

        html += '</div>';

        return html;
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

            html += '<paper-icon-button icon="arrow-back" title="' + Globalize.translate('ButtonPreviousPage') + '" class="btnPreviousPage" ' + (startIndex ? '' : 'disabled') + '></paper-icon-button>';
            html += '<paper-icon-button icon="arrow-forward" title="' + Globalize.translate('ButtonNextPage') + '" class="btnNextPage" ' + (startIndex + limit > totalRecordCount ? 'disabled' : '') + '></paper-icon-button>';
            html += '</div>';
        }

        html += '</div>';

        return html;
    }

    function reload(page, item) {

        Dashboard.showLoadingMsg();

        browsableImageStartIndex = 0;
        browsableImageType = 'Primary';
        selectedProvider = null;

        if (item) {
            reloadItem(page, item);
        }
        else {
            ApiClient.getItem(Dashboard.getCurrentUserId(), currentItem.Id).done(function (item) {
                reloadItem(page, item);
            });
        }
    }

    function reloadItem(page, item) {

        currentItem = item;

        ApiClient.getRemoteImageProviders(getBaseRemoteOptions()).done(function (providers) {

            if (providers.length) {
                $('.lnkBrowseAllImages', page).removeClass('hide');
            } else {
                $('.lnkBrowseAllImages', page).addClass('hide');
            }

            ApiClient.getItemImageInfos(currentItem.Id).done(function (imageInfos) {

                renderStandardImages(page, item, imageInfos, providers);
                renderBackdrops(page, item, imageInfos, providers);
                renderScreenshots(page, item, imageInfos, providers);
                Dashboard.hideLoadingMsg();
            });
        });
    }

    function renderImages(page, item, images, imageProviders, elem) {

        var html = '';

        for (var i = 0, length = images.length; i < length; i++) {

            var image = images[i];

            html += '<div class="editorTile imageEditorTile">';

            if (image.ImageType !== "Backdrop" && image.ImageType !== "Screenshot") {
                html += '<h3>' + image.ImageType + '</h3>';
            }

            var height = 150;

            html += '<div style="height:' + height + 'px;vertical-align:top;background-repeat:no-repeat;background-position:center center;background-size:contain;background-image:url(\'' + LibraryBrowser.getImageUrl(currentItem, image.ImageType, image.ImageIndex, { height: height }) + '\');"></div>';

            html += '<div class="editorTileInner">';

            if (image.Width && image.Height) {
                html += '<p>' + image.Width + ' X ' + image.Height + '</p>';
            } else {
                html += '<p>&nbsp;</p>';
            }

            html += '<div>';

            if (image.ImageType == "Backdrop" || image.ImageType == "Screenshot") {

                if (i > 0) {
                    html += '<paper-icon-button icon="chevron-left" onclick="EditItemImagesPage.moveImage(\'' + image.ImageType + '\', ' + image.ImageIndex + ', ' + (i - 1) + ');" title="' + Globalize.translate('ButtonMoveLeft') + '"></paper-icon-button>';
                } else {
                    html += '<paper-icon-button icon="chevron-left" disabled title="' + Globalize.translate('ButtonMoveLeft') + '"></paper-icon-button>';
                }

                if (i < length - 1) {
                    html += '<paper-icon-button icon="chevron-right" onclick="EditItemImagesPage.moveImage(\'' + image.ImageType + '\', ' + image.ImageIndex + ', ' + (i + 1) + ');" title="' + Globalize.translate('ButtonMoveRight') + '"></paper-icon-button>';
                } else {
                    html += '<paper-icon-button icon="chevron-right" disabled title="' + Globalize.translate('ButtonMoveRight') + '"></paper-icon-button>';
                }
            }

            if (imageProviders.length) {
                html += '<paper-icon-button icon="cloud" onclick="EditItemImagesPage.showDownloadMenu(\'' + image.ImageType + '\');" title="' + Globalize.translate('ButtonBrowseOnlineImages') + '"></paper-icon-button>';
            }

            html += '<paper-icon-button icon="delete" onclick="EditItemImagesPage.deleteImage(\'' + image.ImageType + '\', ' + (image.ImageIndex != null ? image.ImageIndex : "null") + ');" title="' + Globalize.translate('Delete') + '"></paper-icon-button>';

            html += '</div>';

            html += '</div>';

            html += '</div>';
        }

        elem.html(html).trigger('create');
    }

    function renderStandardImages(page, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType != "Screenshot" && i.ImageType != "Backdrop" && i.ImageType != "Chapter";
        });

        renderImages(page, item, images, imageProviders, $('#images', page));
    }

    function renderBackdrops(page, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType == "Backdrop";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            $('#backdropsContainer', page).show();
            renderImages(page, item, images, imageProviders, $('#backdrops', page));
        } else {
            $('#backdropsContainer', page).hide();
        }
    }

    function renderScreenshots(page, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType == "Screenshot";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            $('#screenshotsContainer', page).show();
            renderImages(page, item, images, imageProviders, $('#screenshots', page));
        } else {
            $('#screenshotsContainer', page).hide();
        }
    }

    function editItemImages() {

        var self = this;

        self.deleteImage = function (type, index) {

            var page = $.mobile.activePage;

            Dashboard.confirm(Globalize.translate('DeleteImageConfirmation'), Globalize.translate('HeaderDeleteImage'), function (result) {

                if (result) {
                    ApiClient.deleteItemImage(currentItem.Id, type, index).done(function () {

                        reload(page);

                    });
                }

            });
        };

        self.moveImage = function (type, index, newIndex) {

            var page = $.mobile.activePage;

            ApiClient.updateItemImageIndex(currentItem.Id, type, index, newIndex).done(function () {

                reload(page);

            });


        };

        self.showDownloadMenu = function (type) {
            browsableImageStartIndex = 0;
            browsableImageType = type;

            var page = $.mobile.activePage;

            selectedProvider = null;
            $('.popupDownload', page).popup('open');
            reloadBrowsableImages(page);
        };
    }

    window.EditItemImagesPage = new editItemImages();

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

        $('.btnOpenUploadMenu', page).on('click', function () {

            require(['components/imageuploader/imageuploader'], function () {

                ImageUploader.show(currentItem.Id).done(function (hasChanges) {

                    if (hasChanges) {
                        reload(page);
                    }
                });
            });
        });

        $('.btnBrowseAllImages', page).on('click', function () {

            selectedProvider = null;
            browsableImageType = 'Primary';
            $('.popupDownload', page).popup('open');
            reloadBrowsableImages(page);
        });
    }

    function showEditor(itemId) {

        Dashboard.showLoadingMsg();

        ApiClient.ajax({

            type: 'GET',
            url: 'components/imageeditor/imageeditor.template.html'

        }).done(function (template) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {

                var dlg = document.createElement('paper-dialog');

                dlg.setAttribute('with-backdrop', 'with-backdrop');
                dlg.setAttribute('role', 'alertdialog');
                dlg.entryAnimation = 'scale-up-animation';
                dlg.exitAnimation = 'fade-out-animation';
                dlg.classList.add('fullscreen-editor-paper-dialog');
                dlg.classList.add('ui-body-b');

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" class="mini btnCloseDialog"></paper-fab>';
                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + item.Name + '</div>';
                html += '</h2>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                initEditor(dlg);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('iron-overlay-closed', onDialogClosed);

                document.body.classList.add('bodyWithPopupOpen');
                PaperDialogHelper.openWithHash(dlg, 'imageeditor');

                var editorContent = dlg.querySelector('.editorContent');
                reload(editorContent, item);

                $('.btnCloseDialog', dlg).on('click', closeDialog);
            });
        });
    }

    function closeDialog() {

        history.back();
    }

    function onDialogClosed() {

        document.body.classList.remove('bodyWithPopupOpen');
        $(this).remove();
        Dashboard.hideLoadingMsg();
    }

    window.ImageEditor = {
        show: function (itemId) {

            require(['components/paperdialoghelper', 'jqmpopup'], function () {

                Dashboard.importCss('css/metadataeditor.css');
                showEditor(itemId);
            });
        }
    };

})(jQuery, document, window, window.FileReader, escape);