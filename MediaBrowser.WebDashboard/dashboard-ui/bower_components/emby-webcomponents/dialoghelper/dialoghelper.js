define(['historyManager', 'focusManager', 'browser', 'layoutManager', 'inputManager', 'css!./dialoghelper.css'], function (historyManager, focusManager, browser, layoutManager, inputManager) {

    function dialogHashHandler(dlg, hash, resolve) {

        var self = this;
        self.originalUrl = window.location.href;
        var activeElement = document.activeElement;
        var removeScrollLockOnClose = false;

        function onHashChange(e) {

            var isBack = self.originalUrl == window.location.href;

            if (isBack || !isOpened(dlg)) {
                window.removeEventListener('popstate', onHashChange);
            }

            if (isBack) {
                self.closedByBack = true;
                closeDialog(dlg);
            }
        }

        function onBackCommand(e) {

            if (e.detail.command == 'back') {
                inputManager.off(dlg, onBackCommand);

                self.closedByBack = true;
                closeDialog(dlg);
                e.preventDefault();
            }
        }

        function onDialogClosed() {

            removeBackdrop(dlg);
            dlg.classList.remove('opened');

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

        dlg.addEventListener('close', onDialogClosed);

        var center = !dlg.classList.contains('fixedSize');
        if (center) {
            dlg.style.left = '50%';
            dlg.style.top = '50%';
        }

        dlg.classList.remove('hide');

        // Use native methods if available
        if (dlg.showModal) {
            if (dlg.getAttribute('modal')) {
                dlg.showModal();
            } else {
                closeOnBackdropClick(dlg);
                dlg.showModal();
            }
            // Undo the auto-focus applied by the native dialog element
            safeBlur(document.activeElement);
        } else {
            addBackdropOverlay(dlg);
        }

        dlg.classList.add('opened');

        if (center) {
            centerDialog(dlg);
        }
        animateDialogOpen(dlg);

        if (dlg.getAttribute('data-autofocus') == 'true') {
            autoFocus(dlg);
        }

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

    function parentWithTag(elem, tagName) {

        while (elem.tagName != tagName) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function closeOnBackdropClick(dlg) {

        dlg.addEventListener('click', function (event) {
            var rect = dlg.getBoundingClientRect();
            var isInDialog = (rect.top <= event.clientY && event.clientY <= rect.top + rect.height
              && rect.left <= event.clientX && event.clientX <= rect.left + rect.width);

            if (!isInDialog) {
                if (parentWithTag(event.target, 'SELECT')) {
                    isInDialog = true;
                }
            }

            if (!isInDialog) {
                close(dlg);
            }
        });
    }

    function autoFocus(dlg) {

        // The dialog may have just been created and webComponents may not have completed initialiazation yet.
        // Without this, seeing some script errors in Firefox

        var delay = browser.animate ? 0 : 500;
        if (!delay) {
            focusManager.autoFocus(dlg);
            return;
        }

        setTimeout(function () {
            focusManager.autoFocus(dlg);
        }, delay);
    }

    function safeBlur(el) {
        if (el && el.blur && el != document.body) {
            el.blur();
        }
    }

    function addBackdropOverlay(dlg) {

        var backdrop = document.createElement('div');
        backdrop.classList.add('dialogBackdrop');
        dlg.parentNode.insertBefore(backdrop, dlg.nextSibling);
        dlg.backdrop = backdrop;

        // Doing this immediately causes the opacity to jump immediately without animating
        setTimeout(function () {
            backdrop.classList.add('opened');
        }, 0);

        backdrop.addEventListener('click', function () {
            close(dlg);
        });
    }

    function isHistoryEnabled(dlg) {
        return dlg.getAttribute('data-history') == 'true';
    }

    function open(dlg) {

        return new Promise(function (resolve, reject) {

            new dialogHashHandler(dlg, 'dlg' + new Date().getTime(), resolve);
        });
    }

    function isOpened(dlg) {

        //return dlg.opened;
        return !dlg.classList.contains('hide');
    }

    function close(dlg) {

        if (isOpened(dlg)) {
            if (isHistoryEnabled(dlg)) {
                history.back();
            } else {
                closeDialog(dlg);
            }
        }
    }

    function scaleUp(elem) {

        var keyframes = [
          { transform: 'scale(0)', offset: 0 },
          { transform: 'scale(1,1)', offset: 1 }];
        var timing = elem.animationConfig.entry.timing;
        return elem.animate(keyframes, timing);
    }

    function fadeIn(elem) {

        var keyframes = [
          { opacity: '0', offset: 0 },
          { opacity: '1', offset: 1 }];
        var timing = elem.animationConfig.entry.timing;
        return elem.animate(keyframes, timing);
    }

    function fadeOut(elem) {

        var keyframes = [
          { opacity: '1', offset: 0 },
          { opacity: '0', offset: 1 }];
        var timing = elem.animationConfig.exit.timing;
        return elem.animate(keyframes, timing);
    }

    function closeDialog(dlg) {

        if (!dlg.classList.contains('hide')) {

            var onAnimationFinish = function () {
                dlg.classList.add('hide');
                if (dlg.close) {
                    dlg.close();
                } else {
                    dlg.dispatchEvent(new CustomEvent('close', {
                        bubbles: false,
                        cancelable: false
                    }));
                }
            };
            if (!dlg.animationConfig || !dlg.animate) {
                onAnimationFinish();
                return;
            }

            fadeOut(dlg).onfinish = onAnimationFinish;
        }
    }

    function animateDialogOpen(dlg) {

        if (!dlg.animationConfig || !dlg.animate) {
            return;
        }
        if (dlg.animationConfig.entry.name == 'fade-in-animation') {
            fadeIn(dlg);
        } else if (dlg.animationConfig.entry.name == 'scale-up-animation') {
            scaleUp(dlg);
        }
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

    function centerDialog(dlg) {

        dlg.style.marginLeft = (-(dlg.offsetWidth / 2)) + 'px';
        dlg.style.marginTop = (-(dlg.offsetHeight / 2)) + 'px';
    }

    function removeBackdrop(dlg) {

        var backdrop = dlg.backdrop;

        if (backdrop) {
            dlg.backdrop = null;

            backdrop.classList.remove('opened');

            setTimeout(function () {
                backdrop.parentNode.removeChild(backdrop);
            }, 300);
        }
    }

    function createDialog(options) {

        options = options || {};

        var dlg = document.createElement('dialog');

        // If there's no native dialog support, use a plain div
        // Also not working well in samsung tizen browser, content inside not clickable
        if (!dlg.showModal || browser.tv) {
            dlg = document.createElement('div');
        }

        dlg.classList.add('hide');

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

        if (options.autoFocus !== false) {
            dlg.setAttribute('data-autofocus', 'true');
        }

        var defaultEntryAnimation = browser.animate ? 'scale-up-animation' : 'fade-in-animation';
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
                timing: { duration: options.exitAnimationDuration || 300, easing: 'ease-in' }
            }
        };

        // too buggy in IE, not even worth it
        if (!browser.animate) {
            dlg.animationConfig = null;
            dlg.entryAnimation = null;
            dlg.exitAnimation = null;
        }

        dlg.classList.add('dialog');

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

        return dlg;
    }

    return {
        open: open,
        close: close,
        createDialog: createDialog
    };
});