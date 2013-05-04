(function ($, document, window, FileReader, escape) {

    var currentItem;
    var currentFile;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            LibraryBrowser.renderTitle(item, $('#itemName', page), $('#parentName', page), $('#grandParentName', page));

            Dashboard.hideLoadingMsg();
        });
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

        Dashboard.hideLoadingMsg();

        Dashboard.validateCurrentUser(page);
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

            ApiClient.uploadImage(currentItem.Id, imageType, file).done(function () {

                processImageChangeResult(page);

            });

            return false;
        };
    }

    window.EditItemImagesPage = new editItemImages();

    $(document).on('pageinit', "#editItemImagesPage", function () {

        var page = this;


    }).on('pageshow', "#editItemImagesPage", function () {

        var page = this;

        reload(page);

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

        $("#imageDropZone", page).off('dragover').off('drop');
    });

})(jQuery, document, window, FileReader, escape);