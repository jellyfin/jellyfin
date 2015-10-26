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
        if ($.browser.edge) {
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

    function createDialog(options) {

        options = options || {};

        var dlg = document.createElement('paper-dialog');

        dlg.setAttribute('with-backdrop', 'with-backdrop');
        dlg.setAttribute('role', 'alertdialog');

        // without this safari will scroll the background instead of the dialog contents
        // but not needed here since this is already on top of an existing dialog
        // but skip it in IE because it's causing the entire browser to hang
        // Also have to disable for firefox because it's causing select elements to not be clickable
        if (!$.browser.msie && !$.browser.mozilla) {
            if (options.modal !== false) {
                dlg.setAttribute('modal', 'modal');
            }
        }

        //// seeing max call stack size exceeded in the debugger with this
        dlg.setAttribute('noAutoFocus', 'noAutoFocus');
        dlg.entryAnimation = 'scale-up-animation';
        dlg.exitAnimation = 'fade-out-animation';

        dlg.classList.add('popupEditor');

        if (options.size == 'small') {
            dlg.classList.add('small-paper-dialog');
        }
        else if (options.size == 'medium') {
            dlg.classList.add('medium-paper-dialog');
        } else {
            dlg.classList.add('fullscreen-paper-dialog');
        }

        var theme = options.theme || 'b';

        dlg.classList.add('ui-body-' + theme);
        dlg.classList.add('background-theme-' + theme);
        dlg.classList.add('smoothScrollY');

        return dlg;
    }

    function positionTo(dlg, elem) {

        var windowHeight = $(window).height();

        // If the window height is under a certain amount, don't bother trying to position
        // based on an element.
        if (windowHeight >= 540) {

            var pos = $(elem).offset();

            pos.top += elem.offsetHeight / 2;
            pos.left += elem.offsetWidth / 2;

            // Account for margins
            pos.top -= 24;
            pos.left -= 24;

            // Account for popup size - we can't predict this yet so just estimate
            pos.top -= $(dlg).height() / 2;
            pos.left -= $(dlg).width() / 2;

            // Account for scroll position
            pos.top -= $(window).scrollTop();
            pos.left -= $(window).scrollLeft();

            // Avoid showing too close to the bottom
            pos.top = Math.min(pos.top, windowHeight - 300);
            pos.left = Math.min(pos.left, $(window).width() - 300);

            // Do some boundary checking
            pos.top = Math.max(pos.top, 0);
            pos.left = Math.max(pos.left, 0);

            dlg.style.position = 'fixed';
            dlg.style.left = pos.left + 'px';
            dlg.style.top = pos.top + 'px';
        }
    }

    globalScope.PaperDialogHelper = {
        openWithHash: openWithHash,
        close: close,
        createDialog: createDialog,
        positionTo: positionTo
    };

})(this);