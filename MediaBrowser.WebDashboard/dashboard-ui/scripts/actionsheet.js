define(['paperdialoghelper', 'browser', 'paper-menu', 'paper-dialog', 'scale-up-animation', 'fade-out-animation'], function (paperDialogHelper, browser) {

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function show(options) {

        // items
        // positionTo
        // showCancel
        // title
        var html = '';

        html += '<div style="margin:0;padding:.8em 1em;">';

        var windowHeight = $(window).height();
        var pos;

        // If the window height is under a certain amount, don't bother trying to position
        // based on an element.
        if (options.positionTo && windowHeight >= 540) {

            pos = $(options.positionTo).offset();

            pos.top += $(options.positionTo).innerHeight() / 2;
            pos.left += $(options.positionTo).innerWidth() / 2;

            // Account for margins
            pos.top -= 24;
            pos.left -= 24;

            // Account for popup size - we can't predict this yet so just estimate
            pos.top -= (55 * options.items.length) / 2;
            pos.left -= 80;

            // Account for scroll position
            pos.top -= $(window).scrollTop();
            pos.left -= $(window).scrollLeft();

            // Avoid showing too close to the bottom
            pos.top = Math.min(pos.top, $(window).height() - 300);
            pos.left = Math.min(pos.left, $(window).width() - 300);

            // Do some boundary checking
            pos.top = Math.max(pos.top, 0);
            pos.left = Math.max(pos.left, 0);
        }

        if (options.title) {
            html += '<h3>';
            html += options.title;
            html += '</h3>';
        }

        var itemsWithIcons = options.items.filter(function (o) {
            return o.ironIcon;
        });

        // If any items have an icon, give them all an icon just to make sure they're all lined up evenly
        var renderIcon = itemsWithIcons.length;
        var center = options.title && (!itemsWithIcons.length || itemsWithIcons.length != options.items.length);

        if (center) {
            html += '<paper-menu style="text-align:center;">';
        } else {
            html += '<paper-menu>';
        }
        for (var i = 0, length = options.items.length; i < length; i++) {

            var option = options.items[i];

            html += '<paper-menu-item class="actionSheetMenuItem" data-id="' + option.id + '" style="display:block;">';

            if (option.ironIcon) {
                if (center) {
                    html += '<iron-icon style="margin-right:.5em;" icon="' + option.ironIcon + '"></iron-icon>';
                } else {
                    html += '<iron-icon icon="' + option.ironIcon + '"></iron-icon>';
                }
            }
            else if (renderIcon && !center) {
                html += '<iron-icon></iron-icon>';
            }
            html += '<span>' + option.name + '</span>';
            html += '</paper-menu-item>';
        }
        html += '</paper-menu>';
        html += '</div>';

        if (options.showCancel) {
            html += '<div class="buttons">';
            html += '<paper-button dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
            html += '</div>';
        }

        var dlg = paperDialogHelper.createDialog({
            modal: false,
            entryAnimationDuration: 160,
            exitAnimationDuration: 200,
            enableHistory: options.enableHistory
        });
        dlg.innerHTML = html;

        if (pos) {
            dlg.style.position = 'fixed';
            dlg.style.left = pos.left + 'px';
            dlg.style.top = pos.top + 'px';
        }

        document.body.appendChild(dlg);

        paperDialogHelper.open(dlg);

        // Has to be assigned a z-index after the call to .open() 
        dlg.addEventListener('iron-overlay-closed', function () {
            dlg.parentNode.removeChild(dlg);
        });

        // Seeing an issue in some non-chrome browsers where this is requiring a double click
        var eventName = browser.firefox ? 'mousedown' : 'click';

        dlg.addEventListener(eventName, function (e) {

            var target = parentWithClass(e.target, 'actionSheetMenuItem');
            if (target) {

                var selectedId = target.getAttribute('data-id');

                paperDialogHelper.close(dlg);

                // Add a delay here to allow the click animation to finish, for nice effect
                setTimeout(function () {

                    if (options.callback) {
                        options.callback(selectedId);
                    }

                }, 100);
            }
        });
    }

    return {
        show: show
    };
});