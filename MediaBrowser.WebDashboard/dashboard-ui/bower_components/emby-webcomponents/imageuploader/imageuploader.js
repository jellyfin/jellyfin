define(['dialogHelper', 'connectionManager', 'dom', 'loading', 'scrollHelper', 'layoutManager', 'globalize', 'require', 'emby-button', 'emby-select', 'formDialogStyle', 'css!./style'], function (dialogHelper, connectionManager, dom, loading, scrollHelper, layoutManager, globalize, require) {
    'use strict';

    var currentItemId;
    var currentServerId;
    var currentFile;
    var hasChanges = false;

    function onFileReaderError(evt) {

        loading.hide();

        switch (evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                require(['toast'], function (toast) {
                    toast(globalize.translate('sharedcomponents#MessageFileReadError'));
                });
                break;
            case evt.target.error.ABORT_ERR:
                break; // noop
            default:
                require(['toast'], function (toast) {
                    toast(globalize.translate('sharedcomponents#MessageFileReadError'));
                });
                break;
        }
    }

    function setFiles(page, files) {

        var file = files[0];

        if (!file || !file.type.match('image.*')) {
            page.querySelector('#imageOutput').innerHTML = '';
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
        reader.onabort = function () {
            loading.hide();
            console.log('File read cancelled');
        };

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {

                // Render thumbnail.
                var html = ['<img style="max-width:100%;max-height:100%;" src="', e.target.result, '" title="', escape(theFile.name), '"/>'].join('');

                page.querySelector('#imageOutput').innerHTML = html;
                page.querySelector('#fldUpload').classList.remove('hide');
            };
        })(file);

        // Read in the image file as a data URL.
        reader.readAsDataURL(file);
    }

    function onSubmit(e) {

        var file = currentFile;

        if (!file) {
            return false;
        }

        if (file.type !== "image/png" && file.type !== "image/jpeg" && file.type !== "image/jpeg") {
            return false;
        }

        loading.show();

        var dlg = dom.parentWithClass(this, 'dialog');

        var imageType = dlg.querySelector('#selectImageType').value;

        connectionManager.getApiClient(currentServerId).uploadItemImage(currentItemId, imageType, file).then(function () {

            dlg.querySelector('#uploadImage').value = '';

            loading.hide();
            hasChanges = true;
            dialogHelper.close(dlg);
        });

        e.preventDefault();
        return false;
    }

    function initEditor(page) {

        page.querySelector('form').addEventListener('submit', onSubmit);

        page.querySelector('#uploadImage').addEventListener("change", function () {
            setFiles(page, this.files);
        });

        page.querySelector('.btnBrowse').addEventListener("click", function () {
            page.querySelector('#uploadImage').click();
        });
    }

    function showEditor(options, resolve, reject) {

        options = options || {};

        require(['text!./imageuploader.template.html'], function (template) {

            currentItemId = options.itemId;
            currentServerId = options.serverId;

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

            // Has to be assigned a z-index after the call to .open() 
            dlg.addEventListener('close', function () {

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.off(dlg, false);
                }

                loading.hide();
                resolve(hasChanges);
            });

            dialogHelper.open(dlg);

            initEditor(dlg);

            dlg.querySelector('#selectImageType').value = options.imageType || 'Primary';

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
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