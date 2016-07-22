define(['dialogHelper', 'css!css/metadataeditor.css', 'emby-button', 'paper-icon-button-light'], function (dialogHelper) {

    var currentItem;
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

    function addListeners(elems, eventName, fn) {

        for (var i = 0, length = elems.length; i < length; i++) {

            elems[i].addEventListener(eventName, fn);
        }
    }

    function reloadItem(page, item) {

        currentItem = item;

        ApiClient.getRemoteImageProviders(getBaseRemoteOptions()).then(function (providers) {

            var btnBrowseAllImages = page.querySelectorAll('.btnBrowseAllImages');
            for (var i = 0, length = btnBrowseAllImages.length; i < length; i++) {

                if (providers.length) {
                    btnBrowseAllImages[i].classList.remove('hide');
                } else {
                    btnBrowseAllImages[i].classList.add('hide');
                }
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
                    html += '<button is="paper-icon-button-light" class="btnMoveImage autoSize" data-imagetype="' + image.ImageType + '" data-index="' + image.ImageIndex + '" data-newindex="' + (image.ImageIndex - 1) + '" title="' + Globalize.translate('ButtonMoveLeft') + '"><i class="md-icon">chevron_left</i></button>';
                } else {
                    html += '<button is="paper-icon-button-light" class="autoSize" disabled title="' + Globalize.translate('ButtonMoveLeft') + '"><i class="md-icon">chevron_left</i></button>';
                }

                if (i < length - 1) {
                    html += '<button is="paper-icon-button-light" class="btnMoveImage autoSize" data-imagetype="' + image.ImageType + '" data-index="' + image.ImageIndex + '" data-newindex="' + (image.ImageIndex + 1) + '" title="' + Globalize.translate('ButtonMoveRight') + '"><i class="md-icon">chevron_right</i></button>';
                } else {
                    html += '<button is="paper-icon-button-light" class="autoSize" disabled title="' + Globalize.translate('ButtonMoveRight') + '"><i class="md-icon">chevron_right</i></button>';
                }
            }
            else {
                if (imageProviders.length) {
                    html += '<button is="paper-icon-button-light" data-imagetype="' + image.ImageType + '" class="btnSearchImages autoSize" title="' + Globalize.translate('ButtonBrowseOnlineImages') + '"><i class="md-icon">search</i></button>';
                }
            }

            html += '<button is="paper-icon-button-light" data-imagetype="' + image.ImageType + '" data-index="' + (image.ImageIndex != null ? image.ImageIndex : "null") + '" class="btnDeleteImage autoSize" title="' + Globalize.translate('Delete') + '"><i class="md-icon">delete</i></button>';

            html += '</div>';

            html += '</div>';
            html += '</div>';
            html += '</div>';
        }

        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);

        addListeners(elem.querySelectorAll('.btnSearchImages'), 'click', function () {
            showImageDownloader(page, this.getAttribute('data-imagetype'));
        });

        addListeners(elem.querySelectorAll('.btnDeleteImage'), 'click', function () {
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

        addListeners(elem.querySelectorAll('.btnMoveImage'), 'click', function () {
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
            page.querySelector('#backdropsContainer', page).classList.remove('hide');
            renderImages(page, item, images, imageProviders, page.querySelector('#backdrops'));
        } else {
            page.querySelector('#backdropsContainer', page).classList.add('hide');
        }
    }

    function renderScreenshots(page, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType == "Screenshot";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            page.querySelector('#screenshotsContainer', page).classList.remove('hide');
            renderImages(page, item, images, imageProviders, page.querySelector('#screenshots'));
        } else {
            page.querySelector('#screenshotsContainer', page).classList.add('hide');
        }
    }

    function showImageDownloader(page, imageType) {
        require(['components/imagedownloader/imagedownloader'], function (ImageDownloader) {

            ImageDownloader.show(currentItem.Id, currentItem.Type, imageType).then(function () {

                hasChanges = true;
                reload(page);
            });
        });
    }

    function initEditor(page, options) {

        addListeners(page.querySelectorAll('.btnOpenUploadMenu'), 'click', function () {
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

        addListeners(page.querySelectorAll('.btnBrowseAllImages'), 'click', function () {
            showImageDownloader(page, this.getAttribute('data-imagetype') || 'Primary');
        });
    }

    function showEditor(itemId, options, resolve, reject) {

        options = options || {};

        Dashboard.showLoadingMsg();

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/imageeditor/imageeditor.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(function (item) {

                var dlg = dialogHelper.createDialog({
                    size: 'fullscreen-border',
                    removeOnClose: true
                });

                var theme = options.theme || 'b';

                dlg.classList.add('ui-body-' + theme);
                dlg.classList.add('background-theme-' + theme);
                dlg.classList.add('popupEditor');

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<button type="button" is="emby-button" icon="arrow-back" class="fab mini btnCloseDialog autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + item.Name + '</div>';
                html += '</h2>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                initEditor(dlg, options);

                // Has to be assigned a z-index after the call to .open() 
                dlg.addEventListener('close', function () {

                    Dashboard.hideLoadingMsg();

                    if (hasChanges) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                dialogHelper.open(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                reload(editorContent, item);

                dlg.querySelector('.btnCloseDialog').addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });
            });
        }

        xhr.send();
    }

    return {
        show: function (itemId, options) {

            return new Promise(function (resolve, reject) {

                hasChanges = false;

                showEditor(itemId, options, resolve, reject);
            });
        }
    };
});