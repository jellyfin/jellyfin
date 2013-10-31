(function ($, document, window, FileReader, escape) {

    var currentItem;
    var currentFile;

    var browsableImagePageSize = 10;
    var browsableImageStartIndex = 0;
    var browsableImageType = 'Primary';

    function updateTabs(page, item) {

        var query = MetadataEditor.getEditQueryString(item);

        $('#btnEditPeople', page).attr('href', 'edititempeople.html?' + query);
        $('#btnEditMetadata', page).attr('href', 'edititemmetadata.html?' + query);
    }

    function reloadBrowsableImages(page) {

        var options = {
            itemId: currentItem.Id,
            imageType: browsableImageType,
            startIndex: browsableImageStartIndex,
            limit: browsableImagePageSize
        };

        ApiClient.getAvailableRemoteImages(options).done(function (result) {

            renderRemoteImages(page, currentItem, result, browsableImageType, options.startIndex, options.limit);
        });

    }

    function renderRemoteImages(page, item, imagesResult, imageType, startIndex, limit) {

        $('.availableImagesPaging', page).html(getPagingHtml(startIndex, limit, imagesResult.TotalRecordCount)).trigger('create');

        var html = '';

        for (var i = 0, length = imagesResult.Images.length; i < length; i++) {

            html += getRemoteImageHtml(imagesResult.Images[i], imageType);
        }

        $('.availableImagesList', page).html(html).trigger('create');

        $('.selectPage', page).on('change', function () {
            browsableImageStartIndex = (parseInt(this.value) - 1) * browsableImagePageSize;
            reloadBrowsableImages(page);
        });

        $('.btnNextPage', page).on('click', function () {
            browsableImageStartIndex += browsableImagePageSize;
            reloadBrowsableImages(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            browsableImageStartIndex -= browsableImagePageSize;
            reloadBrowsableImages(page);
        });

    }


    function getRemoteImageHtml(image, imageType) {

        var html = '';

        html += '<div class="remoteImageContainer">';

        var cssClass = "remoteImage";

        if (imageType == "Backdrop") {
            cssClass += " remoteBackdropImage";
        }
        else {
            cssClass += " remotePosterImage";
        }

        html += '<div class="' + cssClass + '" style="background-image:url(\'' + image.Url + '\');">';
        html += '</div>';

        html += '<div class="remoteImageDetails">';
        html += image.ProviderName;
        html += '</div>';

        if (image.Width || image.Height) {

            html += '<div class="remoteImageDetails">';
            html += image.Width + 'x' + image.Height;
            
            if (image.Language) {

                html += ' • ' + image.Language;
            }
            html += '</div>';
        }

        if (image.CommunityRating) {
            html += '<div class="remoteImageDetails">';
            html += image.CommunityRating.toFixed(1);

            if (image.VoteCount) {
                html += ' • ' + image.VoteCount + ' votes';
            }

            html += '</div>';
        }

        html += '<div><button type="button" data-icon="save" data-mini="true">Download</button></div>';
        
        html += '</div>';

        return html;
    }

    function getPagingHtml(startIndex, limit, totalRecordCount) {

        var html = '';

        var pageCount = Math.ceil(totalRecordCount / limit);
        var pageNumber = (startIndex / limit) + 1;

        var dropdownHtml = '<select class="selectPage" data-enhance="false" data-role="none">';
        for (var i = 1; i <= pageCount; i++) {

            if (i == pageNumber) {
                dropdownHtml += '<option value="' + i + '" selected="selected">' + i + '</option>';
            } else {
                dropdownHtml += '<option value="' + i + '">' + i + '</option>';
            }
        }
        dropdownHtml += '</select>';

        var recordsEnd = Math.min(startIndex + limit, totalRecordCount);

        // 20 is the minimum page size
        var showControls = totalRecordCount > 20;

        html += '<div class="listPaging">';

        html += '<span style="margin-right: 10px;">';

        var startAtDisplay = totalRecordCount ? startIndex + 1 : 0;
        html += startAtDisplay + '-' + recordsEnd + ' of ' + totalRecordCount;

        if (showControls) {
            html += ', page ' + dropdownHtml + ' of ' + pageCount;
        }

        html += '</span>';

        if (showControls) {
            html += '<button data-icon="arrow-left" data-iconpos="notext" data-inline="true" data-mini="true" class="btnPreviousPage" ' + (startIndex ? '' : 'disabled') + '>Previous Page</button>';

            html += '<button data-icon="arrow-right" data-iconpos="notext" data-inline="true" data-mini="true" class="btnNextPage" ' + (startIndex + limit > totalRecordCount ? 'disabled' : '') + '>Next Page</button>';
        }

        html += '</div>';

        return html;
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        MetadataEditor.getItemPromise().done(function (item) {

            currentItem = item;

            LibraryBrowser.renderName(item, $('.itemName', page), true);

            updateTabs(page, item);

            if (item.Type == "Person" || item.Type == "Studio" || item.Type == "MusicGenre" || item.Type == "Genre" || item.Type == "Artist" || item.Type == "GameGenre") {
                $('#btnEditPeople', page).hide();
            } else {
                $('#btnEditPeople', page).show();
            }

            ApiClient.getItemImageInfos(currentItem.Id, currentItem.Type, currentItem.Name).done(function (imageInfos) {
                renderStandardImages(page, item, imageInfos);
                renderBackdrops(page, item, imageInfos);
                renderScreenshots(page, item, imageInfos);
                Dashboard.hideLoadingMsg();
            });
        });
    }

    function renderImages(page, item, images, elem) {

        var html = '';

        for (var i = 0, length = images.length; i < length; i++) {

            var image = images[i];

            html += '<div style="display:inline-block;margin:5px;background:#202020;padding:10px;">';

            html += '<div style="float:left;height:100px;width:175px;vertical-align:top;background-repeat:no-repeat;background-size:contain;background-image:url(\'' + LibraryBrowser.getImageUrl(currentItem, image.ImageType, image.ImageIndex, { maxwidth: 300 }) + '\');"></div>';

            html += '<div style="float:right;vertical-align:top;margin-left:1em;width:125px;">';
            html += '<p style="margin-top:0;">' + image.ImageType + '</p>';

            html += '<p>' + image.Width + ' * ' + image.Height + '</p>';

            html += '<p>' + (parseInt(image.Size / 1024)) + ' KB</p>';

            html += '<p style="margin-left:-5px;">';

            if (image.ImageType == "Backdrop" || image.ImageType == "Screenshot") {

                if (i > 0) {
                    html += '<button type="button" data-icon="arrow-left" data-mini="true" data-inline="true" data-iconpos="notext" onclick="EditItemImagesPage.moveImage(\'' + image.ImageType + '\', ' + image.ImageIndex + ', ' + (i - 1) + ');">Move left</button>';
                } else {
                    html += '<button type="button" data-icon="arrow-left" data-mini="true" data-inline="true" data-iconpos="notext" disabled>Move left</button>';
                }

                if (i < length - 1) {
                    html += '<button type="button" data-icon="arrow-right" data-mini="true" data-inline="true" data-iconpos="notext" onclick="EditItemImagesPage.moveImage(\'' + image.ImageType + '\', ' + image.ImageIndex + ', ' + (i + 1) + ');">Move right</button>';
                } else {
                    html += '<button type="button" data-icon="arrow-right" data-mini="true" data-inline="true" data-iconpos="notext" disabled>Move right</button>';
                }
            }

            html += '<button type="button" data-icon="delete" data-mini="true" data-inline="true" data-iconpos="notext" onclick="EditItemImagesPage.deleteImage(\'' + image.ImageType + '\', ' + (image.ImageIndex != null ? image.ImageIndex : "null") + ');">Delete</button>';

            html += '</p>';

            html += '</div>';

            html += '</div>';
        }

        elem.html(html).trigger('create');
    }

    function renderStandardImages(page, item, imageInfos) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType != "Screenshot" && i.ImageType != "Backdrop" && i.ImageType != "Chapter";
        });

        if (images.length) {
            $('#imagesContainer', page).show();
            renderImages(page, item, images, $('#images', page));
        } else {
            $('#imagesContainer', page).hide();
        }
    }

    function renderBackdrops(page, item, imageInfos) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType == "Backdrop";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            $('#backdropsContainer', page).show();
            renderImages(page, item, images, $('#backdrops', page));
        } else {
            $('#backdropsContainer', page).hide();
        }
    }

    function renderScreenshots(page, item, imageInfos) {

        var images = imageInfos.filter(function (i) {
            return i.ImageType == "Screenshot";

        }).sort(function (a, b) {
            return a.ImageIndex - b.ImageIndex;
        });

        if (images.length) {
            $('#screenshotsContainer', page).show();
            renderImages(page, item, images, $('#screenshots', page));
        } else {
            $('#screenshotsContainer', page).hide();
        }
    }

    function onFileReaderError(evt) {

        Dashboard.hideLoadingMsg();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                Dashboard.showError('File Not Found!');
                break;
            case evt.target.error.NOT_READABLE_ERR:
                Dashboard.showError('File is not readable');
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                Dashboard.showError('An error occurred reading this file.');
        };
    }

    function setFiles(page, files) {

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            $('#imageOutput', page).html('');
            $('#fldUpload', page).hide();
            currentFile = null;
            return;
        }

        currentFile = file;

        var reader = new FileReader();

        reader.onerror = onFileReaderError;
        reader.onloadstart = function () {
            $('#fldUpload', page).hide();
        };
        reader.onabort = function () {
            Dashboard.hideLoadingMsg();
            Dashboard.showError('File read cancelled');
        };

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {

                // Render thumbnail.
                var html = ['<img style="max-width:500px;max-height:200px;" src="', e.target.result, '" title="', escape(theFile.name), '"/>'].join('');

                $('#imageOutput', page).html(html);
                $('#fldUpload', page).show();
            };
        })(file);

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function processImageChangeResult(page) {

        reload(page);
    }

    function editItemImages() {

        var self = this;

        self.onSubmit = function () {

            var file = currentFile;

            if (!file) {
                return false;
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                return false;
            }

            Dashboard.showLoadingMsg();

            var page = $.mobile.activePage;

            var imageType = $('#selectImageType', page).val();

            ApiClient.uploadItemImage(currentItem.Id, currentItem.Type, currentItem.Name, imageType, file).done(function () {

                $('#uploadImage', page).val('').trigger('change');
                $('#popupUpload', page).popup("close");
                processImageChangeResult(page);

            });

            return false;
        };

        self.deleteImage = function (type, index) {

            var page = $.mobile.activePage;

            Dashboard.confirm("Are you sure you wish to delete this image?", "Delete " + type + " Image", function (result) {

                if (result) {
                    ApiClient.deleteItemImage(currentItem.Id, currentItem.Type, currentItem.Name, type, index).done(function () {

                        processImageChangeResult(page);

                    });
                }

            });


        };

        self.moveImage = function (type, index, newIndex) {

            var page = $.mobile.activePage;

            ApiClient.updateItemImageIndex(currentItem.Id, currentItem.Type, currentItem.Name, type, index, newIndex).done(function () {

                processImageChangeResult(page);

            });


        };
    }

    window.EditItemImagesPage = new editItemImages();

    $(document).on('pageinit', "#editItemImagesPage", function () {

        var page = this;


        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.id != currentItem.Id) {

                MetadataEditor.currentItemId = data.id;
                MetadataEditor.currentItemName = data.itemName;
                MetadataEditor.currentItemType = data.itemType;
                //Dashboard.navigate('edititemmetadata.html?id=' + data.id);

                $.mobile.urlHistory.ignoreNextHashChange = true;
                window.location.hash = 'editItemImagesPage?id=' + data.id;

                reload(page);
            }
        });

        $('#lnkBrowseImages', page).on('click', function () {

            reloadBrowsableImages(page);
        });

        $('#selectBrowsableImageType', page).on('change', function () {

            browsableImageType = this.value;
            browsableImageStartIndex = 0;

            reloadBrowsableImages(page);
        });

    }).on('pageshow', "#editItemImagesPage", function () {

        var page = this;

        reload(page);

        $('#uploadImage', page).on("change", function () {
            setFiles(page, this.files);
        });

        $("#imageDropZone", page).on('dragover', function (e) {

            e.preventDefault();

            e.originalEvent.dataTransfer.dropEffect = 'Copy';

            return false;

        }).on('drop', function (e) {

            e.preventDefault();

            setFiles(page, e.originalEvent.dataTransfer.files);

            return false;
        });

    }).on('pagehide', "#editItemImagesPage", function () {

        var page = this;

        currentItem = null;

        $('#uploadImage', page).off("change");

        $("#imageDropZone", page).off('dragover').off('drop');
    });

})(jQuery, document, window, window.FileReader, escape);