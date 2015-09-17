(function (globalScope) {

    function paperDialogHashHandler(dlg, hash) {

        function onHashChange(e, data) {

            data = data.state;

            if (data.direction == 'back') {
                if (dlg) {
                    if (data.hash != '#' + hash) {
                        dlg.close();
                        dlg = null;
                    }
                }
            }
        }

        function onDialogClosed() {

            dlg = null;
            $(window).off('navigate', onHashChange);

            if (window.location.hash == '#' + hash) {
                history.back();
            }
        }

        var self = this;

        $(dlg).on('iron-overlay-closed', onDialogClosed);
        dlg.open();

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