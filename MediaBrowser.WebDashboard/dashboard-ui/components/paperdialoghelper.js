define(['paper-dialog', 'scale-up-animation', 'fade-out-animation', 'fade-in-animation'], function () {

    function paperDialogHashHandler(dlg, hash, resolve, lockDocumentScroll) {

        var self = this;
        self.originalUrl = window.location.href;
        var activeElement = document.activeElement;

        function onHashChange(e) {

            var isBack = self.originalUrl == window.location.href;

            if (isBack || !dlg.opened) {
                window.removeEventListener('popstate', onHashChange);
            }

            if (isBack) {
                self.closedByBack = true;
                dlg.close();
            }
        }

        function onDialogClosed() {

            if (lockDocumentScroll !== false) {
                Dashboard.onPopupClose();
            }

            window.removeEventListener('popstate', onHashChange);

            if (!self.closedByBack) {
                var state = history.state || {};
                if (state.dialogId == hash) {
                    history.back();
                }
            }

            activeElement.focus();

            if (dlg.getAttribute('data-removeonclose') == 'true') {
                dlg.parentNode.removeChild(dlg);
            }

            //resolve();
            // if we just called history.back(), then use a timeout to allow the history events to fire first
            setTimeout(function () {
                resolve({
                    element: dlg,
                    closedByBack: self.closedByBack
                });
            }, 1);
        }

        dlg.addEventListener('iron-overlay-closed', onDialogClosed);
        dlg.open();

        if (lockDocumentScroll !== false) {
            Dashboard.onPopupOpen();
        }

        var state = {
            dialogId: hash,
            navigate: false
        };
        history.pushState(state, "Dialog", hash);

        jQuery.onStatePushed(state);

        window.addEventListener('popstate', onHashChange);
    }

    function open(dlg) {

        return new Promise(function (resolve, reject) {

            new paperDialogHashHandler(dlg, 'dlg' + new Date().getTime(), resolve);
        });
    }

    function close(dlg) {

        if (dlg.opened) {
            history.back();
        }
    }

    function onDialogOpened(e) {

        //Emby.FocusManager.autoFocus(e.target, true);
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
        if (!browserInfo.msie && !browserInfo.mozilla) {
            if (options.modal !== false) {
                dlg.setAttribute('modal', 'modal');
            }
        }

        // seeing max call stack size exceeded in the debugger with this
        dlg.setAttribute('noAutoFocus', 'noAutoFocus');

        // These don't seem to perform well on mobile
        var defaultEntryAnimation = browserInfo.mobile ? 'fade-in-animation' : 'scale-up-animation';
        dlg.entryAnimation = options.entryAnimation || defaultEntryAnimation;
        dlg.exitAnimation = 'fade-out-animation';

        dlg.animationConfig = {
            // scale up
            'entry': {
                name: options.entryAnimation || defaultEntryAnimation,
                node: dlg,
                timing: { duration: options.entryAnimationDuration || 300, easing: 'ease-out' }
            },
            // fade out
            'exit': {
                name: 'fade-out-animation',
                node: dlg,
                timing: { duration: options.exitAnimationDuration || 400, easing: 'ease-in' }
            }
        };

        if (options.size != 'auto') {
            dlg.classList.add('popupEditor');

            if (options.size == 'small') {
                dlg.classList.add('small-paper-dialog');
            }
            else if (options.size == 'medium') {
                dlg.classList.add('medium-paper-dialog');
            } else {
                dlg.classList.add('fullscreen-paper-dialog');
            }
        }

        var theme = options.theme || 'b';

        dlg.classList.add('ui-body-' + theme);
        dlg.classList.add('background-theme-' + theme);
        dlg.classList.add('smoothScrollY');

        if (options.removeOnClose) {
            dlg.setAttribute('data-removeonclose', 'true');
        }

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

    window.PaperDialogHelper = {
        open: open,
        close: close,
        createDialog: createDialog,
        positionTo: positionTo
    };

    return PaperDialogHelper;
});