(function (globalScope) {

    function paperDialogHashHandler(dlg, hash) {

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

            Dashboard.onPopupClose();

            dlg = null;
            $(window).off('navigate', onHashChange);

            if (window.location.hash == '#' + hash) {
                history.back();
            }
        }

        var self = this;

        $(dlg).on('iron-overlay-closed', onDialogClosed);
        dlg.open();
        Dashboard.onPopupOpen();

        window.location.hash = hash;

        $(window).on('navigate', onHashChange);
    }

    function openWithHash(dlg, hash) {

        new paperDialogHashHandler(dlg, hash);
    }

    globalScope.PaperDialogHelper = {
        openWithHash: openWithHash
    };

})(this);