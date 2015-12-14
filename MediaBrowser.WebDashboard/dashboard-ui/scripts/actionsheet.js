(function () {

    function show(options) {

        require(['paper-menu', 'paper-dialog', 'paper-dialog-scrollable', 'scale-up-animation', 'fade-out-animation'], function () {
            showInternal(options);
        });
    }

    function showInternal(options) {

        // items
        // positionTo
        // showCancel
        // title
        var html = '';

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
            html += '<h2>';
            html += options.title;
            html += '</h2>';
        }

        // There seems to be a bug with this in safari causing it to immediately roll up to 0 height
        var isScrollable = !browserInfo.safari;

        if (isScrollable) {
            html += '<paper-dialog-scrollable>';
        }

        // If any items have an icon, give them all an icon just to make sure they're all lined up evenly
        var renderIcon = options.items.filter(function (o) {
            return o.ironIcon;
        }).length;

        if (options.title && !renderIcon) {
            html += '<paper-menu style="text-align:center;">';
        } else {
            html += '<paper-menu>';
        }
        for (var i = 0, length = options.items.length; i < length; i++) {

            var option = options.items[i];

            html += '<paper-menu-item class="actionSheetMenuItem" data-id="' + option.id + '" style="display:block;">';

            if (option.ironIcon) {
                html += '<iron-icon icon="' + option.ironIcon + '"></iron-icon>';
            }
            else if (renderIcon) {
                html += '<iron-icon></iron-icon>';
            }
            html += '<span>' + option.name + '</span>';
            html += '</paper-menu-item>';
        }
        html += '</paper-menu>';

        if (isScrollable) {
            html += '</paper-dialog-scrollable>';
        }

        if (options.showCancel) {
            html += '<div class="buttons">';
            html += '<paper-button dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
            html += '</div>';
        }

        var dlg = document.createElement('paper-dialog');
        dlg.setAttribute('with-backdrop', 'with-backdrop');
        dlg.innerHTML = html;

        if (pos) {
            dlg.style.position = 'fixed';
            dlg.style.left = pos.left + 'px';
            dlg.style.top = pos.top + 'px';
        }
        document.body.appendChild(dlg);

        // The animations flicker in IE
        if (!browserInfo.msie) {
            dlg.animationConfig = {
                // scale up
                'entry': {
                    name: 'scale-up-animation',
                    node: dlg,
                    timing: { duration: 160, easing: 'ease-out' }
                },
                // fade out
                'exit': {
                    name: 'fade-out-animation',
                    node: dlg,
                    timing: { duration: 200, easing: 'ease-in' }
                }
            };
        }

        setTimeout(function () {
            dlg.open();
        }, 50);

        // Has to be assigned a z-index after the call to .open() 
        dlg.addEventListener('iron-overlay-closed', function () {
            dlg.parentNode.removeChild(dlg);
        });

        // Seeing an issue in some non-chrome browsers where this is requiring a double click
        var eventName = browserInfo.chrome || browserInfo.safari ? 'click' : 'mousedown';

        $('.actionSheetMenuItem', dlg).on(eventName, function () {

            var selectedId = this.getAttribute('data-id');

            // Add a delay here to allow the click animation to finish, for nice effect
            setTimeout(function () {

                dlg.close();

                if (options.callback) {
                    options.callback(selectedId);
                }

            }, 100);
        });
    }

    window.ActionSheetElement = {
        show: show
    };
})();