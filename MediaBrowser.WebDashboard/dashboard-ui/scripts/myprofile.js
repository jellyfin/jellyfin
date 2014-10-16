(function ($, window, document, FileReader) {

    var currentFile;

    function reloadUser(page) {

        var userId = getParameterByName("userId");

        Dashboard.showLoadingMsg();

        ApiClient.getUser(userId).done(function (user) {

            $('.username', page).html(user.Name);
            $('#uploadUserImage', page).val('').trigger('change');

            Dashboard.setPageTitle(user.Name);

            if (user.PrimaryImageTag) {

                var imageUrl = ApiClient.getUserImageUrl(user.Id, {
                    height: 200,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                $('#fldImage', page).show().html('').html("<img width='140px' src='" + imageUrl + "' />");
            }

            if (user.ConnectLinkType == 'Guest') {

                $('.newImageForm', page).hide();
                $('#btnDeleteImage', page).hide();
            }
            else if (user.PrimaryImageTag) {

                $('#btnDeleteImage', page).show();
                $('#headerUploadNewImage', page).show();
                $('.newImageForm', page).show();

            } else {
                $('.newImageSection', page).show();
                $('#fldImage', page).hide().html('');
                $('#btnDeleteImage', page).hide();
                $('#headerUploadNewImage', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });

    }

    function processImageChangeResult() {

        Dashboard.hideLoadingMsg();

        var page = $.mobile.activePage;

        reloadUser(page);
    }

    function onFileReaderError(evt) {

        Dashboard.hideLoadingMsg();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                Dashboard.showError(Globalize.translate('FileNotFound'));
                break;
            case evt.target.error.NOT_READABLE_ERR:
                Dashboard.showError(Globalize.translate('FileReadError'));
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                Dashboard.showError(Globalize.translate('FileReadError'));
        };
    }

    function onFileReaderOnloadStart(evt) {

        $('#fldUpload', $.mobile.activePage).hide();
    }

    function onFileReaderAbort(evt) {

        Dashboard.hideLoadingMsg();
        Dashboard.showError(Globalize.translate('FileReadCancelled'));
    }

    function setFiles(page, files) {

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            $('#userImageOutput', page).html('');
            $('#fldUpload', page).hide();
            currentFile = null;
            return;
        }

        currentFile = file;

        var reader = new FileReader();

        reader.onerror = onFileReaderError;
        reader.onloadstart = onFileReaderOnloadStart;
        reader.onabort = onFileReaderAbort;

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {

                // Render thumbnail.
                var html = ['<img style="max-width:500px;max-height:200px;" src="', e.target.result, '" title="', escape(theFile.name), '"/>'].join('');

                $('#userImageOutput', page).html(html);
                $('#fldUpload', page).show();
            };
        })(file);

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function onImageDrop(e) {

        e.preventDefault();

        setFiles($.mobile.activePage, e.originalEvent.dataTransfer.files);

        return false;
    }

    function onImageDragOver(e) {

        e.preventDefault();

        e.originalEvent.dataTransfer.dropEffect = 'Copy';

        return false;
    }

    function myProfilePage() {

        var self = this;

        self.onImageSubmit = function () {

            var file = currentFile;

            if (!file) {
                return false;
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                return false;
            }

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.uploadUserImage(userId, 'Primary', file).done(processImageChangeResult);

            return false;
        };

        self.onFileUploadChange = function (fileUpload) {

            setFiles($.mobile.activePage, fileUpload.files);
        };
    }

    window.MyProfilePage = new myProfilePage();

    $(document).on('pagebeforeshow', "#userImagePage", function () {

        var page = this;

        Dashboard.getCurrentUser().done(function (loggedInUser) {

            if (loggedInUser.Configuration.IsAdministrator) {
                $('#lnkParentalControl', page).show();
            } else {
                $('#lnkParentalControl', page).hide();
            }
        });

    }).on('pageinit', "#userImagePage", function () {

        var page = this;

        reloadUser(page);

        $("#userImageDropZone", page).on('dragover', onImageDragOver).on('drop', onImageDrop);

        $('#btnDeleteImage', page).on('click', function () {

            Dashboard.confirm(Globalize.translate('DeleteImageConfirmation'), Globalize.translate('DeleteImage'), function (result) {

                if (result) {

                    Dashboard.showLoadingMsg();

                    var userId = getParameterByName("userId");

                    ApiClient.deleteUserImage(userId, "primary").done(processImageChangeResult);
                }

            });
        });
    });


})(jQuery, window, document, window.FileReader);