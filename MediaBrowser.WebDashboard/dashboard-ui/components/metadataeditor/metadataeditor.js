(function ($, document, window, FileReader, escape) {

    var currentItem;

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
            ApiClient.getItem(Dashboard.getCurrentUserId(), currentItem.Id).done(function (item) {
                reloadItem(page, item);
            });
        }
    }

    function reloadItem(page, item) {

        currentItem = item;

    }

    function initEditor(page) {

    }

    function showEditor(itemId) {

        Dashboard.showLoadingMsg();

        HttpClient.send({

            type: 'GET',
            url: 'components/metadataeditor/metadataeditor.template.html'

        }).done(function (template) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {

                var dlg = document.createElement('paper-dialog');

                dlg.setAttribute('with-backdrop', 'with-backdrop');
                dlg.setAttribute('role', 'alertdialog');
                // without this safari will scroll the background instead of the dialog contents
                dlg.setAttribute('modal', 'modal');
                // seeing max call stack size exceeded in the debugger with this
                dlg.setAttribute('noAutoFocus', 'noAutoFocus');
                dlg.entryAnimation = 'scale-up-animation';
                dlg.exitAnimation = 'fade-out-animation';
                dlg.classList.add('smoothScrollY');

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog"></paper-fab>';
                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + Globalize.translate('ButtonEdit') + '</div>';
                html += '</h2>';

                html += '<div class="editorContent">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                initEditor(dlg);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('iron-overlay-closed', onDialogClosed);

                PaperDialogHelper.openWithHash(dlg, 'metadataeditor');

                var editorContent = dlg.querySelector('.editorContent');
                reload(editorContent, item);

                $('.btnCloseDialog', dlg).on('click', function () {

                    PaperDialogHelper.close(dlg);
                });
            });
        });
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
    }

    window.MetadataEditor = {
        show: function (itemId) {

            require(['components/paperdialoghelper'], function () {

                Dashboard.importCss('css/metadataeditor.css');
                showEditor(itemId);
            });
        }
    };

})(jQuery, document, window, window.FileReader, escape);