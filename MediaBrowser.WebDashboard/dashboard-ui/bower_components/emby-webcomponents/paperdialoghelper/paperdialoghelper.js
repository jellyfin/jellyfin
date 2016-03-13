define(['historyManager', 'focusManager', 'browser', 'layoutManager', 'inputManager', 'paper-dialog', 'scale-up-animation', 'fade-out-animation', 'fade-in-animation', 'css!./paperdialoghelper.css'], function (historyManager, focusManager, browser, layoutManager, inputManager) {

    function paperDialogHashHandler(dlg, hash, resolve) {

        var self = this;
        self.originalUrl = window.location.href;
        var activeElement = document.activeElement;
        var removeScrollLockOnClose = false;

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

        function onBackCommand(e) {

            if (e.detail.command == 'back') {
                inputManager.off(dlg, onBackCommand);

                self.closedByBack = true;
                dlg.close();
                e.preventDefault();
            }
        }

        function onDialogClosed() {

            if (removeScrollLockOnClose) {
                document.body.classList.remove('noScroll');
            }

            window.removeEventListener('popstate', onHashChange);
            inputManager.off(dlg, onBackCommand);

            if (!self.closedByBack && isHistoryEnabled(dlg)) {
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

        if (dlg.getAttribute('data-lockscroll') == 'true' && !document.body.classList.contains('noScroll')) {
            document.body.classList.add('noScroll');
            removeScrollLockOnClose = true;
        }

        if (isHistoryEnabled(dlg)) {
            historyManager.pushState({ dialogId: hash }, "Dialog", hash);

            window.addEventListener('popstate', onHashChange);
        } else {
            inputManager.on(dlg, onBackCommand);
        }
    }

    function isHistoryEnabled(dlg) {
        return dlg.getAttribute('data-history') == 'true';
    }

    function open(dlg) {

        return new Promise(function (resolve, reject) {

            new paperDialogHashHandler(dlg, 'dlg' + new Date().getTime(), resolve);
        });
    }

    function close(dlg) {

        if (dlg.opened) {
            if (isHistoryEnabled(dlg)) {
                history.back();
            } else {
                dlg.close();
            }
        }
    }

    function onDialogOpened(e) {

        focusManager.autoFocus(e.target);
    }

    function shouldLockDocumentScroll(options) {

        if (options.lockScroll != null) {
            return options.lockScroll;
        }

        if (options.size == 'fullscreen') {
            return true;
        }

        return browser.mobile;
    }

    function createDialog(options) {

        options = options || {};

        var dlg = document.createElement('paper-dialog');

        dlg.setAttribute('with-backdrop', 'with-backdrop');
        dlg.setAttribute('role', 'alertdialog');

        if (shouldLockDocumentScroll(options)) {
            dlg.setAttribute('data-lockscroll', 'true');
        }

        if (options.enableHistory !== false && historyManager.enableNativeHistory()) {
            dlg.setAttribute('data-history', 'true');
        }

        // without this safari will scroll the background instead of the dialog contents
        // but not needed here since this is already on top of an existing dialog
        // but skip it in IE because it's causing the entire browser to hang
        // Also have to disable for firefox because it's causing select elements to not be clickable
        if (options.modal !== false) {
            dlg.setAttribute('modal', 'modal');
        }

        // seeing max call stack size exceeded in the debugger with this
        dlg.setAttribute('noAutoFocus', 'noAutoFocus');

        var defaultEntryAnimation = browser.animate && !browser.mobile ? 'scale-up-animation' : 'fade-in-animation';
        dlg.entryAnimation = options.entryAnimation || defaultEntryAnimation;
        dlg.exitAnimation = 'fade-out-animation';

        // If it's not fullscreen then lower the default animation speed to make it open really fast
        var entryAnimationDuration = options.entryAnimationDuration || (options.size ? 240 : 300);

        dlg.animationConfig = {
            // scale up
            'entry': {
                name: dlg.entryAnimation,
                node: dlg,
                timing: { duration: entryAnimationDuration, easing: 'ease-out' }
            },
            // fade out
            'exit': {
                name: dlg.exitAnimation,
                node: dlg,
                timing: { duration: options.exitAnimationDuration || 400, easing: 'ease-in' }
            }
        };

        // too buggy in IE, not even worth it
        if (!browser.animate) {
            dlg.animationConfig = null;
            dlg.entryAnimation = null;
            dlg.exitAnimation = null;
        }

        dlg.classList.add('paperDialog');

        dlg.classList.add('scrollY');

        if (layoutManager.tv || layoutManager.mobile) {
            // Need scrollbars for mouse use
            dlg.classList.add('hiddenScroll');
        }

        if (options.removeOnClose) {
            dlg.setAttribute('data-removeonclose', 'true');
        }

        if (options.size) {
            dlg.classList.add('fixedSize');
            dlg.classList.add(options.size);
        }

        if (options.autoFocus !== false) {
            dlg.addEventListener('iron-overlay-opened', onDialogOpened);
        }

        return dlg;
    }

    return {
        open: open,
        close: close,
        createDialog: createDialog
    };
});