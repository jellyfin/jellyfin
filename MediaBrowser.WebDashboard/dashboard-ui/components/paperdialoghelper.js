(function (globalScope) {

    function paperDialogHashHandler(dlg, hash, lockDocumentScroll) {

        var isActive = true;

        function onHashChange(e, data) {

            data = data.state;
            isActive = data.hash == '#' + hash;

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
            history.back();
        } else {
            dlg.close();
        }
    }

    globalScope.PaperDialogHelper = {
        openWithHash: openWithHash,
        close: close
    };

})(this);