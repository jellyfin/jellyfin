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
            $(window).off('navigate', onHashChange);

            if (window.location.hash == '#' + hash) {
                history.back();
            }
        }

        var self = this;

        $(dlg).on('iron-overlay-closed', onDialogClosed);
        dlg.open();

        if (lockDocumentScroll !== false) {
            Dashboard.onPopupOpen();
        }

        window.location.hash = hash;

        $(window).on('navigate', onHashChange);
    }

    function openWithHash(dlg, hash, lockDocumentScroll) {

        new paperDialogHashHandler(dlg, hash, lockDocumentScroll);
    }

    globalScope.PaperDialogHelper = {
        openWithHash: openWithHash
    };

})(this);