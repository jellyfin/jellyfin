define(['dialogHelper', 'layoutManager', 'dialogText', 'paper-button', 'css!./actionsheet'], function (dialogHelper, layoutManager, dialogText) {

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function getOffsets(elems) {

        var doc = document;
        var results = [];

        if (!doc) {
            return results;
        }

        var box;
        var elem;

        for (var i = 0, length = elems.length; i < length; i++) {

            elem = elems[i];
            // Support: BlackBerry 5, iOS 3 (original iPhone)
            // If we don't have gBCR, just use 0,0 rather than error
            if (elem.getBoundingClientRect) {
                box = elem.getBoundingClientRect();
            } else {
                box = { top: 0, left: 0 };
            }

            results[i] = {
                top: box.top,
                left: box.left
            };
        }

        return results;
    }

    function getPosition(options, dlg) {

        var windowHeight = window.innerHeight;

        if (windowHeight < 540) {
            return null;
        }

        var pos = getOffsets([options.positionTo])[0];

        pos.top += options.positionTo.offsetHeight / 2;
        pos.left += options.positionTo.offsetWidth / 2;

        // Account for popup size 
        pos.top -= ((dlg.offsetHeight || 300) / 2);
        pos.left -= ((dlg.offsetWidth || 160) / 2);

        // Avoid showing too close to the bottom
        pos.top = Math.min(pos.top, windowHeight - 300);
        pos.left = Math.min(pos.left, window.innerWidth - 300);

        // Do some boundary checking
        pos.top = Math.max(pos.top, 10);
        pos.left = Math.max(pos.left, 10);

        return pos;
    }

    function addCenterFocus(dlg) {

        require(['scrollHelper'], function (scrollHelper) {
            scrollHelper.centerFocus.on(dlg.querySelector('.actionSheetScroller'), false);
        });
    }

    function show(options) {

        // items
        // positionTo
        // showCancel
        // title
        var dialogOptions = {
            removeOnClose: true,
            enableHistory: options.enableHistory
        };

        var backButton = false;

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
            backButton = true;
            dialogOptions.autoFocus = true;
        } else {

            dialogOptions.modal = false;
            dialogOptions.entryAnimationDuration = 160;
            dialogOptions.exitAnimationDuration = 200;
            dialogOptions.autoFocus = false;
        }

        var dlg = dialogHelper.createDialog(dialogOptions);

        if (!layoutManager.tv) {
            dlg.classList.add('extraSpacing');
        }

        dlg.classList.add('actionSheet');

        var html = '';
        html += '<div class="actionSheetContent">';

        if (options.title) {

            if (layoutManager.tv) {
                html += '<h1 class="actionSheetTitle">';
                html += options.title;
                html += '</h1>';
            } else {
                html += '<h2 class="actionSheetTitle">';
                html += options.title;
                html += '</h2>';
            }
        }

        html += '<div class="actionSheetScroller">';

        options.items.forEach(function (o) {
            o.ironIcon = o.selected ? 'check' : null;
        });

        var itemsWithIcons = options.items.filter(function (o) {
            return o.ironIcon;
        });

        // If any items have an icon, give them all an icon just to make sure they're all lined up evenly
        var renderIcon = itemsWithIcons.length;
        var center = options.title && (!itemsWithIcons.length /*|| itemsWithIcons.length != options.items.length*/);

        if (center) {
            dlg.classList.add('centered');
        }

        var enablePaperMenu = !layoutManager.tv;
        var itemTagName = 'paper-button';

        if (enablePaperMenu) {
            html += '<paper-menu>';
            itemTagName = 'paper-menu-item';
        }

        for (var i = 0, length = options.items.length; i < length; i++) {

            var option = options.items[i];

            var autoFocus = option.selected ? ' autoFocus' : '';
            html += '<' + itemTagName + autoFocus + ' class="actionSheetMenuItem" data-id="' + option.id + '" style="display:block;">';

            if (option.ironIcon) {
                html += '<iron-icon class="actionSheetItemIcon" icon="' + option.ironIcon + '"></iron-icon>';
            }
            else if (renderIcon && !center) {
                html += '<iron-icon class="actionSheetItemIcon"></iron-icon>';
            }
            html += '<span>' + option.name + '</span>';
            html += '</' + itemTagName + '>';
        }

        if (enablePaperMenu) {
            html += '</paper-menu>';
        }

        if (options.showCancel) {
            html += '<div class="buttons">';
            html += '<paper-button dialog-dismiss>' + dialogText.get('Cancel') + '</paper-button>';
            html += '</div>';
        }
        html += '</div>';

        dlg.innerHTML = html;

        if (layoutManager.tv) {
            addCenterFocus(dlg);
        }

        document.body.appendChild(dlg);

        // Seeing an issue in some non-chrome browsers where this is requiring a double click
        //var eventName = browser.firefox ? 'mousedown' : 'click';
        var eventName = 'click';

        return new Promise(function (resolve, reject) {

            dlg.addEventListener(eventName, function (e) {

                var actionSheetMenuItem = parentWithClass(e.target, 'actionSheetMenuItem');

                if (actionSheetMenuItem) {

                    var selectedId = actionSheetMenuItem.getAttribute('data-id');

                    dialogHelper.close(dlg);

                    // Add a delay here to allow the click animation to finish, for nice effect
                    setTimeout(function () {

                        if (options.callback) {
                            options.callback(selectedId);
                        }

                        resolve(selectedId);

                    }, 100);
                }

            });

            dialogHelper.open(dlg);

            var pos = options.positionTo ? getPosition(options, dlg) : null;

            if (pos) {
                dlg.style.position = 'fixed';
                dlg.style.margin = 0;
                dlg.style.left = pos.left + 'px';
                dlg.style.top = pos.top + 'px';
            }
        });
    }

    return {
        show: show
    };
});