(function (globalScope) {

    function paperDialogHashHandler(dlg, hash, lockDocumentScroll) {

        function onHashChange(e, data) {

            data = data.state;
            var isActive = data.hash == '#' + hash;

            if (data.direction == 'back') {
                if (dlg) {
                    if (!isActive) {
                        dlg.close();
                        dlg = null;
                    }
                }
            }
        }

        function onDialogClosed() {

            if (lockDocumentScroll !== false) {
                Dashboard.onPopupClose();
            }

            dlg = null;
            if (enableHashChange()) {
                $(window).off('navigate', onHashChange);

                if (window.location.hash == '#' + hash) {
                    history.back();
                }
            }
        }

        var self = this;

        $(dlg).on('iron-overlay-closed', onDialogClosed);
        dlg.open();

        if (lockDocumentScroll !== false) {
            Dashboard.onPopupOpen();
        }

        if (enableHashChange()) {

            window.location.hash = hash;

            $(window).on('navigate', onHashChange);
        }
    }

    function enableHashChange() {
        // It's not firing popstate in response to hashbang changes
        if ($.browser.msie) {
            return false;
        }
        return true;
    }

    function openWithHash(dlg, hash, lockDocumentScroll) {

        new paperDialogHashHandler(dlg, hash, lockDocumentScroll);
    }

    function close(dlg) {

        if (enableHashChange()) {

            if (dlg.opened) {
                history.back();
            }

        } else {
            dlg.close();
        }
    }

    function createDialog() {
        var dlg = document.createElement('paper-dialog');

        dlg.setAttribute('with-backdrop', 'with-backdrop');
        dlg.setAttribute('role', 'alertdialog');

        // without this safari will scroll the background instead of the dialog contents
        // but not needed here since this is already on top of an existing dialog
        dlg.setAttribute('modal', 'modal');

        // seeing max call stack size exceeded in the debugger with this
        dlg.setAttribute('noAutoFocus', 'noAutoFocus');
        dlg.entryAnimation = 'scale-up-animation';
        dlg.exitAnimation = 'fade-out-animation';
        dlg.classList.add('fullscreen-editor-paper-dialog');
        dlg.classList.add('ui-body-b');
        dlg.classList.add('background-theme-b');
        dlg.classList.add('smoothScrollY');

        return dlg;
    }

    globalScope.PaperDialogHelper = {
        openWithHash: openWithHash,
        close: close,
        createDialog: createDialog
    };

})(this);