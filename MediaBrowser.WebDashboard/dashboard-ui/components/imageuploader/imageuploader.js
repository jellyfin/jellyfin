define(['dialogHelper', 'jQuery', 'emby-button', 'emby-select'], function (dialogHelper, $) {

    var currentItemId;
    var currentFile;
    var currentDeferred;
    var hasChanges = false;

    function onFileReaderError(evt) {

        Dashboard.hideLoadingMsg();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageFileNotFound'));
                });
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageFileReadError'));
                });
                break;
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
            console.log('File read cancelled');
        };

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {

                // Render thumbnail.
                var html = ['<img style="max-width:300px;max-height:100px;" src="', e.target.result, '" title="', escape(theFile.name), '"/>'].join('');

                $('#imageOutput', page).html(html);
                $('#fldUpload', page).show();
            };
        })(file);

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function processImageChangeResult(page) {

        hasChanges = true;
        history.back();
    }

    function onSubmit() {

        var file = currentFile;

        if (!file) {
            return false;
        }

        if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
            return false;
        }

        Dashboard.showLoadingMsg();

        var page = $(this).parents('.dialog');

        var imageType = $('#selectImageType', page).val();

        ApiClient.uploadItemImage(currentItemId, imageType, file).then(function () {

            $('#uploadImage', page).val('').trigger('change');
            Dashboard.hideLoadingMsg();
            processImageChangeResult(page);
        });

        return false;
    }

    function initEditor(page) {

        $('form', page).off('submit', onSubmit).on('submit', onSubmit);

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
    }

    function showEditor(itemId, options) {

        options = options || {};

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/imageuploader/imageuploader.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;
            currentItemId = itemId;

            var dlg = dialogHelper.createDialog({
                size: 'fullscreen-border'
            });

            var theme = options.theme || 'b';

            dlg.classList.add('ui-body-' + theme);
            dlg.classList.add('background-theme-' + theme);

            var html = '';
            html += '<h2 class="dialogHeader">';
            html += '<button type="button" is="emby-button" icon="arrow-back" class="fab mini btnCloseDialog autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + Globalize.translate('HeaderUploadImage') + '</div>';
            html += '</h2>';

            html += '<div class="editorContent" style="padding:0 1em;">';
            html += Globalize.translateDocument(template);
            html += '</div>';

            dlg.innerHTML = html;

            // Has to be assigned a z-index after the call to .open() 
            $(dlg).on('close', onDialogClosed);

            dialogHelper.open(dlg);

            var editorContent = dlg.querySelector('.editorContent');
            initEditor(editorContent);

            $('#selectImageType', dlg).val(options.imageType || 'Primary');

            $('.btnCloseDialog', dlg).on('click', function () {

                dialogHelper.close(dlg);
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