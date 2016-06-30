define(['scripts/userpasswordpage'], function (Userpasswordpage) {

    var currentFile;

    function reloadUser(page) {

        var userId = getParameterByName("userId");

        Dashboard.showLoadingMsg();

        ApiClient.getUser(userId).then(function (user) {

            page.querySelector('.username').innerHTML = user.Name;
            var uploadUserImage = page.querySelector('#uploadUserImage');
            uploadUserImage.value = '';
            uploadUserImage.dispatchEvent(new CustomEvent('change', {}));

            Dashboard.setPageTitle(user.Name);

            var imageUrl;

            if (user.PrimaryImageTag) {

                imageUrl = ApiClient.getUserImageUrl(user.Id, {
                    height: 200,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

            } else {
                imageUrl = "css/images/logindefault.png";
            }

            var fldImage = page.querySelector('#fldImage');
            fldImage.classList.remove('hide');
            fldImage.innerHTML = "<img width='140px' src='" + imageUrl + "' />";

            var showImageEditing = false;

            if (user.ConnectLinkType == 'Guest') {

                page.querySelector('.connectMessage').classList.remove('hide');
            }
            else if (user.PrimaryImageTag) {

                showImageEditing = true;
                page.querySelector('.connectMessage').classList.add('hide');

            } else {
                showImageEditing = true;
                page.querySelector('.connectMessage').classList.add('hide');
            }

            Dashboard.getCurrentUser().then(function (loggedInUser) {

                if (showImageEditing && AppInfo.supportsFileInput && (loggedInUser.Policy.IsAdministrator || user.Policy.EnableUserPreferenceAccess)) {
                    page.querySelector('.newImageForm').classList.remove('hide');
                    page.querySelector('#btnDeleteImage').classList.remove('hide');
                } else {
                    page.querySelector('.newImageForm').classList.add('hide');
                    page.querySelector('#btnDeleteImage').classList.add('hide');
                }
            });

            Dashboard.hideLoadingMsg();
        });

    }

    function onFileReaderError(evt) {

        Dashboard.hideLoadingMsg();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                require(['toast'], function (toast) {
                    toast(Globalize.translate('FileNotFound'));
                });
                break;
            case evt.target.error.NOT_READABLE_ERR:
                require(['toast'], function (toast) {
                    toast(Globalize.translate('FileReadError'));
                });
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('FileReadError'));
                    });
                    break;
                }
        };
    }

    function onFileReaderAbort(evt) {

        Dashboard.hideLoadingMsg();
        require(['toast'], function (toast) {
            toast(Globalize.translate('FileReadCancelled'));
        });
    }

    function setFiles(page, files) {

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            page.querySelector('#userImageOutput').innerHTML = '';
            page.querySelector('#fldUpload').classList.add('hide');
            currentFile = null;
            return;
        }

        currentFile = file;

        var reader = new FileReader();

        reader.onerror = onFileReaderError;
        reader.onloadstart = function () {
            page.querySelector('#fldUpload').classList.add('hide');
        };
        reader.onabort = onFileReaderAbort;

        // Closure to capture the file information.
        reader.onload = function (e) {

            // Render thumbnail.
            var html = ['<img style="max-width:500px;max-height:200px;" src="', e.target.result, '" title="', escape(file.name), '"/>'].join('');

            page.querySelector('#userImageOutput').innerHTML = html;
            page.querySelector('#fldUpload').classList.remove('hide');
        };

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function onImageDragOver(e) {

        e.preventDefault();

        e.originalEvent.dataTransfer.dropEffect = 'Copy';

        return false;
    }

    return function (view, params) {

        reloadUser(view);

        var userpasswordpage = new Userpasswordpage(view, params);

        var userImageDropZone = view.querySelector('#userImageDropZone');
        userImageDropZone.addEventListener('dragOver', onImageDragOver);
        userImageDropZone.addEventListener('drop', function (e) {

            e.preventDefault();

            setFiles(view, e.originalEvent.dataTransfer.files);

            return false;
        });

        view.querySelector('#btnDeleteImage').addEventListener('click', function () {

            require(['confirm'], function (confirm) {

                confirm(Globalize.translate('DeleteImageConfirmation'), Globalize.translate('DeleteImage')).then(function () {

                    Dashboard.showLoadingMsg();

                    var userId = getParameterByName("userId");

                    ApiClient.deleteUserImage(userId, "primary").then(function () {

                        Dashboard.hideLoadingMsg();

                        reloadUser(view);
                    });
                });
            });
        });

        view.querySelector('.newImageForm').addEventListener('submit', function (e) {

            var file = currentFile;

            if (!file) {
                return false;
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                return false;
            }

            Dashboard.showLoadingMsg();

            var userId = getParameterByName("userId");

            ApiClient.uploadUserImage(userId, 'Primary', file).then(function () {

                Dashboard.hideLoadingMsg();

                reloadUser(view);
            });

            e.preventDefault();
            return false;
        });

        view.querySelector('#uploadUserImage').addEventListener('change', function (e) {
            setFiles(view, e.target.files);
        });
    };
});