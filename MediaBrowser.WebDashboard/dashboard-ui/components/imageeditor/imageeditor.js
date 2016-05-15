define(['dialogHelper', 'jQuery', 'css!css/metadataeditor.css', 'paper-fab', 'paper-icon-button-light'], function (dialogHelper, $) {

    var currentItem;
    var currentDeferred;
    var hasChanges = false;

    function getBaseRemoteOptions() {

        var options = {};

        options.itemId = currentItem.Id;

        return options;
    }

    function reload(page, item) {

        Dashboard.showLoadingMsg();

        if (item) {
            reloadItem(page, item);
        }
        else {
            ApiClient.getItem(Dashboard.getCurrentUserId(), currentItem.Id).then(function (item) {
                reloadItem(page, item);
            });
        }
    }

    function reloadItem(page, item) {

        currentItem = item;

        ApiClient.getRemoteImageProviders(getBaseRemoteOptions()).then(function (providers) {

            if (providers.length) {
                $('.btnBrowseAllImages', page).removeClass('hide');
            } else {
                $('.btnBrowseAllImages', page).addClass('hide');
            }

            ApiClient.getItemImageInfos(currentItem.Id).then(function (imageInfos) {

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
            html += '<div class="editorTileInner">';

            var height = 150;

            html += '<div style="height:' + height + 'px;vertical-align:top;background-repeat:no-repeat;background-position:center bottom;background-size:contain;" class="lazy" data-src="' + LibraryBrowser.getImageUrl(currentItem, image.ImageType, image.ImageIndex, { height: height }) + '"></div>';

            html += '<div class="editorTileFooter">';

            if (image.ImageType !== "Backdrop" && image.ImageType !== "Screenshot") {
                html += '<h3>' + image.ImageType + '</h3>';
            }

            if (image.Width && image.Height) {
                html += '<p>' + image.Width + ' X ' + image.Height + '</p>';
            } else {
                html += '<p>&nbsp;</p>';
            }

            html += '<div>';

            if (image.ImageType == "Backdrop" || image.ImageType == "Screenshot") {

                if (i > 0) {
                    html += '<button is="paper-icon-button-light"" class="btnMoveImage" data-imagetype="' + image.ImageType + '" data-index="' + image.ImageIndex + '" data-newindex="' + (image.ImageIndex - 1) + '" title="' + Globalize.translate('ButtonMoveLeft') + '"><iron-icon icon="chevron-left"></iron-icon></button>';
                } else {
                    html += '<button is="paper-icon-button-light"" disabled title="' + Globalize.translate('ButtonMoveLeft') + '"><iron-icon icon="chevron-left"></iron-icon></button>';
                }

                if (i < length - 1) {
                    html += '<button is="paper-icon-button-light"" class="btnMoveImage" data-imagetype="' + image.ImageType + '" data-index="' + image.ImageIndex + '" data-newindex="' + (image.ImageIndex + 1) + '" title="' + Globalize.translate('ButtonMoveRight') + '"><iron-icon icon="chevron-right"></iron-icon></button>';
                } else {
                    html += '<button is="paper-icon-button-light"" disabled title="' + Globalize.translate('ButtonMoveRight') + '"><iron-icon icon="chevron-right"></iron-icon></button>';
                }
            }
            else {
                if (imageProviders.length) {
                    html += '<button is="paper-icon-button-light"" data-imagetype="' + image.ImageType + '" class="btnSearchImages" title="' + Globalize.translate('ButtonBrowseOnlineImages') + '"><iron-icon icon="search"></iron-icon></button>';
                }
            }

            html += '<button is="paper-icon-button-light"" data-imagetype="' + image.ImageType + '" data-index="' + (image.ImageIndex != null ? image.ImageIndex : "null") + '" class="btnDeleteImage" title="' + Globalize.translate('Delete') + '"><iron-icon icon="delete"></iron-icon></button>';

            html += '</div>';

            html += '</div>';
            html += '</div>';
            html += '</div>';
        }

        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);

        $('.btnSearchImages', elem).on('click', function () {
            showImageDownloader(page, this.getAttribute('data-imagetype'));
        });

        $('.btnDeleteImage', elem).on('click', function () {

            var type = this.getAttribute('data-imagetype');
            var index = this.getAttribute('data-index');
            index = index == "null" ? null : parseInt(index);

            require(['confirm'], function (confirm) {

                confirm(Globalize.translate('DeleteImageConfirmation'), Globalize.translate('HeaderDeleteImage')).then(function () {

                    ApiClient.deleteItemImage(currentItem.Id, type, index).then(function () {

                        hasChanges = true;
                        reload(page);

                    });
                });
            });
        });

        $('.btnMoveImage', elem).on('click', function () {
            var type = this.getAttribute('data-imagetype');
            var index = parseInt(this.getAttribute('data-index'));
            var newIndex = parseInt(this.getAttribute('data-newindex'));
            ApiClient.updateItemImageIndex(currentItem.Id, type, index, newIndex).then(function () {

                hasChanges = true;
                reload(page);

            });
        });
    }

    function renderStandardImages(page, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType != "Screenshot" && i.ImageType != "Backdrop" && i.ImageType != "Chapter";
        });

        renderImages(page, item, images, imageProviders, page.querySelector('#images'));
    }

    function renderBackdrops(page, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType == "Backdrop";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            $('#backdropsContainer', page).show();
            renderImages(page, item, images, imageProviders, page.querySelector('#backdrops'));
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

    function showImageDownloader(page, imageType) {
        require(['components/imagedownloader/imagedownloader'], function (ImageDownloader) {

            ImageDownloader.show(currentItem.Id, currentItem.Type, imageType).then(function (hasChanged) {

                if (hasChanged) {
                    hasChanges = true;
                    reload(page);
                }
            });
        });
    }

    function initEditor(page, options) {

        $('.btnOpenUploadMenu', page).on('click', function () {

            var imageType = this.getAttribute('data-imagetype');

            require(['components/imageuploader/imageuploader'], function (imageUploader) {

                imageUploader.show(currentItem.Id, {

                    theme: options.theme,
                    imageType: imageType

                }).then(function (hasChanged) {

                    if (hasChanged) {
                        hasChanges = true;
                        reload(page);
                    }
                });
            });
        });

        $('.btnBrowseAllImages', page).on('click', function () {
            showImageDownloader(page, this.getAttribute('data-imagetype') || 'Primary');
        });
    }

    function showEditor(itemId, options) {

        options = options || {};

        Dashboard.showLoadingMsg();

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/imageeditor/imageeditor.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(function (item) {

                var dlg = dialogHelper.createDialog({
                    size: 'fullscreen-border'
                });

                var theme = options.theme || 'b';

                dlg.classList.add('ui-body-' + theme);
                dlg.classList.add('background-theme-' + theme);
                dlg.classList.add('popupEditor');

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog" tabindex="-1"></paper-fab>';
                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + item.Name + '</div>';
                html += '</h2>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                initEditor(dlg, options);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('close', onDialogClosed);

                dialogHelper.open(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                reload(editorContent, item);

                $('.btnCloseDialog', dlg).on('click', function () {

                    dialogHelper.close(dlg);
                });
            });
        }

        xhr.send();
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    return {
        show: function (itemId, options) {

            var deferred = jQuery.Deferred();

            currentDeferred = deferred;
            hasChanges = false;

            showEditor(itemId, options);
            return deferred.promise();
        }
    };
});