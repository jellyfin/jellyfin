(function ($, document, window, FileReader, escape) {

    var currentItem;
    var currentFile;

    function getPromise() {

        var name = getParameterByName('person');

        if (name) {
            return ApiClient.getPerson(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('studio');

        if (name) {

            return ApiClient.getStudio(name, Dashboard.getCurrentUserId());

        }

        name = getParameterByName('genre');

        if (name) {
            return ApiClient.getGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicgenre');

        if (name) {
            return ApiClient.getMusicGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('artist');

        if (name) {
            return ApiClient.getArtist(name, Dashboard.getCurrentUserId());
        }
        else {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), getParameterByName('id'));
        }
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        getPromise().done(function (item) {

            currentItem = item;

            LibraryBrowser.renderName(item, $('.itemName', page), true);
            LibraryBrowser.renderParentName(item, $('.parentName', page));

            ApiClient.getItemImageInfos(currentItem.Id).done(function (imageInfos) {
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

            ApiClient.uploadItemImage(currentItem.Id, imageType, file).done(function () {

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
                    ApiClient.deleteItemImage(currentItem.Id, type, index).done(function () {

                        processImageChangeResult(page);

                    });
                }

            });


        };

        self.moveImage = function (type, index, newIndex) {

            var page = $.mobile.activePage;

            ApiClient.updateItemImageIndex(currentItem.Id, type, index, newIndex).done(function () {

                processImageChangeResult(page);

            });


        };
    }

    window.EditItemImagesPage = new editItemImages();

    $(document).on('pageinit', "#editItemImagesPage", function () {

        var page = this;


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