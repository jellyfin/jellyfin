var UserImagePage = {

    onPageShow: function () {

        UserImagePage.reloadUser();

        $("#userImageDropZone", this).on('dragover', UserImagePage.onImageDragOver).on('drop', UserImagePage.onImageDrop);
    },

    onPageHide: function () {
        $("#userImageDropZone", this).off('dragover', UserImagePage.onImageDragOver).off('drop', UserImagePage.onImageDrop);
    },

    reloadUser: function () {

        var userId = getParameterByName("userId");

        Dashboard.showLoadingMsg();

        ApiClient.getUser(userId).done(function (user) {

            var page = $($.mobile.activePage);

            $('#uploadUserImage', page).val('').trigger('change');

            Dashboard.setPageTitle(user.Name);

            if (user.PrimaryImageTag) {

                var imageUrl = ApiClient.getUserImageUrl(user.Id, {
                    height: 450,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                $('#fldImage', page).show().html('').html("<img height='200px' src='" + imageUrl + "' />");

                $('#fldDeleteImage', page).show();
                $('#headerUploadNewImage', page).show();
            } else {
                $('#fldImage', page).hide().html('');
                $('#fldDeleteImage', page).hide();
                $('#headerUploadNewImage', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });
    },

    deleteImage: function () {

        Dashboard.confirm("Are you sure you wish to delete the image?", "Delete Image", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                var userId = getParameterByName("userId");

                ApiClient.deleteUserImage(userId, "primary").done(UserImagePage.processImageChangeResult);
            }

        });
    },

    processImageChangeResult: function () {
        
        Dashboard.hideLoadingMsg();

        Dashboard.validateCurrentUser($.mobile.activePage);
        UserImagePage.reloadUser();
    },

    onFileUploadChange: function (fileUpload) {

        UserImagePage.setFiles(fileUpload.files);
    },

    setFiles: function (files) {

        var page = $.mobile.activePage;

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            $('#userImageOutput', page).html('');
            $('#fldUpload', page).hide();
            UserImagePage.currentFile = null;
            return;
        }

        UserImagePage.currentFile = file;

        var reader = new FileReader();

        reader.onerror = UserImagePage.onFileReaderError;
        reader.onloadstart = UserImagePage.onFileReaderOnloadStart;
        reader.onabort = UserImagePage.onFileReaderAbort;

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
    },

    onFileReaderError: function (evt) {

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
    },

    onFileReaderOnloadStart: function (evt) {

        $('#fldUpload', $.mobile.activePage).hide();
    },

    onFileReaderAbort: function (evt) {

        Dashboard.hideLoadingMsg();
        Dashboard.showError('File read cancelled');
    },

    onSubmit: function () {

        var file = UserImagePage.currentFile;

        if (!file) {
            return false;
        }

        if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
            return false;
        }

        Dashboard.showLoadingMsg();

        var userId = getParameterByName("userId");

        ApiClient.uploadUserImage(userId, 'Primary', file).done(UserImagePage.processImageChangeResult);

        return false;
    },

    onImageDrop: function (e) {

        e.preventDefault();

        UserImagePage.setFiles(e.originalEvent.dataTransfer.files);

        return false;
    },

    onImageDragOver: function (e) {

        e.preventDefault();

        e.originalEvent.dataTransfer.dropEffect = 'Copy';

        return false;
    }
};

$(document).on('pageshow', "#userImagePage", UserImagePage.onPageShow).on('pagehide', "#userImagePage", UserImagePage.onPageHide);
