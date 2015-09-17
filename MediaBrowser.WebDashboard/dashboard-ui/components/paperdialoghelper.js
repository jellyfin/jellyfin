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

            if (isActive) {
                document.body.classList.add('bodyWithPopupOpen');
            }
            else {
                document.body.classList.remove('bodyWithPopupOpen');
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