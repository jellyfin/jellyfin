define(['dialogHelper', 'connectionManager', 'loading', 'dom', 'layoutManager', 'focusManager', 'globalize', 'scrollHelper', 'imageLoader', 'require', 'cardStyle', 'formDialogStyle', 'emby-button', 'paper-icon-button-light'], function (dialogHelper, connectionManager, loading, dom, layoutManager, focusManager, globalize, scrollHelper, imageLoader, require) {
    'use strict';

    var currentItem;
    var hasChanges = false;

    function getBaseRemoteOptions() {

        var options = {};

        options.itemId = currentItem.Id;

        return options;
    }

    function reload(page, item, focusContext) {

        loading.show();

        var apiClient;

        if (item) {
            apiClient = connectionManager.getApiClient(item.ServerId);
            reloadItem(page, item, apiClient, focusContext);
        }
        else {

            apiClient = connectionManager.getApiClient(currentItem.ServerId);
            apiClient.getItem(apiClient.getCurrentUserId(), currentItem.Id).then(function (item) {
                reloadItem(page, item, apiClient, focusContext);
            });
        }
    }

    function addListeners(container, className, eventName, fn) {

        container.addEventListener(eventName, function (e) {
            var elem = dom.parentWithClass(e.target, className);
            if (elem) {
                fn.call(elem, e);
            }
        });
    }

    function reloadItem(page, item, apiClient, focusContext) {

        currentItem = item;

        apiClient.getRemoteImageProviders(getBaseRemoteOptions()).then(function (providers) {

            var btnBrowseAllImages = page.querySelectorAll('.btnBrowseAllImages');
            for (var i = 0, length = btnBrowseAllImages.length; i < length; i++) {

                if (providers.length) {
                    btnBrowseAllImages[i].classList.remove('hide');
                } else {
                    btnBrowseAllImages[i].classList.add('hide');
                }
            }


            apiClient.getItemImageInfos(currentItem.Id).then(function (imageInfos) {

                renderStandardImages(page, apiClient, item, imageInfos, providers);
                renderBackdrops(page, apiClient, item, imageInfos, providers);
                renderScreenshots(page, apiClient, item, imageInfos, providers);
                loading.hide();

                if (layoutManager.tv) {
                    focusManager.autoFocus((focusContext || page));
                }
            });
        });
    }

    function getImageUrl(item, apiClient, type, index, options) {

        options = options || {};
        options.type = type;
        options.index = index;

        if (type === 'Backdrop') {
            options.tag = item.BackdropImageTags[index];
        } else if (type === 'Screenshot') {
            options.tag = item.ScreenshotImageTags[index];
        } else if (type === 'Primary') {
            options.tag = item.PrimaryImageTag || item.ImageTags[type];
        } else {
            options.tag = item.ImageTags[type];
        }

        // For search hints
        return apiClient.getScaledImageUrl(item.Id || item.ItemId, options);
    }

    function getCardHtml(image, index, numImages, apiClient, imageProviders, imageSize, tagName, enableFooterButtons) {

        var html = '';

        var cssClass = "card scalableCard imageEditorCard";
        var cardBoxCssClass = 'cardBox visualCardBox';

        cssClass += " backdropCard backdropCard-scalable";

        if (tagName === 'button') {
            cssClass += ' card-focusscale btnImageCard';
            cardBoxCssClass += ' cardBox-focustransform cardBox-focustransform-transition';

            html += '<button type="button" class="' + cssClass + '"';
        } else {
            html += '<div class="' + cssClass + '"';
        }

        html += ' data-id="' + currentItem.Id + '" data-serverid="' + apiClient.serverId() + '" data-index="' + index + '" data-numimages="' + numImages + '" data-imagetype="' + image.ImageType + '" data-providers="' + imageProviders.length + '"';

        html += '>';

        html += '<div class="' + cardBoxCssClass + '">';
        html += '<div class="cardScalable visualCardBox-cardScalable" style="background-color:transparent;">';
        html += '<div class="cardPadder-backdrop"></div>';

        html += '<div class="cardContent">';

        var imageUrl = getImageUrl(currentItem, apiClient, image.ImageType, image.ImageIndex, { maxWidth: imageSize });

        html += '<div class="cardImageContainer" style="background-image:url(\'' + imageUrl + '\');background-position:center bottom;"></div>';

        html += '</div>';
        html += '</div>';

        html += '<div class="cardFooter visualCardBox-cardFooter">';

        html += '<h3 class="cardText cardTextCentered" style="margin:0;">' + image.ImageType + '</h3>';

        html += '<div class="cardText cardText-secondary cardTextCentered">';
        if (image.Width && image.Height) {
            html += image.Width + ' X ' + image.Height;
        } else {
            html += '&nbsp;';
        }
        html += '</div>';

        if (enableFooterButtons) {
            html += '<div class="cardText cardTextCentered">';

            if (image.ImageType === "Backdrop" || image.ImageType === "Screenshot") {

                if (index > 0) {
                    html += '<button type="button" is="paper-icon-button-light" class="btnMoveImage autoSize" data-imagetype="' + image.ImageType + '" data-index="' + image.ImageIndex + '" data-newindex="' + (image.ImageIndex - 1) + '" title="' + globalize.translate('sharedcomponents#MoveLeft') + '"><i class="md-icon">chevron_left</i></button>';
                } else {
                    html += '<button type="button" is="paper-icon-button-light" class="autoSize" disabled title="' + globalize.translate('sharedcomponents#MoveLeft') + '"><i class="md-icon">chevron_left</i></button>';
                }

                if (index < numImages - 1) {
                    html += '<button type="button" is="paper-icon-button-light" class="btnMoveImage autoSize" data-imagetype="' + image.ImageType + '" data-index="' + image.ImageIndex + '" data-newindex="' + (image.ImageIndex + 1) + '" title="' + globalize.translate('sharedcomponents#MoveRight') + '"><i class="md-icon">chevron_right</i></button>';
                } else {
                    html += '<button type="button" is="paper-icon-button-light" class="autoSize" disabled title="' + globalize.translate('sharedcomponents#MoveRight') + '"><i class="md-icon">chevron_right</i></button>';
                }
            }
            else {
                if (imageProviders.length) {
                    html += '<button type="button" is="paper-icon-button-light" data-imagetype="' + image.ImageType + '" class="btnSearchImages autoSize" title="' + globalize.translate('sharedcomponents#Search') + '"><i class="md-icon">search</i></button>';
                }
            }

            html += '<button type="button" is="paper-icon-button-light" data-imagetype="' + image.ImageType + '" data-index="' + (image.ImageIndex != null ? image.ImageIndex : "null") + '" class="btnDeleteImage autoSize" title="' + globalize.translate('sharedcomponents#Delete') + '"><i class="md-icon">delete</i></button>';
            html += '</div>';
        }

        html += '</div>';
        html += '</div>';
        html += '</div>';
        html += '</' + tagName + '>';

        return html;
    }

    function deleteImage(context, itemId, type, index, apiClient, enableConfirmation) {

        var afterConfirm = function () {
            apiClient.deleteItemImage(itemId, type, index).then(function () {

                hasChanges = true;
                reload(context);

            });
        };

        if (!enableConfirmation) {
            afterConfirm();
            return;
        }

        require(['confirm'], function (confirm) {

            confirm({
                
                text: globalize.translate('sharedcomponents#ConfirmDeleteImage'),
                confirmText: globalize.translate('sharedcomponents#Delete'),
                primary: 'cancel'

            }).then(afterConfirm);
        });
    }

    function moveImage(context, apiClient, itemId, type, index, newIndex, focusContext) {

        apiClient.updateItemImageIndex(itemId, type, index, newIndex).then(function () {

            hasChanges = true;
            reload(context, null, focusContext);
        }, function() {

            require(['alert'], function (alert) {
                alert(globalize.translate('sharedcomponents#DefaultErrorMessage'));
            });
        });
    }

    function renderImages(page, item, apiClient, images, imageProviders, elem) {

        var html = '';

        var imageSize = 300;
        var windowSize = dom.getWindowSize();
        if (windowSize.innerWidth >= 1280) {
            imageSize = Math.round(windowSize.innerWidth / 4);
        }

        var tagName = layoutManager.tv ? 'button' : 'div';
        var enableFooterButtons = !layoutManager.tv;

        for (var i = 0, length = images.length; i < length; i++) {

            var image = images[i];

            html += getCardHtml(image, i, length, apiClient, imageProviders, imageSize, tagName, enableFooterButtons);
        }

        elem.innerHTML = html;
        imageLoader.lazyChildren(elem);
    }

    function renderStandardImages(page, apiClient, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType !== "Screenshot" && i.ImageType !== "Backdrop" && i.ImageType !== "Chapter";
        });

        renderImages(page, item, apiClient, images, imageProviders, page.querySelector('#images'));
    }

    function renderBackdrops(page, apiClient, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType === "Backdrop";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            page.querySelector('#backdropsContainer', page).classList.remove('hide');
            renderImages(page, item, apiClient, images, imageProviders, page.querySelector('#backdrops'));
        } else {
            page.querySelector('#backdropsContainer', page).classList.add('hide');
        }
    }

    function renderScreenshots(page, apiClient, item, imageInfos, imageProviders) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType === "Screenshot";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            page.querySelector('#screenshotsContainer', page).classList.remove('hide');
            renderImages(page, item, apiClient, images, imageProviders, page.querySelector('#screenshots'));
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
        }, function () {
            require(['alert'], function (alert) {
                alert('This feature is coming soon to Emby Theater.');
            });
        });
    }

    function showActionSheet(context, imageCard) {

        var itemId = imageCard.getAttribute('data-id');
        var serverId = imageCard.getAttribute('data-serverid');
        var apiClient = connectionManager.getApiClient(serverId);

        var type = imageCard.getAttribute('data-imagetype');
        var index = parseInt(imageCard.getAttribute('data-index'));
        var providerCount = parseInt(imageCard.getAttribute('data-providers'));
        var numImages = parseInt(imageCard.getAttribute('data-numimages'));

        require(['actionsheet'], function (actionSheet) {

            var commands = [];

            commands.push({
                name: globalize.translate('sharedcomponents#Delete'),
                id: 'delete'
            });

            if (type === 'Backdrop' || type === 'Screenshot') {
                if (index > 0) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#MoveLeft'),
                        id: 'moveleft'
                    });
                }

                if (index < numImages - 1) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#MoveRight'),
                        id: 'moveright'
                    });
                }
            }

            if (providerCount) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Search'),
                    id: 'search'
                });
            }

            actionSheet.show({

                items: commands,
                positionTo: imageCard

            }).then(function (id) {

                switch (id) {

                    case 'delete':
                        deleteImage(context, itemId, type, index, apiClient, false);
                        break;
                    case 'search':
                        showImageDownloader(context, type);
                        break;
                    case 'moveleft':
                        moveImage(context, apiClient, itemId, type, index, index - 1, dom.parentWithClass(imageCard, 'itemsContainer'));
                        break;
                    case 'moveright':
                        moveImage(context, apiClient, itemId, type, index, index + 1, dom.parentWithClass(imageCard, 'itemsContainer'));
                        break;
                    default:
                        break;
                }

            });
        });
    }

    function initEditor(context, options) {

        addListeners(context, 'btnOpenUploadMenu', 'click', function () {
            var imageType = this.getAttribute('data-imagetype');

            require(['components/imageuploader/imageuploader'], function (imageUploader) {

                imageUploader.show(currentItem.Id, {

                    theme: options.theme,
                    imageType: imageType

                }).then(function (hasChanged) {

                    if (hasChanged) {
                        hasChanges = true;
                        reload(context);
                    }
                });
            }, function () {
                require(['alert'], function (alert) {
                    alert('This feature is coming soon to Emby Theater.');
                });
            });
        });

        addListeners(context, 'btnSearchImages', 'click', function () {
            showImageDownloader(context, this.getAttribute('data-imagetype'));
        });

        addListeners(context, 'btnBrowseAllImages', 'click', function () {
            showImageDownloader(context, this.getAttribute('data-imagetype') || 'Primary');
        });

        addListeners(context, 'btnImageCard', 'click', function () {
            showActionSheet(context, this);
        });

        addListeners(context, 'btnDeleteImage', 'click', function () {
            var type = this.getAttribute('data-imagetype');
            var index = this.getAttribute('data-index');
            index = index === "null" ? null : parseInt(index);
            var apiClient = connectionManager.getApiClient(currentItem.ServerId);
            deleteImage(context, currentItem.Id, type, index, apiClient, true);
        });

        addListeners(context, 'btnMoveImage', 'click', function () {
            var type = this.getAttribute('data-imagetype');
            var index = this.getAttribute('data-index');
            var newIndex = this.getAttribute('data-newindex');
            var apiClient = connectionManager.getApiClient(currentItem.ServerId);
            moveImage(context, apiClient, currentItem.Id, type, index, newIndex, dom.parentWithClass(this, 'itemsContainer'));
        });
    }

    function showEditor(options, resolve, reject) {

        var itemId = options.itemId;
        var serverId = options.serverId;

        loading.show();

        require(['text!./imageeditor.template.html'], function (template) {
            var apiClient = connectionManager.getApiClient(serverId);
            apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function (item) {

                var dialogOptions = {
                    removeOnClose: true
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                } else {
                    dialogOptions.size = 'fullscreen-border';
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('formDialog');

                dlg.innerHTML = globalize.translateDocument(template, 'sharedcomponents');

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg, false);
                }

                initEditor(dlg, options);

                // Has to be assigned a z-index after the call to .open() 
                dlg.addEventListener('close', function () {

                    if (layoutManager.tv) {
                        scrollHelper.centerFocus.off(dlg, false);
                    }

                    loading.hide();

                    if (hasChanges) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                dialogHelper.open(dlg);

                reload(dlg, item);

                dlg.querySelector('.btnCancel').addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });
            });
        });
    }

    return {
        show: function (options) {

            return new Promise(function (resolve, reject) {

                hasChanges = false;

                showEditor(options, resolve, reject);
            });
        }
    };
});